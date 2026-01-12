using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using FinApp.API.Hubs;

namespace FinApp.API.Services;

public interface IFinnhubWebSocketService
{
    Task ConnectAsync();
    Task SubscribeAsync(string symbol);
    Task UnsubscribeAsync(string symbol);
    Task DisconnectAsync();
    bool IsConnected { get; }
    decimal? GetLatestPrice(string symbol);
    event EventHandler<TradeData>? OnTradeReceived;
}

public class FinnhubWebSocketService : IFinnhubWebSocketService, IDisposable
{
    private readonly ILogger<FinnhubWebSocketService> _logger;
    private readonly IHubContext<MarketDataHub> _hubContext;
    private readonly string _apiKey;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cts;
    private readonly ConcurrentDictionary<string, decimal> _latestPrices = new();
    private readonly ConcurrentDictionary<string, bool> _subscriptions = new();
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private bool _isConnecting;

    public bool IsConnected => _webSocket?.State == WebSocketState.Open;
    public event EventHandler<TradeData>? OnTradeReceived;

    public FinnhubWebSocketService(
        IConfiguration configuration,
        ILogger<FinnhubWebSocketService> logger,
        IHubContext<MarketDataHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
        _apiKey = configuration["Finnhub:ApiKey"]
            ?? Environment.GetEnvironmentVariable("FINNHUB_API_KEY")
            ?? throw new ArgumentNullException("Finnhub:ApiKey configuration is missing");
    }

    public async Task ConnectAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (IsConnected || _isConnecting) return;

            _isConnecting = true;
            _cts = new CancellationTokenSource();
            _webSocket = new ClientWebSocket();

            var uri = new Uri($"wss://ws.finnhub.io?token={_apiKey}");
            _logger.LogInformation("Connecting to Finnhub WebSocket...");

            await _webSocket.ConnectAsync(uri, _cts.Token);
            _logger.LogInformation("Connected to Finnhub WebSocket");

            // Start receiving messages
            _ = ReceiveMessagesAsync(_cts.Token);

            // Resubscribe to existing subscriptions
            foreach (var symbol in _subscriptions.Keys)
            {
                await SendSubscribeMessageAsync(symbol);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Finnhub WebSocket");
            throw;
        }
        finally
        {
            _isConnecting = false;
            _connectionLock.Release();
        }
    }

    public async Task SubscribeAsync(string symbol)
    {
        var upperSymbol = symbol.ToUpper();

        if (_subscriptions.TryAdd(upperSymbol, true))
        {
            _logger.LogInformation("Subscribing to {Symbol}", upperSymbol);

            if (IsConnected)
            {
                await SendSubscribeMessageAsync(upperSymbol);
            }
            else
            {
                await ConnectAsync();
            }
        }
    }

    public async Task UnsubscribeAsync(string symbol)
    {
        var upperSymbol = symbol.ToUpper();

        if (_subscriptions.TryRemove(upperSymbol, out _))
        {
            _logger.LogInformation("Unsubscribing from {Symbol}", upperSymbol);

            if (IsConnected)
            {
                var message = JsonSerializer.Serialize(new { type = "unsubscribe", symbol = upperSymbol });
                var bytes = Encoding.UTF8.GetBytes(message);
                await _webSocket!.SendAsync(bytes, WebSocketMessageType.Text, true, _cts?.Token ?? default);
            }
        }
    }

    public decimal? GetLatestPrice(string symbol)
    {
        return _latestPrices.TryGetValue(symbol.ToUpper(), out var price) ? price : null;
    }

    public async Task DisconnectAsync()
    {
        if (_webSocket != null && IsConnected)
        {
            _cts?.Cancel();
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            _logger.LogInformation("Disconnected from Finnhub WebSocket");
        }
    }

    private async Task SendSubscribeMessageAsync(string symbol)
    {
        if (!IsConnected) return;

        var message = JsonSerializer.Serialize(new { type = "subscribe", symbol });
        var bytes = Encoding.UTF8.GetBytes(message);
        await _webSocket!.SendAsync(bytes, WebSocketMessageType.Text, true, _cts?.Token ?? default);
        _logger.LogInformation("Sent subscribe message for {Symbol}", symbol);
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        try
        {
            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                var result = await _webSocket!.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogWarning("WebSocket closed by server");
                    await ReconnectAsync();
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await ProcessMessageAsync(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("WebSocket receive loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving WebSocket message");
            await ReconnectAsync();
        }
    }

    private async Task ProcessMessageAsync(string message)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var typeElement))
            {
                var type = typeElement.GetString();

                if (type == "trade" && root.TryGetProperty("data", out var dataElement))
                {
                    foreach (var trade in dataElement.EnumerateArray())
                    {
                        var symbol = trade.GetProperty("s").GetString() ?? "";
                        var price = trade.GetProperty("p").GetDecimal();
                        var volume = trade.GetProperty("v").GetInt64();
                        var timestamp = trade.GetProperty("t").GetInt64();

                        // Update latest price
                        _latestPrices[symbol] = price;

                        var tradeData = new TradeData
                        {
                            Symbol = symbol,
                            Price = price,
                            Volume = volume,
                            Timestamp = timestamp
                        };

                        // Raise event
                        OnTradeReceived?.Invoke(this, tradeData);

                        // Broadcast to SignalR clients
                        await _hubContext.Clients.Group(symbol).SendAsync("ReceiveTrade", tradeData);
                        await _hubContext.Clients.All.SendAsync("ReceivePriceUpdate", new { symbol, price, timestamp });

                        _logger.LogDebug("Trade: {Symbol} @ {Price}", symbol, price);
                    }
                }
                else if (type == "ping")
                {
                    _logger.LogDebug("Received ping from Finnhub");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WebSocket message: {Message}", message);
        }
    }

    private async Task ReconnectAsync()
    {
        _logger.LogInformation("Attempting to reconnect to Finnhub WebSocket...");
        await Task.Delay(5000); // Wait 5 seconds before reconnecting

        try
        {
            _webSocket?.Dispose();
            _webSocket = null;
            await ConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconnect, will retry...");
            _ = ReconnectAsync(); // Keep trying
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _webSocket?.Dispose();
        _cts?.Dispose();
        _connectionLock.Dispose();
    }
}

public class TradeData
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public long Volume { get; set; }
    public long Timestamp { get; set; }
}

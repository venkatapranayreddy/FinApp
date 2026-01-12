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
    private readonly string? _apiKey;
    private readonly bool _isEnabled;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cts;
    private readonly ConcurrentDictionary<string, decimal> _latestPrices = new();
    private readonly ConcurrentDictionary<string, bool> _subscriptions = new();
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private bool _isConnecting;
    private int _reconnectAttempts;
    private const int MaxReconnectAttempts = 5;
    private bool _rateLimited;
    private DateTime _rateLimitResetTime = DateTime.MinValue;

    public bool IsConnected => _isEnabled && !_rateLimited && _webSocket?.State == WebSocketState.Open;
    public event EventHandler<TradeData>? OnTradeReceived;

    public FinnhubWebSocketService(
        IConfiguration configuration,
        ILogger<FinnhubWebSocketService> logger,
        IHubContext<MarketDataHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
        _apiKey = configuration["Finnhub:ApiKey"]
            ?? Environment.GetEnvironmentVariable("FINNHUB_API_KEY");

        _isEnabled = !string.IsNullOrEmpty(_apiKey);

        if (!_isEnabled)
        {
            _logger.LogWarning("Finnhub API key not configured. Real-time WebSocket features disabled.");
        }
    }

    public async Task ConnectAsync()
    {
        if (!_isEnabled)
        {
            _logger.LogDebug("Finnhub WebSocket disabled - skipping connection");
            return;
        }

        // Check if we're rate limited
        if (_rateLimited && DateTime.UtcNow < _rateLimitResetTime)
        {
            _logger.LogDebug("Finnhub rate limited until {ResetTime}, skipping connection", _rateLimitResetTime);
            return;
        }

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

            // Reset rate limit and reconnect tracking on successful connection
            _rateLimited = false;
            _reconnectAttempts = 0;

            // Start receiving messages
            _ = ReceiveMessagesAsync(_cts.Token);

            // Resubscribe to existing subscriptions
            foreach (var symbol in _subscriptions.Keys)
            {
                await SendSubscribeMessageAsync(symbol);
            }
        }
        catch (WebSocketException ex) when (ex.Message.Contains("429"))
        {
            _rateLimited = true;
            _rateLimitResetTime = DateTime.UtcNow.AddMinutes(5); // Wait 5 minutes before retry
            _logger.LogWarning("Finnhub rate limited (429). Will retry after {ResetTime}", _rateLimitResetTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Finnhub WebSocket");
            // Don't throw - just log and continue without WebSocket
        }
        finally
        {
            _isConnecting = false;
            _connectionLock.Release();
        }
    }

    public async Task SubscribeAsync(string symbol)
    {
        if (!_isEnabled) return;

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
        // Check if we're rate limited
        if (_rateLimited)
        {
            _logger.LogDebug("Skipping reconnect - rate limited until {ResetTime}", _rateLimitResetTime);
            return;
        }

        // Check max reconnect attempts
        if (_reconnectAttempts >= MaxReconnectAttempts)
        {
            _logger.LogWarning("Max reconnect attempts ({Max}) reached. Stopping reconnection.", MaxReconnectAttempts);
            return;
        }

        _reconnectAttempts++;

        // Exponential backoff: 5s, 10s, 20s, 40s, 80s
        var delay = TimeSpan.FromSeconds(5 * Math.Pow(2, _reconnectAttempts - 1));
        _logger.LogInformation("Reconnect attempt {Attempt}/{Max} in {Delay}s...",
            _reconnectAttempts, MaxReconnectAttempts, delay.TotalSeconds);

        await Task.Delay(delay);

        try
        {
            _webSocket?.Dispose();
            _webSocket = null;
            await ConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconnect (attempt {Attempt}/{Max})",
                _reconnectAttempts, MaxReconnectAttempts);

            if (_reconnectAttempts < MaxReconnectAttempts && !_rateLimited)
            {
                _ = ReconnectAsync();
            }
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

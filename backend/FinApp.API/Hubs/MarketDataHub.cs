using Microsoft.AspNetCore.SignalR;
using FinApp.API.Services;

namespace FinApp.API.Hubs;

public class MarketDataHub : Hub
{
    private readonly IFinnhubWebSocketService _finnhubService;
    private readonly ILogger<MarketDataHub> _logger;

    public MarketDataHub(IFinnhubWebSocketService finnhubService, ILogger<MarketDataHub> logger)
    {
        _finnhubService = finnhubService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to real-time trades for a symbol
    /// </summary>
    public async Task SubscribeToSymbol(string symbol)
    {
        var upperSymbol = symbol.ToUpper();
        _logger.LogInformation("Client {ConnectionId} subscribing to {Symbol}", Context.ConnectionId, upperSymbol);

        // Add client to symbol group
        await Groups.AddToGroupAsync(Context.ConnectionId, upperSymbol);

        // Subscribe to Finnhub
        await _finnhubService.SubscribeAsync(upperSymbol);

        // Send current price if available
        var currentPrice = _finnhubService.GetLatestPrice(upperSymbol);
        if (currentPrice.HasValue)
        {
            await Clients.Caller.SendAsync("ReceivePriceUpdate", new
            {
                symbol = upperSymbol,
                price = currentPrice.Value,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        await Clients.Caller.SendAsync("SubscriptionConfirmed", upperSymbol);
    }

    /// <summary>
    /// Unsubscribe from real-time trades for a symbol
    /// </summary>
    public async Task UnsubscribeFromSymbol(string symbol)
    {
        var upperSymbol = symbol.ToUpper();
        _logger.LogInformation("Client {ConnectionId} unsubscribing from {Symbol}", Context.ConnectionId, upperSymbol);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, upperSymbol);
        await Clients.Caller.SendAsync("UnsubscriptionConfirmed", upperSymbol);
    }

    /// <summary>
    /// Subscribe to multiple symbols at once
    /// </summary>
    public async Task SubscribeToSymbols(string[] symbols)
    {
        foreach (var symbol in symbols)
        {
            await SubscribeToSymbol(symbol);
        }
    }

    /// <summary>
    /// Get current price for a symbol
    /// </summary>
    public async Task GetCurrentPrice(string symbol)
    {
        var upperSymbol = symbol.ToUpper();
        var price = _finnhubService.GetLatestPrice(upperSymbol);

        if (price.HasValue)
        {
            await Clients.Caller.SendAsync("ReceivePriceUpdate", new
            {
                symbol = upperSymbol,
                price = price.Value,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }
        else
        {
            // Subscribe to get the price
            await SubscribeToSymbol(upperSymbol);
        }
    }

    /// <summary>
    /// Ping to keep connection alive
    /// </summary>
    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }
}

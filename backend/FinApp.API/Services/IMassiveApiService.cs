using FinApp.API.Models;

namespace FinApp.API.Services;

public interface IMassiveApiService
{
    // Tickers - fetches 10 at a time with pagination
    Task<List<MassiveTicker>> GetAllTickersAsync(string exchange);
    Task<List<MassiveTicker>> GetTickersAsync(string exchange, int limit = 10);

    // Ticker Details (company name, etc.)
    Task<TickerDetailsResponse?> GetTickerDetailsAsync(string ticker);

    // Aggregates
    Task<MassiveAggsResponse?> GetAggregatesAsync(string ticker, DateTime from, DateTime to);

    // Snapshots
    Task<MassiveTickerSnapshot?> GetSnapshotAsync(string ticker);
    Task<List<MassiveTickerSnapshot>> GetSnapshotsAsync(IEnumerable<string> tickers);
    Task<List<MassiveTickerSnapshot>> GetTopMoversAsync(string direction = "gainers");

    // Trades & Quotes
    Task<MassiveTradesResponse?> GetTradesAsync(string ticker, string? timestamp = null, int limit = 10);
    Task<MassiveLastTrade?> GetLastTradeAsync(string ticker);
    Task<MassiveQuotesResponse?> GetQuotesAsync(string ticker, string? timestamp = null, int limit = 10);
    Task<MassiveLastQuote?> GetLastQuoteAsync(string ticker);

    // Technical Indicators
    Task<MassiveIndicatorResponse?> GetSmaAsync(string ticker, int window = 20, string timespan = "day", int limit = 10);
    Task<MassiveIndicatorResponse?> GetEmaAsync(string ticker, int window = 20, string timespan = "day", int limit = 10);
    Task<MassiveMacdResponse?> GetMacdAsync(string ticker, string timespan = "day", int limit = 10);
    Task<MassiveIndicatorResponse?> GetRsiAsync(string ticker, int window = 14, string timespan = "day", int limit = 10);
}

using FinApp.API.Data;
using FinApp.API.Models;
using Supabase.Postgrest;

namespace FinApp.API.Services;

public class StockService : IStockService
{
    private readonly SupabaseContext _supabase;
    private readonly IMassiveApiService _massiveApi;
    private readonly ILogger<StockService> _logger;

    public StockService(
        SupabaseContext supabase,
        IMassiveApiService massiveApi,
        ILogger<StockService> logger)
    {
        _supabase = supabase;
        _massiveApi = massiveApi;
        _logger = logger;
    }

    public async Task<List<Stock>> GetAllStocksAsync(int page = 1, int pageSize = 50, string? exchange = null)
    {
        var offset = (page - 1) * pageSize;

        if (!string.IsNullOrEmpty(exchange))
        {
            var response = await _supabase.Client.From<Stock>()
                .Filter("exchange", Constants.Operator.Equals, exchange)
                .Order("ticker", Constants.Ordering.Ascending)
                .Range(offset, offset + pageSize - 1)
                .Get();
            return response.Models;
        }
        else
        {
            var response = await _supabase.Client.From<Stock>()
                .Order("ticker", Constants.Ordering.Ascending)
                .Range(offset, offset + pageSize - 1)
                .Get();
            return response.Models;
        }
    }

    public async Task<int> GetStockCountAsync(string? exchange = null)
    {
        if (!string.IsNullOrEmpty(exchange))
        {
            var response = await _supabase.Client.From<Stock>()
                .Filter("exchange", Constants.Operator.Equals, exchange)
                .Count(Constants.CountType.Exact);
            return response;
        }
        else
        {
            var response = await _supabase.Client.From<Stock>()
                .Count(Constants.CountType.Exact);
            return response;
        }
    }

    public async Task<int> SyncStocksAsync()
    {
        var syncedCount = 0;
        var skippedCount = 0;
        var exchanges = new[] { "XNAS", "XNYS" };
        var exchangeNameMap = new Dictionary<string, string>
        {
            { "XNAS", "NASDAQ" },
            { "XNYS", "NYSE" }
        };

        foreach (var exchange in exchanges)
        {
            _logger.LogInformation("Syncing stocks from {Exchange}...", exchange);

            var tickers = await _massiveApi.GetAllTickersAsync(exchange);
            _logger.LogInformation("Found {Count} tickers for {Exchange}", tickers.Count, exchange);

            foreach (var ticker in tickers)
            {
                try
                {
                    // Check if ticker already exists
                    var existing = await _supabase.Client.From<Stock>()
                        .Filter("ticker", Constants.Operator.Equals, ticker.Ticker)
                        .Single();

                    if (existing != null)
                    {
                        // Update existing record
                        existing.Name = ticker.Name;
                        existing.Exchange = exchangeNameMap.GetValueOrDefault(exchange, exchange);
                        existing.MarketCap = ticker.MarketCap;
                        existing.UpdatedAt = DateTime.UtcNow;

                        await _supabase.Client.From<Stock>().Update(existing);
                        skippedCount++;
                    }
                    else
                    {
                        // Insert new record
                        var stock = new Stock
                        {
                            Id = Guid.NewGuid(),
                            Ticker = ticker.Ticker,
                            Name = ticker.Name,
                            Exchange = exchangeNameMap.GetValueOrDefault(exchange, exchange),
                            MarketCap = ticker.MarketCap,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await _supabase.Client.From<Stock>().Insert(stock);
                        syncedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync ticker {Ticker}", ticker.Ticker);
                }
            }
        }

        _logger.LogInformation("Synced {NewCount} new stocks, updated {UpdatedCount} existing", syncedCount, skippedCount);
        return syncedCount + skippedCount;
    }

    public async Task<StockPerformanceResponse> GetStockPerformanceAsync(FilterRequest filter)
    {
        // Get total count first
        int totalCount;
        if (!string.IsNullOrEmpty(filter.Exchange))
        {
            totalCount = await _supabase.Client.From<Stock>()
                .Filter("exchange", Constants.Operator.Equals, filter.Exchange)
                .Count(Constants.CountType.Exact);
        }
        else
        {
            totalCount = await _supabase.Client.From<Stock>()
                .Count(Constants.CountType.Exact);
        }

        // Fetch only the stocks for current page (to avoid rate limiting)
        var offset = (filter.Page - 1) * filter.PageSize;
        List<Stock> stocks;

        if (!string.IsNullOrEmpty(filter.Exchange))
        {
            var stocksResponse = await _supabase.Client.From<Stock>()
                .Filter("exchange", Constants.Operator.Equals, filter.Exchange)
                .Order("ticker", Constants.Ordering.Ascending)
                .Range(offset, offset + filter.PageSize - 1)
                .Get();
            stocks = stocksResponse.Models;
        }
        else
        {
            var stocksResponse = await _supabase.Client.From<Stock>()
                .Order("ticker", Constants.Ordering.Ascending)
                .Range(offset, offset + filter.PageSize - 1)
                .Get();
            stocks = stocksResponse.Models;
        }

        var performances = new List<StockPerformance>();

        foreach (var stock in stocks)
        {
            var aggs = await _massiveApi.GetAggregatesAsync(stock.Ticker, filter.StartDate, filter.EndDate);

            if (aggs?.Results != null && aggs.Results.Count >= 1)
            {
                var firstBar = aggs.Results.First();
                var lastBar = aggs.Results.Last();

                var percentChange = firstBar.Open != 0
                    ? ((lastBar.Close - firstBar.Open) / firstBar.Open) * 100
                    : 0;

                var isProfit = percentChange > 0;

                // Apply profit/loss filter
                if (filter.ProfitOnly == true && !isProfit) continue;
                if (filter.LossOnly == true && isProfit) continue;

                performances.Add(new StockPerformance
                {
                    Ticker = stock.Ticker,
                    Name = stock.Name,
                    Exchange = stock.Exchange,
                    StartPrice = firstBar.Open,
                    EndPrice = lastBar.Close,
                    PercentChange = Math.Round(percentChange, 2),
                    IsProfit = isProfit,
                    Volume = aggs.Results.Sum(r => r.Volume),
                    MarketCap = stock.MarketCap,
                    OpenPrice = firstBar.Open,
                    ClosePrice = lastBar.Close,
                    HighPrice = aggs.Results.Max(r => r.High),
                    LowPrice = aggs.Results.Min(r => r.Low)
                });
            }
        }

        // Data is already paginated from DB, return directly
        return new StockPerformanceResponse
        {
            Data = performances,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }
}

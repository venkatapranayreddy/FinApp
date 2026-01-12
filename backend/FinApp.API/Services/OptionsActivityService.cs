using FinApp.API.Models;

namespace FinApp.API.Services;

public interface IOptionsActivityService
{
    Task<OptionsActivityResponse> GetUnusualActivityAsync(OptionsActivityFilter filter);
    Task<OptionsActivityDetails> GetActivityDetailsAsync(string symbol);
    Task<List<UnusualOptionsActivity>> GetTopMoversAsync(int count = 10);
}

public class OptionsActivityService : IOptionsActivityService
{
    private readonly IMassiveApiService _massiveApi;
    private readonly ILogger<OptionsActivityService> _logger;
    private static List<UnusualOptionsActivity>? _cachedData;
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    // Thresholds for classification
    private const decimal HIGH_IV_THRESHOLD = 50m;
    private const decimal BULLISH_RATIO_THRESHOLD = 0.7m; // Put/Call < 0.7 = Bullish
    private const decimal BEARISH_RATIO_THRESHOLD = 1.3m; // Put/Call > 1.3 = Bearish
    private const decimal SMALL_CAP_MAX = 2_000_000_000m;
    private const decimal MID_CAP_MAX = 10_000_000_000m;
    private const decimal LARGE_CAP_MAX = 200_000_000_000m;

    public OptionsActivityService(IMassiveApiService massiveApi, ILogger<OptionsActivityService> logger)
    {
        _massiveApi = massiveApi;
        _logger = logger;
    }

    public async Task<OptionsActivityResponse> GetUnusualActivityAsync(OptionsActivityFilter filter)
    {
        var allData = await GetOrRefreshDataAsync();

        // Apply filters
        var filteredData = allData.AsEnumerable();

        if (filter.BiasFilter.HasValue)
        {
            filteredData = filteredData.Where(x => x.Bias == filter.BiasFilter.Value);
        }

        if (filter.HighIVOnly == true)
        {
            filteredData = filteredData.Where(x => x.ImpliedVolatility > HIGH_IV_THRESHOLD);
        }

        if (filter.CapCategory.HasValue)
        {
            filteredData = filteredData.Where(x => x.CapCategory == filter.CapCategory.Value);
        }

        if (filter.MinVolume > 0)
        {
            filteredData = filteredData.Where(x => x.TotalOptionsVolume >= filter.MinVolume);
        }

        if (filter.MinVolumeChangePercent > 0)
        {
            filteredData = filteredData.Where(x => x.VolumeChangePercent >= filter.MinVolumeChangePercent);
        }

        // Apply sorting
        filteredData = filter.SortBy?.ToLower() switch
        {
            "volumechangepercent" => filter.SortDescending
                ? filteredData.OrderByDescending(x => x.VolumeChangePercent)
                : filteredData.OrderBy(x => x.VolumeChangePercent),
            "totaloptionsvolume" => filter.SortDescending
                ? filteredData.OrderByDescending(x => x.TotalOptionsVolume)
                : filteredData.OrderBy(x => x.TotalOptionsVolume),
            "impliedvolatility" => filter.SortDescending
                ? filteredData.OrderByDescending(x => x.ImpliedVolatility)
                : filteredData.OrderBy(x => x.ImpliedVolatility),
            "putcallratio" => filter.SortDescending
                ? filteredData.OrderByDescending(x => x.PutCallRatio)
                : filteredData.OrderBy(x => x.PutCallRatio),
            "pricechangepercent" => filter.SortDescending
                ? filteredData.OrderByDescending(x => x.PriceChangePercent)
                : filteredData.OrderBy(x => x.PriceChangePercent),
            _ => filteredData.OrderByDescending(x => x.VolumeChangePercent)
        };

        var dataList = filteredData.ToList();
        var totalCount = dataList.Count;

        // Apply pagination
        var pagedData = dataList
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        // Calculate stats
        var stats = new OptionsActivityStats
        {
            TotalSymbols = totalCount,
            BullishCount = dataList.Count(x => x.Bias == DirectionalBias.Bullish),
            BearishCount = dataList.Count(x => x.Bias == DirectionalBias.Bearish),
            NeutralCount = dataList.Count(x => x.Bias == DirectionalBias.Neutral),
            AvgVolumeChangePercent = dataList.Any() ? dataList.Average(x => x.VolumeChangePercent) : 0,
            TopBullishSymbol = dataList.Where(x => x.Bias == DirectionalBias.Bullish)
                .OrderByDescending(x => x.VolumeChangePercent)
                .FirstOrDefault()?.Symbol ?? "",
            TopBearishSymbol = dataList.Where(x => x.Bias == DirectionalBias.Bearish)
                .OrderByDescending(x => x.VolumeChangePercent)
                .FirstOrDefault()?.Symbol ?? ""
        };

        return new OptionsActivityResponse
        {
            Data = pagedData,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize),
            LastUpdated = _cacheExpiry.Subtract(CacheDuration),
            Stats = stats
        };
    }

    public async Task<OptionsActivityDetails> GetActivityDetailsAsync(string symbol)
    {
        var allData = await GetOrRefreshDataAsync();
        var activity = allData.FirstOrDefault(x => x.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));

        if (activity == null)
        {
            throw new KeyNotFoundException($"Symbol {symbol} not found in options activity data");
        }

        var topContracts = GenerateMockContracts(activity);
        var explanation = GenerateAiExplanation(activity, topContracts);
        var catalysts = GeneratePotentialCatalysts(activity);

        return new OptionsActivityDetails
        {
            Symbol = activity.Symbol,
            CompanyName = activity.CompanyName,
            Summary = activity,
            TopContracts = topContracts,
            AiExplanation = explanation,
            PotentialCatalysts = catalysts,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<List<UnusualOptionsActivity>> GetTopMoversAsync(int count = 10)
    {
        var allData = await GetOrRefreshDataAsync();
        return allData
            .OrderByDescending(x => x.VolumeChangePercent)
            .Take(count)
            .ToList();
    }

    #region Private Methods

    private async Task<List<UnusualOptionsActivity>> GetOrRefreshDataAsync()
    {
        if (_cachedData != null && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedData;
        }

        _logger.LogInformation("Refreshing options activity data...");

        // In production, this would fetch from real APIs
        // For now, generate realistic mock data
        _cachedData = await GenerateMockDataAsync();
        _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

        return _cachedData;
    }

    private async Task<List<UnusualOptionsActivity>> GenerateMockDataAsync()
    {
        // Simulated stock data with unusual options activity
        var stocksData = new List<(string Symbol, string Name, decimal Price, decimal MarketCap)>
        {
            ("AAPL", "Apple Inc.", 259.37m, 3812000000000m),
            ("MSFT", "Microsoft Corp", 479.28m, 3562000000000m),
            ("NVDA", "NVIDIA Corporation", 148.50m, 3650000000000m),
            ("TSLA", "Tesla Inc.", 394.50m, 1267000000000m),
            ("AMD", "Advanced Micro Devices", 119.80m, 194000000000m),
            ("META", "Meta Platforms Inc.", 617.25m, 1568000000000m),
            ("AMZN", "Amazon.com Inc.", 227.35m, 2400000000000m),
            ("GOOGL", "Alphabet Inc.", 328.57m, 3964000000000m),
            ("NFLX", "Netflix Inc.", 935.12m, 405000000000m),
            ("SPY", "SPDR S&P 500 ETF", 596.45m, 0m),
            ("QQQ", "Invesco QQQ Trust", 523.80m, 0m),
            ("PLTR", "Palantir Technologies", 75.20m, 171000000000m),
            ("COIN", "Coinbase Global", 275.60m, 70000000000m),
            ("SOFI", "SoFi Technologies", 15.85m, 17000000000m),
            ("HOOD", "Robinhood Markets", 42.30m, 37000000000m),
            ("MARA", "Marathon Digital", 21.45m, 6800000000m),
            ("RIOT", "Riot Platforms", 13.20m, 4200000000m),
            ("GME", "GameStop Corp", 27.85m, 12000000000m),
            ("AMC", "AMC Entertainment", 3.45m, 1800000000m),
            ("SMCI", "Super Micro Computer", 32.50m, 19000000000m),
            ("ARM", "Arm Holdings", 148.90m, 156000000000m),
            ("MU", "Micron Technology", 102.35m, 113000000000m),
            ("INTC", "Intel Corporation", 19.45m, 83000000000m),
            ("BAC", "Bank of America", 46.80m, 360000000000m),
            ("JPM", "JPMorgan Chase", 256.70m, 735000000000m),
            ("XOM", "Exxon Mobil", 105.40m, 455000000000m),
            ("CVX", "Chevron Corporation", 148.25m, 268000000000m),
            ("PFE", "Pfizer Inc.", 26.15m, 148000000000m),
            ("MRNA", "Moderna Inc.", 36.80m, 14000000000m),
            ("BABA", "Alibaba Group", 85.60m, 209000000000m),
            ("NIO", "NIO Inc.", 4.25m, 8500000000m),
            ("RIVN", "Rivian Automotive", 12.85m, 13500000000m),
            ("LCID", "Lucid Group", 2.35m, 7200000000m),
            ("F", "Ford Motor Company", 9.85m, 39000000000m),
            ("GM", "General Motors", 52.40m, 58000000000m),
        };

        var random = new Random(DateTime.UtcNow.DayOfYear); // Consistent within same day
        var result = new List<UnusualOptionsActivity>();

        foreach (var stock in stocksData)
        {
            // Generate realistic options data
            var avgVolume = random.Next(50000, 500000);
            var volumeMultiplier = 1m + (decimal)(random.NextDouble() * 15); // 1x to 16x normal
            var todayVolume = (long)(avgVolume * volumeMultiplier);

            // Some stocks have more calls, some more puts
            var callPutSplit = 0.3 + random.NextDouble() * 0.4; // 30-70% calls
            var callVolume = (long)(todayVolume * callPutSplit);
            var putVolume = todayVolume - callVolume;

            var priceChange = (decimal)(random.NextDouble() * 10 - 5); // -5% to +5%
            var iv = 20m + (decimal)(random.NextDouble() * 80); // 20% to 100%

            var activity = new UnusualOptionsActivity
            {
                Symbol = stock.Symbol,
                CompanyName = stock.Name,
                LastPrice = stock.Price * (1 + priceChange / 100),
                PriceChange = stock.Price * priceChange / 100,
                PriceChangePercent = priceChange,
                TotalOptionsVolume = todayVolume,
                AvgOptionsVolume30Day = avgVolume,
                CallVolume = callVolume,
                PutVolume = putVolume,
                VolumeChangePercent = volumeMultiplier * 100,
                PutCallRatio = putVolume > 0 && callVolume > 0 ? (decimal)putVolume / callVolume : 0,
                ImpliedVolatility = iv,
                OpenInterest = random.Next(100000, 5000000),
                MarketCap = stock.MarketCap,
                Timestamp = DateTime.UtcNow
            };

            // Calculate directional bias
            activity.Bias = CalculateDirectionalBias(activity.PutCallRatio);

            // Calculate market cap category
            activity.CapCategory = CalculateMarketCapCategory(activity.MarketCap);

            // Calculate activity score (weighted combination of factors)
            activity.ActivityScore = CalculateActivityScore(activity);

            result.Add(activity);
        }

        // Fetch real prices for top symbols if possible
        await EnrichWithRealPricesAsync(result);

        return result.OrderByDescending(x => x.VolumeChangePercent).ToList();
    }

    private async Task EnrichWithRealPricesAsync(List<UnusualOptionsActivity> activities)
    {
        try
        {
            var topSymbols = activities.Take(10).Select(a => a.Symbol).ToList();

            foreach (var symbol in topSymbols)
            {
                try
                {
                    var details = await _massiveApi.GetTickerDetailsAsync(symbol);
                    var activity = activities.First(a => a.Symbol == symbol);

                    if (details != null)
                    {
                        activity.CompanyName = details.Name;
                        if (details.MarketCap.HasValue)
                        {
                            activity.MarketCap = details.MarketCap.Value;
                            activity.CapCategory = CalculateMarketCapCategory(activity.MarketCap);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to enrich data for {Symbol}", symbol);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enrich options data with real prices");
        }
    }

    private DirectionalBias CalculateDirectionalBias(decimal putCallRatio)
    {
        if (putCallRatio < BULLISH_RATIO_THRESHOLD)
            return DirectionalBias.Bullish;
        if (putCallRatio > BEARISH_RATIO_THRESHOLD)
            return DirectionalBias.Bearish;
        return DirectionalBias.Neutral;
    }

    private MarketCapCategory CalculateMarketCapCategory(decimal marketCap)
    {
        if (marketCap <= 0) return MarketCapCategory.LargeCap; // ETFs default to large
        if (marketCap < SMALL_CAP_MAX) return MarketCapCategory.SmallCap;
        if (marketCap < MID_CAP_MAX) return MarketCapCategory.MidCap;
        if (marketCap < LARGE_CAP_MAX) return MarketCapCategory.LargeCap;
        return MarketCapCategory.MegaCap;
    }

    private decimal CalculateActivityScore(UnusualOptionsActivity activity)
    {
        // Weighted score based on multiple factors
        var volumeScore = Math.Min(activity.VolumeChangePercent / 100, 10) * 40; // Max 400 points
        var ivScore = Math.Min(activity.ImpliedVolatility / 20, 5) * 20; // Max 100 points
        var absoluteVolumeScore = Math.Min(activity.TotalOptionsVolume / 100000m, 10) * 20; // Max 200 points

        // Bonus for extreme put/call ratios (strong directional bias)
        var biasScore = activity.Bias != DirectionalBias.Neutral ? 50 : 0;

        return volumeScore + ivScore + absoluteVolumeScore + biasScore;
    }

    private List<TopOptionContract> GenerateMockContracts(UnusualOptionsActivity activity)
    {
        var contracts = new List<TopOptionContract>();
        var random = new Random(activity.Symbol.GetHashCode());

        // Generate 5-8 top contracts
        var contractCount = random.Next(5, 9);
        var basePrice = activity.LastPrice;

        for (int i = 0; i < contractCount; i++)
        {
            var isCall = random.NextDouble() > (double)activity.PutCallRatio / 2;
            var strikeOffset = (decimal)(random.NextDouble() * 0.2 - 0.1); // -10% to +10% from current
            var strike = Math.Round(basePrice * (1 + strikeOffset), 0);

            // Expiration: 1 week to 2 months out
            var daysToExpiry = random.Next(7, 60);
            var expiry = DateTime.UtcNow.AddDays(daysToExpiry);
            expiry = new DateTime(expiry.Year, expiry.Month, expiry.Day, 0, 0, 0, DateTimeKind.Utc);

            // Find next Friday
            while (expiry.DayOfWeek != DayOfWeek.Friday)
                expiry = expiry.AddDays(1);

            var volume = random.Next(1000, 50000);
            var oi = random.Next(5000, 200000);
            var optionPrice = (decimal)(random.NextDouble() * 10 + 0.5);

            contracts.Add(new TopOptionContract
            {
                ContractSymbol = $"{activity.Symbol}{expiry:yyMMdd}{(isCall ? "C" : "P")}{strike:00000000}",
                OptionType = isCall ? "Call" : "Put",
                StrikePrice = strike,
                ExpirationDate = expiry,
                Volume = volume,
                OpenInterest = oi,
                ImpliedVolatility = activity.ImpliedVolatility + (decimal)(random.NextDouble() * 20 - 10),
                LastPrice = optionPrice,
                Bid = optionPrice - 0.05m,
                Ask = optionPrice + 0.05m,
                Delta = isCall ? (decimal)(0.3 + random.NextDouble() * 0.4) : (decimal)(-0.3 - random.NextDouble() * 0.4),
                ActivityType = random.NextDouble() > 0.5 ? "Opening" : "Closing",
                IsUnusual = volume > oi * 0.3,
                VolumeToOIRatio = oi > 0 ? (decimal)volume / oi : 0
            });
        }

        return contracts.OrderByDescending(c => c.Volume).ToList();
    }

    private string GenerateAiExplanation(UnusualOptionsActivity activity, List<TopOptionContract> contracts)
    {
        var volumeMultiple = activity.VolumeChangePercent / 100;
        var topContract = contracts.FirstOrDefault();

        var explanation = activity.Bias switch
        {
            DirectionalBias.Bullish => GenerateBullishExplanation(activity, volumeMultiple, topContract),
            DirectionalBias.Bearish => GenerateBearishExplanation(activity, volumeMultiple, topContract),
            _ => GenerateNeutralExplanation(activity, volumeMultiple)
        };

        return explanation;
    }

    private string GenerateBullishExplanation(UnusualOptionsActivity activity, decimal volumeMultiple, TopOptionContract? topContract)
    {
        var sb = new System.Text.StringBuilder();

        sb.Append($"**Unusual CALL buying detected in {activity.Symbol}.**\n\n");
        sb.Append($"Options volume is **{volumeMultiple:F1}x normal levels**, with call volume significantly ");
        sb.Append($"outpacing put volume (Put/Call ratio: {activity.PutCallRatio:F2}).\n\n");

        if (topContract != null)
        {
            sb.Append($"The most active contract is the **${topContract.StrikePrice} {topContract.OptionType}** ");
            sb.Append($"expiring on {topContract.ExpirationDate:MMM dd, yyyy}, ");
            sb.Append($"with {topContract.Volume:N0} contracts traded ");
            sb.Append($"({topContract.ActivityType.ToLower()} positions likely).\n\n");
        }

        sb.Append("**Interpretation:** This pattern often suggests traders are positioning for ");
        sb.Append("potential upside movement. Large call buying ahead of catalysts (earnings, ");
        sb.Append("product launches, or sector momentum) is typically a bullish signal.\n\n");

        if (activity.ImpliedVolatility > HIGH_IV_THRESHOLD)
        {
            sb.Append($"**Note:** Elevated IV ({activity.ImpliedVolatility:F1}%) indicates the market expects ");
            sb.Append("significant price movement. Premium is expensive.");
        }

        return sb.ToString();
    }

    private string GenerateBearishExplanation(UnusualOptionsActivity activity, decimal volumeMultiple, TopOptionContract? topContract)
    {
        var sb = new System.Text.StringBuilder();

        sb.Append($"**Unusual PUT buying detected in {activity.Symbol}.**\n\n");
        sb.Append($"Options volume is **{volumeMultiple:F1}x normal levels**, with put volume significantly ");
        sb.Append($"exceeding call volume (Put/Call ratio: {activity.PutCallRatio:F2}).\n\n");

        if (topContract != null)
        {
            sb.Append($"The most active contract is the **${topContract.StrikePrice} {topContract.OptionType}** ");
            sb.Append($"expiring on {topContract.ExpirationDate:MMM dd, yyyy}, ");
            sb.Append($"with {topContract.Volume:N0} contracts traded.\n\n");
        }

        sb.Append("**Interpretation:** Heavy put buying can indicate:\n");
        sb.Append("- Bearish speculation expecting downside\n");
        sb.Append("- Portfolio hedging by institutional investors\n");
        sb.Append("- Protection ahead of uncertain events\n\n");

        if (activity.ImpliedVolatility > HIGH_IV_THRESHOLD)
        {
            sb.Append($"**Caution:** High IV ({activity.ImpliedVolatility:F1}%) means put premiums are elevated. ");
            sb.Append("The stock needs to move significantly for puts to profit.");
        }

        return sb.ToString();
    }

    private string GenerateNeutralExplanation(UnusualOptionsActivity activity, decimal volumeMultiple)
    {
        var sb = new System.Text.StringBuilder();

        sb.Append($"**Elevated options activity in {activity.Symbol} without clear directional bias.**\n\n");
        sb.Append($"Options volume is **{volumeMultiple:F1}x normal levels**, ");
        sb.Append($"with relatively balanced call/put flow (ratio: {activity.PutCallRatio:F2}).\n\n");

        sb.Append("**Interpretation:** Balanced activity often indicates:\n");
        sb.Append("- Volatility plays (straddles/strangles) expecting a big move in either direction\n");
        sb.Append("- Market makers adjusting positions\n");
        sb.Append("- Mixed sentiment among institutional traders\n\n");

        sb.Append("Consider monitoring for a directional breakout as the catalyst approaches.");

        return sb.ToString();
    }

    private List<string> GeneratePotentialCatalysts(UnusualOptionsActivity activity)
    {
        var catalysts = new List<string>();
        var random = new Random(activity.Symbol.GetHashCode() + DateTime.UtcNow.DayOfYear);

        // Add some generic catalysts based on the stock
        var possibleCatalysts = new[]
        {
            "Upcoming earnings announcement",
            "FDA decision pending",
            "Product launch expected",
            "Merger/acquisition speculation",
            "Analyst day presentation",
            "Industry conference appearance",
            "Sector rotation momentum",
            "Technical breakout level",
            "Options expiration (OPEX) related",
            "Institutional rebalancing",
            "Short squeeze potential",
            "Macro event hedging"
        };

        // Pick 2-4 relevant catalysts
        var count = random.Next(2, 5);
        var selected = possibleCatalysts.OrderBy(_ => random.Next()).Take(count);
        catalysts.AddRange(selected);

        return catalysts;
    }

    #endregion
}

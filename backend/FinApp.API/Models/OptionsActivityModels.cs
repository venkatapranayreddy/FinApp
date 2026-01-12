namespace FinApp.API.Models;

/// <summary>
/// Raw options activity data from API
/// </summary>
public class OptionsActivityData
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal LastPrice { get; set; }
    public decimal PriceChange { get; set; }
    public decimal PriceChangePercent { get; set; }
    public long TotalOptionsVolume { get; set; }
    public long CallVolume { get; set; }
    public long PutVolume { get; set; }
    public long AvgOptionsVolume30Day { get; set; }
    public decimal ImpliedVolatility { get; set; }
    public long OpenInterest { get; set; }
    public decimal MarketCap { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Calculated unusual options activity with rankings
/// </summary>
public class UnusualOptionsActivity
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal LastPrice { get; set; }
    public decimal PriceChange { get; set; }
    public decimal PriceChangePercent { get; set; }

    // Volume Data
    public long TotalOptionsVolume { get; set; }
    public long AvgOptionsVolume30Day { get; set; }
    public long CallVolume { get; set; }
    public long PutVolume { get; set; }

    // Calculated Metrics
    public decimal VolumeChangePercent { get; set; } // (Today / 30-Day Avg) * 100
    public decimal PutCallRatio { get; set; }
    public decimal ImpliedVolatility { get; set; }
    public long OpenInterest { get; set; }

    // Directional Analysis
    public DirectionalBias Bias { get; set; }
    public string BiasLabel => Bias.ToString();

    // Classification
    public decimal MarketCap { get; set; }
    public MarketCapCategory CapCategory { get; set; }
    public string CapCategoryLabel => CapCategory.ToString().Replace("Cap", " Cap");

    // Activity Score (for ranking)
    public decimal ActivityScore { get; set; }

    public DateTime Timestamp { get; set; }
}

public enum DirectionalBias
{
    Bullish,
    Bearish,
    Neutral
}

public enum MarketCapCategory
{
    SmallCap,    // < 2B
    MidCap,      // 2B - 10B
    LargeCap,    // 10B - 200B
    MegaCap      // > 200B
}

/// <summary>
/// Detailed options activity for a specific symbol
/// </summary>
public class OptionsActivityDetails
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public UnusualOptionsActivity Summary { get; set; } = new();
    public List<TopOptionContract> TopContracts { get; set; } = new();
    public string AiExplanation { get; set; } = string.Empty;
    public List<string> PotentialCatalysts { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual option contract with unusual activity
/// </summary>
public class TopOptionContract
{
    public string ContractSymbol { get; set; } = string.Empty;
    public string OptionType { get; set; } = string.Empty; // Call or Put
    public decimal StrikePrice { get; set; }
    public DateTime ExpirationDate { get; set; }
    public long Volume { get; set; }
    public long OpenInterest { get; set; }
    public decimal ImpliedVolatility { get; set; }
    public decimal LastPrice { get; set; }
    public decimal Bid { get; set; }
    public decimal Ask { get; set; }
    public decimal Delta { get; set; }
    public string ActivityType { get; set; } = string.Empty; // Opening, Closing, Unknown
    public bool IsUnusual { get; set; }
    public decimal VolumeToOIRatio { get; set; }
}

/// <summary>
/// Filter options for the scanner
/// </summary>
public class OptionsActivityFilter
{
    public DirectionalBias? BiasFilter { get; set; }
    public bool? HighIVOnly { get; set; } // IV > 50%
    public MarketCapCategory? CapCategory { get; set; }
    public long MinVolume { get; set; } = 1000;
    public decimal MinVolumeChangePercent { get; set; } = 100; // At least 100% of average
    public string? SortBy { get; set; } = "VolumeChangePercent";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Paginated response for options scanner
/// </summary>
public class OptionsActivityResponse
{
    public List<UnusualOptionsActivity> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public DateTime LastUpdated { get; set; }
    public OptionsActivityStats Stats { get; set; } = new();
}

/// <summary>
/// Summary statistics for the scanner
/// </summary>
public class OptionsActivityStats
{
    public int TotalSymbols { get; set; }
    public int BullishCount { get; set; }
    public int BearishCount { get; set; }
    public int NeutralCount { get; set; }
    public decimal AvgVolumeChangePercent { get; set; }
    public string TopBullishSymbol { get; set; } = string.Empty;
    public string TopBearishSymbol { get; set; } = string.Empty;
}

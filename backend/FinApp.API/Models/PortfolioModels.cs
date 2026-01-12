namespace FinApp.API.Models;

/// <summary>
/// Represents a trade entry in the portfolio
/// </summary>
public class TradeEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Ticker { get; set; } = string.Empty;
    public int Shares { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal StopLossPrice { get; set; }
    public decimal TargetPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    public TradeStatus Status { get; set; } = TradeStatus.Open;
    public string? Notes { get; set; }
}

public enum TradeStatus
{
    Open,
    ClosedProfit,
    ClosedLoss,
    StopLossHit,
    TargetHit
}

/// <summary>
/// Request model for adding a new trade
/// </summary>
public class AddTradeRequest
{
    public string Ticker { get; set; } = string.Empty;
    public int Shares { get; set; }
    public decimal StopLossPrice { get; set; }
    public decimal TargetPrice { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Response model with calculated risk metrics
/// </summary>
public class TradeRiskAnalysis
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public int Shares { get; set; }

    // Prices
    public decimal EntryPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal StopLossPrice { get; set; }
    public decimal TargetPrice { get; set; }

    // Position Values
    public decimal PositionValue { get; set; }
    public decimal CurrentValue { get; set; }

    // Risk Calculations (3-5-7 Rule)
    public decimal RiskPerShare { get; set; }
    public decimal TotalRiskAmount { get; set; }
    public decimal RiskPercentOfPortfolio { get; set; }

    // Profit Calculations
    public decimal ProfitPerShare { get; set; }
    public decimal TotalProfitPotential { get; set; }
    public decimal ProfitPercentOfPortfolio { get; set; }

    // Current P&L
    public decimal UnrealizedPnL { get; set; }
    public decimal UnrealizedPnLPercent { get; set; }

    // Risk/Reward
    public decimal RiskRewardRatio { get; set; }

    // Status
    public string Status { get; set; } = "Open";
    public string StatusColor { get; set; } = "neutral"; // "green", "red", "neutral"

    // 3-5-7 Rule Compliance
    public bool IsWithin3PercentRule { get; set; }
    public string RuleViolation { get; set; } = string.Empty;

    // Dates
    public DateTime EntryDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Portfolio summary with 3-5-7 rule metrics
/// </summary>
public class PortfolioSummary
{
    public decimal TotalPortfolioValue { get; set; } = 100000m; // Default $100k
    public decimal CashBalance { get; set; }
    public decimal InvestedAmount { get; set; }

    // 3-5-7 Rule Metrics
    public decimal MaxRiskPerTrade { get; set; } // 3%
    public decimal MaxSectorExposure { get; set; } // 5%
    public decimal MaxTotalRisk { get; set; } // 7%

    public decimal CurrentTotalRisk { get; set; }
    public decimal CurrentTotalRiskPercent { get; set; }

    // Trade Stats
    public int TotalOpenTrades { get; set; }
    public int TradesInProfit { get; set; }
    public int TradesInLoss { get; set; }
    public decimal TotalUnrealizedPnL { get; set; }

    public List<TradeRiskAnalysis> Trades { get; set; } = new();
}

/// <summary>
/// Request to calculate position size based on risk
/// </summary>
public class PositionSizeRequest
{
    public string Ticker { get; set; } = string.Empty;
    public decimal StopLossPrice { get; set; }
    public decimal? RiskAmount { get; set; } // If null, use 3% of portfolio
    public decimal PortfolioValue { get; set; } = 100000m;
}

/// <summary>
/// Response with calculated position size
/// </summary>
public class PositionSizeResponse
{
    public string Ticker { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal StopLossPrice { get; set; }
    public decimal RiskPerShare { get; set; }
    public decimal MaxRiskAmount { get; set; }
    public int RecommendedShares { get; set; }
    public decimal PositionValue { get; set; }
    public decimal ActualRiskAmount { get; set; }
    public decimal RiskPercentOfPortfolio { get; set; }
}

/// <summary>
/// Ticker price with company info
/// </summary>
public class TickerPriceInfo
{
    public string Ticker { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Exchange { get; set; }
    public decimal? MarketCap { get; set; }
}

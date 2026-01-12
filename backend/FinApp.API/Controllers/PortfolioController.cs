using FinApp.API.Models;
using FinApp.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly IMassiveApiService _massiveApi;
    private readonly ILogger<PortfolioController> _logger;

    // In-memory storage for trades (in production, use a database)
    private static readonly List<TradeEntry> _trades = new();
    private static decimal _portfolioValue = 100000m;

    // 3-5-7 Rule Constants
    private const decimal MAX_RISK_PER_TRADE_PERCENT = 0.03m; // 3%
    private const decimal MAX_SECTOR_EXPOSURE_PERCENT = 0.05m; // 5%
    private const decimal MAX_TOTAL_RISK_PERCENT = 0.07m; // 7%

    public PortfolioController(IMassiveApiService massiveApi, ILogger<PortfolioController> logger)
    {
        _massiveApi = massiveApi;
        _logger = logger;
    }

    /// <summary>
    /// Get current price and company info for a ticker
    /// </summary>
    [HttpGet("price/{ticker}")]
    public async Task<ActionResult<TickerPriceInfo>> GetCurrentPrice(string ticker)
    {
        try
        {
            var upperTicker = ticker.ToUpper();

            // Fetch price and company details in parallel for speed
            var priceTask = GetLatestPriceAsync(upperTicker);
            var detailsTask = _massiveApi.GetTickerDetailsAsync(upperTicker);

            await Task.WhenAll(priceTask, detailsTask);

            var price = priceTask.Result;
            var details = detailsTask.Result;

            if (price == null)
            {
                return NotFound(new { error = $"Price not found for {ticker}" });
            }

            return Ok(new TickerPriceInfo
            {
                Ticker = upperTicker,
                CompanyName = details?.Name ?? upperTicker,
                Price = price.Value,
                Exchange = details?.PrimaryExchange,
                MarketCap = details?.MarketCap
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching price for {Ticker}", ticker);
            return StatusCode(500, new { error = "Failed to fetch price" });
        }
    }

    /// <summary>
    /// Calculate position size based on 3% risk rule
    /// </summary>
    [HttpPost("calculate-position")]
    public async Task<ActionResult<PositionSizeResponse>> CalculatePositionSize([FromBody] PositionSizeRequest request)
    {
        try
        {
            var currentPrice = await GetLatestPriceAsync(request.Ticker.ToUpper());
            if (currentPrice == null)
            {
                return NotFound(new { error = $"Price not found for {request.Ticker}" });
            }

            var riskPerShare = currentPrice.Value - request.StopLossPrice;
            if (riskPerShare <= 0)
            {
                return BadRequest(new { error = "Stop loss must be below current price for long positions" });
            }

            var maxRiskAmount = request.RiskAmount ?? (request.PortfolioValue * MAX_RISK_PER_TRADE_PERCENT);
            var recommendedShares = (int)Math.Floor(maxRiskAmount / riskPerShare);
            var positionValue = recommendedShares * currentPrice.Value;
            var actualRiskAmount = recommendedShares * riskPerShare;

            return Ok(new PositionSizeResponse
            {
                Ticker = request.Ticker.ToUpper(),
                CurrentPrice = currentPrice.Value,
                StopLossPrice = request.StopLossPrice,
                RiskPerShare = riskPerShare,
                MaxRiskAmount = maxRiskAmount,
                RecommendedShares = recommendedShares,
                PositionValue = positionValue,
                ActualRiskAmount = actualRiskAmount,
                RiskPercentOfPortfolio = (actualRiskAmount / request.PortfolioValue) * 100
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating position size for {Ticker}", request.Ticker);
            return StatusCode(500, new { error = "Failed to calculate position size" });
        }
    }

    /// <summary>
    /// Add a new trade to the journal
    /// </summary>
    [HttpPost("trades")]
    public async Task<ActionResult<TradeRiskAnalysis>> AddTrade([FromBody] AddTradeRequest request)
    {
        try
        {
            var currentPrice = await GetLatestPriceAsync(request.Ticker.ToUpper());
            if (currentPrice == null)
            {
                return NotFound(new { error = $"Price not found for {request.Ticker}" });
            }

            var trade = new TradeEntry
            {
                Ticker = request.Ticker.ToUpper(),
                Shares = request.Shares,
                EntryPrice = currentPrice.Value,
                CurrentPrice = currentPrice.Value,
                StopLossPrice = request.StopLossPrice,
                TargetPrice = request.TargetPrice,
                Notes = request.Notes,
                Status = TradeStatus.Open
            };

            _trades.Add(trade);

            var analysis = CalculateTradeRiskAnalysis(trade, currentPrice.Value);
            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding trade for {Ticker}", request.Ticker);
            return StatusCode(500, new { error = "Failed to add trade" });
        }
    }

    /// <summary>
    /// Get all trades with updated prices and risk analysis
    /// </summary>
    [HttpGet("trades")]
    public async Task<ActionResult<PortfolioSummary>> GetTrades()
    {
        try
        {
            var analyses = new List<TradeRiskAnalysis>();
            decimal totalRisk = 0;
            decimal totalUnrealizedPnL = 0;
            int tradesInProfit = 0;
            int tradesInLoss = 0;

            foreach (var trade in _trades.Where(t => t.Status == TradeStatus.Open))
            {
                var currentPrice = await GetLatestPriceAsync(trade.Ticker);
                trade.CurrentPrice = currentPrice ?? trade.EntryPrice;

                var analysis = CalculateTradeRiskAnalysis(trade, trade.CurrentPrice);
                analyses.Add(analysis);

                totalRisk += analysis.TotalRiskAmount;
                totalUnrealizedPnL += analysis.UnrealizedPnL;

                if (analysis.UnrealizedPnL > 0) tradesInProfit++;
                else if (analysis.UnrealizedPnL < 0) tradesInLoss++;
            }

            var investedAmount = _trades.Where(t => t.Status == TradeStatus.Open)
                .Sum(t => t.Shares * t.EntryPrice);

            return Ok(new PortfolioSummary
            {
                TotalPortfolioValue = _portfolioValue,
                CashBalance = _portfolioValue - investedAmount,
                InvestedAmount = investedAmount,
                MaxRiskPerTrade = _portfolioValue * MAX_RISK_PER_TRADE_PERCENT,
                MaxSectorExposure = _portfolioValue * MAX_SECTOR_EXPOSURE_PERCENT,
                MaxTotalRisk = _portfolioValue * MAX_TOTAL_RISK_PERCENT,
                CurrentTotalRisk = totalRisk,
                CurrentTotalRiskPercent = (totalRisk / _portfolioValue) * 100,
                TotalOpenTrades = _trades.Count(t => t.Status == TradeStatus.Open),
                TradesInProfit = tradesInProfit,
                TradesInLoss = tradesInLoss,
                TotalUnrealizedPnL = totalUnrealizedPnL,
                Trades = analyses.OrderByDescending(t => t.EntryDate).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching trades");
            return StatusCode(500, new { error = "Failed to fetch trades" });
        }
    }

    /// <summary>
    /// Get a single trade with risk analysis
    /// </summary>
    [HttpGet("trades/{id}")]
    public async Task<ActionResult<TradeRiskAnalysis>> GetTrade(Guid id)
    {
        try
        {
            var trade = _trades.FirstOrDefault(t => t.Id == id);
            if (trade == null)
            {
                return NotFound(new { error = "Trade not found" });
            }

            var currentPrice = await GetLatestPriceAsync(trade.Ticker);
            trade.CurrentPrice = currentPrice ?? trade.EntryPrice;

            var analysis = CalculateTradeRiskAnalysis(trade, trade.CurrentPrice);
            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching trade {Id}", id);
            return StatusCode(500, new { error = "Failed to fetch trade" });
        }
    }

    /// <summary>
    /// Close a trade
    /// </summary>
    [HttpPut("trades/{id}/close")]
    public ActionResult<TradeRiskAnalysis> CloseTrade(Guid id, [FromQuery] decimal closePrice)
    {
        try
        {
            var trade = _trades.FirstOrDefault(t => t.Id == id);
            if (trade == null)
            {
                return NotFound(new { error = "Trade not found" });
            }

            trade.CurrentPrice = closePrice;

            if (closePrice <= trade.StopLossPrice)
            {
                trade.Status = TradeStatus.StopLossHit;
            }
            else if (closePrice >= trade.TargetPrice)
            {
                trade.Status = TradeStatus.TargetHit;
            }
            else if (closePrice > trade.EntryPrice)
            {
                trade.Status = TradeStatus.ClosedProfit;
            }
            else
            {
                trade.Status = TradeStatus.ClosedLoss;
            }

            var analysis = CalculateTradeRiskAnalysis(trade, closePrice);
            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing trade {Id}", id);
            return StatusCode(500, new { error = "Failed to close trade" });
        }
    }

    /// <summary>
    /// Delete a trade
    /// </summary>
    [HttpDelete("trades/{id}")]
    public ActionResult DeleteTrade(Guid id)
    {
        var trade = _trades.FirstOrDefault(t => t.Id == id);
        if (trade == null)
        {
            return NotFound(new { error = "Trade not found" });
        }

        _trades.Remove(trade);
        return Ok(new { message = "Trade deleted successfully" });
    }

    /// <summary>
    /// Update portfolio value
    /// </summary>
    [HttpPut("settings")]
    public ActionResult UpdatePortfolioSettings([FromBody] PortfolioSettingsRequest request)
    {
        _portfolioValue = request.PortfolioValue;
        return Ok(new { message = "Portfolio settings updated", portfolioValue = _portfolioValue });
    }

    #region Private Helpers

    private async Task<decimal?> GetLatestPriceAsync(string ticker)
    {
        try
        {
            // Get last 5 days of data to ensure we get a price
            var to = DateTime.UtcNow.Date;
            var from = to.AddDays(-5);

            var aggs = await _massiveApi.GetAggregatesAsync(ticker, from, to);
            if (aggs?.Results != null && aggs.Results.Count > 0)
            {
                // Return the most recent closing price
                var latest = aggs.Results.OrderByDescending(r => r.Timestamp).First();
                return latest.Close;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching latest price for {Ticker}", ticker);
            return null;
        }
    }

    private TradeRiskAnalysis CalculateTradeRiskAnalysis(TradeEntry trade, decimal currentPrice)
    {
        var riskPerShare = trade.EntryPrice - trade.StopLossPrice;
        var profitPerShare = trade.TargetPrice - trade.EntryPrice;
        var totalRiskAmount = riskPerShare * trade.Shares;
        var totalProfitPotential = profitPerShare * trade.Shares;
        var unrealizedPnL = (currentPrice - trade.EntryPrice) * trade.Shares;
        var unrealizedPnLPercent = trade.EntryPrice > 0
            ? ((currentPrice - trade.EntryPrice) / trade.EntryPrice) * 100
            : 0;

        var riskRewardRatio = riskPerShare > 0 ? profitPerShare / riskPerShare : 0;
        var riskPercentOfPortfolio = (totalRiskAmount / _portfolioValue) * 100;

        // Determine status color
        string statusColor = "neutral";
        string status = trade.Status.ToString();

        if (trade.Status == TradeStatus.Open)
        {
            if (currentPrice <= trade.StopLossPrice)
            {
                statusColor = "red";
                status = "Stop Loss Hit";
            }
            else if (currentPrice >= trade.TargetPrice)
            {
                statusColor = "green";
                status = "Target Reached";
            }
            else if (unrealizedPnL < 0)
            {
                statusColor = "red";
                status = "In Loss";
            }
            else if (unrealizedPnL > 0)
            {
                statusColor = "green";
                status = "In Profit";
            }
        }
        else if (trade.Status == TradeStatus.ClosedProfit || trade.Status == TradeStatus.TargetHit)
        {
            statusColor = "green";
        }
        else if (trade.Status == TradeStatus.ClosedLoss || trade.Status == TradeStatus.StopLossHit)
        {
            statusColor = "red";
        }

        // Check 3% rule compliance
        var isWithin3PercentRule = riskPercentOfPortfolio <= (MAX_RISK_PER_TRADE_PERCENT * 100);
        var ruleViolation = "";
        if (!isWithin3PercentRule)
        {
            ruleViolation = $"Risk exceeds 3% rule: {riskPercentOfPortfolio:F2}% > 3%";
        }

        return new TradeRiskAnalysis
        {
            Id = trade.Id,
            Ticker = trade.Ticker,
            Shares = trade.Shares,
            EntryPrice = trade.EntryPrice,
            CurrentPrice = currentPrice,
            StopLossPrice = trade.StopLossPrice,
            TargetPrice = trade.TargetPrice,
            PositionValue = trade.Shares * trade.EntryPrice,
            CurrentValue = trade.Shares * currentPrice,
            RiskPerShare = riskPerShare,
            TotalRiskAmount = totalRiskAmount,
            RiskPercentOfPortfolio = riskPercentOfPortfolio,
            ProfitPerShare = profitPerShare,
            TotalProfitPotential = totalProfitPotential,
            ProfitPercentOfPortfolio = (totalProfitPotential / _portfolioValue) * 100,
            UnrealizedPnL = unrealizedPnL,
            UnrealizedPnLPercent = unrealizedPnLPercent,
            RiskRewardRatio = riskRewardRatio,
            Status = status,
            StatusColor = statusColor,
            IsWithin3PercentRule = isWithin3PercentRule,
            RuleViolation = ruleViolation,
            EntryDate = trade.EntryDate,
            Notes = trade.Notes
        };
    }

    #endregion
}

public class PortfolioSettingsRequest
{
    public decimal PortfolioValue { get; set; } = 100000m;
}

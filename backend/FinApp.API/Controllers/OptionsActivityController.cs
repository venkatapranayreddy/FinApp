using FinApp.API.Models;
using FinApp.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OptionsActivityController : ControllerBase
{
    private readonly IOptionsActivityService _optionsService;
    private readonly ILogger<OptionsActivityController> _logger;

    public OptionsActivityController(
        IOptionsActivityService optionsService,
        ILogger<OptionsActivityController> logger)
    {
        _optionsService = optionsService;
        _logger = logger;
    }

    /// <summary>
    /// Get unusual options activity scanner data with filtering and pagination
    /// </summary>
    [HttpGet("scanner")]
    public async Task<ActionResult<OptionsActivityResponse>> GetUnusualActivity(
        [FromQuery] string? bias = null,
        [FromQuery] bool? highIV = null,
        [FromQuery] string? capCategory = null,
        [FromQuery] long minVolume = 1000,
        [FromQuery] decimal minVolumeChange = 100,
        [FromQuery] string sortBy = "VolumeChangePercent",
        [FromQuery] bool sortDesc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var filter = new OptionsActivityFilter
            {
                BiasFilter = ParseBias(bias),
                HighIVOnly = highIV,
                CapCategory = ParseCapCategory(capCategory),
                MinVolume = minVolume,
                MinVolumeChangePercent = minVolumeChange,
                SortBy = sortBy,
                SortDescending = sortDesc,
                Page = Math.Max(1, page),
                PageSize = Math.Clamp(pageSize, 10, 100)
            };

            var result = await _optionsService.GetUnusualActivityAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching unusual options activity");
            return StatusCode(500, new { error = "Failed to fetch options activity data" });
        }
    }

    /// <summary>
    /// Get top movers with highest unusual activity
    /// </summary>
    [HttpGet("top-movers")]
    public async Task<ActionResult<List<UnusualOptionsActivity>>> GetTopMovers([FromQuery] int count = 10)
    {
        try
        {
            var result = await _optionsService.GetTopMoversAsync(Math.Clamp(count, 5, 25));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching top movers");
            return StatusCode(500, new { error = "Failed to fetch top movers" });
        }
    }

    /// <summary>
    /// Get detailed options activity for a specific symbol
    /// </summary>
    [HttpGet("details/{symbol}")]
    public async Task<ActionResult<OptionsActivityDetails>> GetActivityDetails(string symbol)
    {
        try
        {
            var result = await _optionsService.GetActivityDetailsAsync(symbol.ToUpper());
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Symbol {symbol} not found in options activity data" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching options details for {Symbol}", symbol);
            return StatusCode(500, new { error = "Failed to fetch options details" });
        }
    }

    /// <summary>
    /// Get summary statistics for the scanner
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<OptionsActivityStats>> GetStats()
    {
        try
        {
            var result = await _optionsService.GetUnusualActivityAsync(new OptionsActivityFilter
            {
                Page = 1,
                PageSize = 1000
            });
            return Ok(result.Stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching options stats");
            return StatusCode(500, new { error = "Failed to fetch statistics" });
        }
    }

    #region Private Helpers

    private DirectionalBias? ParseBias(string? bias)
    {
        if (string.IsNullOrEmpty(bias)) return null;
        return bias.ToLower() switch
        {
            "bullish" => DirectionalBias.Bullish,
            "bearish" => DirectionalBias.Bearish,
            "neutral" => DirectionalBias.Neutral,
            _ => null
        };
    }

    private MarketCapCategory? ParseCapCategory(string? category)
    {
        if (string.IsNullOrEmpty(category)) return null;
        return category.ToLower() switch
        {
            "small" or "smallcap" => MarketCapCategory.SmallCap,
            "mid" or "midcap" => MarketCapCategory.MidCap,
            "large" or "largecap" => MarketCapCategory.LargeCap,
            "mega" or "megacap" => MarketCapCategory.MegaCap,
            _ => null
        };
    }

    #endregion
}

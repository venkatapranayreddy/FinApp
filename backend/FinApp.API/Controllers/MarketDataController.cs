using FinApp.API.Models;
using FinApp.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketDataController : ControllerBase
{
    private readonly IMassiveApiService _massiveApi;
    private readonly ILogger<MarketDataController> _logger;

    public MarketDataController(IMassiveApiService massiveApi, ILogger<MarketDataController> logger)
    {
        _massiveApi = massiveApi;
        _logger = logger;
    }

    #region Tickers

    [HttpGet("tickers")]
    public async Task<ActionResult<object>> GetTickers(
        [FromQuery] string exchange = "XNAS",
        [FromQuery] int limit = 10)
    {
        try
        {
            var tickers = await _massiveApi.GetTickersAsync(exchange, limit);
            return Ok(new { data = tickers, count = tickers.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tickers");
            return StatusCode(500, new { error = "Failed to fetch tickers" });
        }
    }

    #endregion

    #region Technical Indicators

    [HttpGet("indicators/sma/{ticker}")]
    public async Task<ActionResult<object>> GetSma(
        string ticker,
        [FromQuery] int window = 20,
        [FromQuery] string timespan = "day",
        [FromQuery] int limit = 10)
    {
        try
        {
            var sma = await _massiveApi.GetSmaAsync(ticker.ToUpper(), window, timespan, limit);
            if (sma?.Results == null)
            {
                return Ok(new
                {
                    data = new List<object>(),
                    count = 0,
                    indicator = "SMA",
                    window,
                    timespan
                });
            }
            return Ok(new
            {
                data = sma.Results.Values,
                count = sma.Results.Values?.Count ?? 0,
                indicator = "SMA",
                window,
                timespan
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching SMA for {Ticker}", ticker);
            return StatusCode(500, new { error = "Failed to fetch SMA" });
        }
    }

    [HttpGet("indicators/ema/{ticker}")]
    public async Task<ActionResult<object>> GetEma(
        string ticker,
        [FromQuery] int window = 20,
        [FromQuery] string timespan = "day",
        [FromQuery] int limit = 10)
    {
        try
        {
            var ema = await _massiveApi.GetEmaAsync(ticker.ToUpper(), window, timespan, limit);
            if (ema?.Results == null)
            {
                return Ok(new
                {
                    data = new List<object>(),
                    count = 0,
                    indicator = "EMA",
                    window,
                    timespan
                });
            }
            return Ok(new
            {
                data = ema.Results.Values,
                count = ema.Results.Values?.Count ?? 0,
                indicator = "EMA",
                window,
                timespan
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching EMA for {Ticker}", ticker);
            return StatusCode(500, new { error = "Failed to fetch EMA" });
        }
    }

    [HttpGet("indicators/macd/{ticker}")]
    public async Task<ActionResult<object>> GetMacd(
        string ticker,
        [FromQuery] string timespan = "day",
        [FromQuery] int limit = 10)
    {
        try
        {
            var macd = await _massiveApi.GetMacdAsync(ticker.ToUpper(), timespan, limit);
            if (macd?.Results == null)
            {
                return Ok(new
                {
                    data = new List<object>(),
                    count = 0,
                    indicator = "MACD",
                    timespan
                });
            }
            return Ok(new
            {
                data = macd.Results.Values,
                count = macd.Results.Values?.Count ?? 0,
                indicator = "MACD",
                timespan
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching MACD for {Ticker}", ticker);
            return StatusCode(500, new { error = "Failed to fetch MACD" });
        }
    }

    [HttpGet("indicators/rsi/{ticker}")]
    public async Task<ActionResult<object>> GetRsi(
        string ticker,
        [FromQuery] int window = 14,
        [FromQuery] string timespan = "day",
        [FromQuery] int limit = 10)
    {
        try
        {
            var rsi = await _massiveApi.GetRsiAsync(ticker.ToUpper(), window, timespan, limit);
            if (rsi?.Results == null)
            {
                return Ok(new
                {
                    data = new List<object>(),
                    count = 0,
                    indicator = "RSI",
                    window,
                    timespan
                });
            }
            return Ok(new
            {
                data = rsi.Results.Values,
                count = rsi.Results.Values?.Count ?? 0,
                indicator = "RSI",
                window,
                timespan
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching RSI for {Ticker}", ticker);
            return StatusCode(500, new { error = "Failed to fetch RSI" });
        }
    }

    #endregion

    #region Aggregates

    [HttpGet("aggregates/{ticker}")]
    public async Task<ActionResult<object>> GetAggregates(
        string ticker,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        try
        {
            // Ensure 'to' date is not in the future
            var today = DateTime.UtcNow.Date;
            if (to > today)
            {
                to = today;
            }

            if (from >= to)
            {
                return BadRequest(new { error = "From date must be before to date" });
            }

            _logger.LogInformation("Fetching aggregates for {Ticker} from {From} to {To}", ticker, from, to);

            var aggs = await _massiveApi.GetAggregatesAsync(ticker.ToUpper(), from, to);
            if (aggs?.Results == null || aggs.Results.Count == 0)
            {
                _logger.LogWarning("No aggregates data returned for {Ticker} from {From} to {To}", ticker, from, to);
                return Ok(new
                {
                    data = new List<object>(),
                    count = 0,
                    ticker = ticker.ToUpper(),
                    message = "No data available for the specified date range"
                });
            }
            return Ok(new
            {
                data = aggs.Results,
                count = aggs.ResultsCount,
                ticker = aggs.Ticker
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching aggregates for {Ticker}", ticker);
            return StatusCode(500, new { error = "Failed to fetch aggregates" });
        }
    }

    #endregion
}

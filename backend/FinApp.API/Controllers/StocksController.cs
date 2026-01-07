using FinApp.API.Models;
using FinApp.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StocksController : ControllerBase
{
    private readonly IStockService _stockService;
    private readonly ILogger<StocksController> _logger;

    public StocksController(IStockService stockService, ILogger<StocksController> logger)
    {
        _stockService = stockService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetStocks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? exchange = null)
    {
        try
        {
            var stocks = await _stockService.GetAllStocksAsync(page, pageSize, exchange);
            var totalCount = await _stockService.GetStockCountAsync(exchange);

            // Convert to DTOs to avoid serialization issues with Supabase BaseModel
            var stockDtos = stocks.Select(StockDto.FromStock).ToList();

            return Ok(new
            {
                data = stockDtos,
                totalCount,
                page,
                pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stocks");
            return StatusCode(500, new { error = "Failed to fetch stocks" });
        }
    }

    [HttpGet("sync")]
    public async Task<ActionResult<object>> SyncStocks()
    {
        try
        {
            _logger.LogInformation("Starting stock sync...");
            var count = await _stockService.SyncStocksAsync();

            return Ok(new
            {
                message = "Sync completed",
                syncedCount = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing stocks");
            return StatusCode(500, new { error = "Failed to sync stocks" });
        }
    }

    [HttpPost("performance")]
    public async Task<ActionResult<StockPerformanceResponse>> GetPerformance([FromBody] FilterRequest filter)
    {
        try
        {
            if (filter.StartDate >= filter.EndDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            var result = await _stockService.GetStockPerformanceAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating stock performance");
            return StatusCode(500, new { error = "Failed to calculate performance" });
        }
    }
}

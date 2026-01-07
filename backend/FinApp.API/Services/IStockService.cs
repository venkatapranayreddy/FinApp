using FinApp.API.Models;

namespace FinApp.API.Services;

public interface IStockService
{
    Task<List<Stock>> GetAllStocksAsync(int page = 1, int pageSize = 50, string? exchange = null);
    Task<int> GetStockCountAsync(string? exchange = null);
    Task<int> SyncStocksAsync();
    Task<StockPerformanceResponse> GetStockPerformanceAsync(FilterRequest filter);
}

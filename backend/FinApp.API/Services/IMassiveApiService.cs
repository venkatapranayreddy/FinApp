using FinApp.API.Models;

namespace FinApp.API.Services;

public interface IMassiveApiService
{
    Task<List<MassiveTicker>> GetAllTickersAsync(string exchange);
    Task<MassiveAggsResponse?> GetAggregatesAsync(string ticker, DateTime from, DateTime to);
}

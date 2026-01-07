using System.Net;
using FinApp.API.Models;
using Newtonsoft.Json;

namespace FinApp.API.Services;

public class MassiveApiService : IMassiveApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<MassiveApiService> _logger;
    private const string BaseUrl = "https://api.massive.com/v3";
    private const string BaseUrlV2 = "https://api.massive.com/v2";
    private const int RateLimitDelayMs = 100; // Fast mode
    private const int MaxRetries = 1; // Don't retry, just skip

    public MassiveApiService(IConfiguration configuration, ILogger<MassiveApiService> logger)
    {
        // Check environment variable first, then fall back to config
        _apiKey = Environment.GetEnvironmentVariable("MASSIVE_API_KEY")
            ?? configuration["Massive:ApiKey"]
            ?? throw new ArgumentNullException("Massive:ApiKey configuration is missing");
        _logger = logger;

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<List<MassiveTicker>> GetAllTickersAsync(string exchange)
    {
        var allTickers = new List<MassiveTicker>();
        var url = $"{BaseUrl}/reference/tickers?market=stocks&exchange={exchange}&active=true&limit=1000";

        while (!string.IsNullOrEmpty(url))
        {
            var response = await SendRequestWithRetryAsync(url);

            if (response == null)
            {
                _logger.LogWarning("Failed to fetch tickers after retries, returning {Count} tickers collected so far", allTickers.Count);
                break;
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<MassiveTickersResponse>(content);

            if (result?.Results != null)
            {
                allTickers.AddRange(result.Results);
                _logger.LogInformation("Fetched {Count} tickers, total: {Total}", result.Results.Count, allTickers.Count);
            }

            url = result?.NextUrl;
            if (!string.IsNullOrEmpty(url) && !url.Contains("apiKey"))
            {
                url = $"{url}&apiKey={_apiKey}";
            }

            // Rate limiting delay
            await Task.Delay(RateLimitDelayMs);
        }

        return allTickers;
    }

    public async Task<MassiveAggsResponse?> GetAggregatesAsync(string ticker, DateTime from, DateTime to)
    {
        var fromStr = from.ToString("yyyy-MM-dd");
        var toStr = to.ToString("yyyy-MM-dd");
        // Aggregates endpoint uses v2 API
        var url = $"{BaseUrlV2}/aggs/ticker/{ticker}/range/1/day/{fromStr}/{toStr}?adjusted=true&sort=asc";

        var response = await SendRequestWithRetryAsync(url);

        if (response == null)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<MassiveAggsResponse>(content);
    }

    private async Task<HttpResponseMessage?> SendRequestWithRetryAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);

            // Skip rate limited requests immediately
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogDebug("Rate limited, skipping: {Url}", url);
                return null;
            }

            response.EnsureSuccessStatusCode();
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Request failed, skipping: {Url}", url);
            return null;
        }
    }
}

using System.Net;
using FinApp.API.Models;
using Newtonsoft.Json;

namespace FinApp.API.Services;

public class MassiveApiService : IMassiveApiService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly bool _isEnabled;
    private readonly ILogger<MassiveApiService> _logger;
    private const string BaseUrl = "https://api.polygon.io/v3";
    private const string BaseUrlV2 = "https://api.polygon.io/v2";
    private const string BaseUrlV1 = "https://api.polygon.io/v1";
    private const int RateLimitDelayMs = 100;
    private const int DefaultTickerLimit = 10; // Limit to 10 tickers per API call

    public MassiveApiService(IConfiguration configuration, ILogger<MassiveApiService> logger)
    {
        _apiKey = Environment.GetEnvironmentVariable("MASSIVE_API_KEY")
            ?? configuration["Massive:ApiKey"];
        _logger = logger;
        _isEnabled = !string.IsNullOrEmpty(_apiKey);

        if (!_isEnabled)
        {
            _logger.LogWarning("Polygon API key not configured. Market data features will return empty results.");
        }

        _httpClient = new HttpClient();
    }

    private string AddApiKey(string url)
    {
        if (url.Contains("apiKey="))
        {
            return url;
        }
        var separator = url.Contains("?") ? "&" : "?";
        return $"{url}{separator}apiKey={_apiKey}";
    }

    #region Tickers

    public async Task<List<MassiveTicker>> GetAllTickersAsync(string exchange)
    {
        var allTickers = new List<MassiveTicker>();
        var url = $"{BaseUrl}/reference/tickers?market=stocks&exchange={exchange}&active=true&limit={DefaultTickerLimit}";

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
                _logger.LogInformation("Fetched {Count} tickers (batch of {Limit}), total: {Total}",
                    result.Results.Count, DefaultTickerLimit, allTickers.Count);
            }

            url = result?.NextUrl;

            await Task.Delay(RateLimitDelayMs);
        }

        return allTickers;
    }

    public async Task<List<MassiveTicker>> GetTickersAsync(string exchange, int limit = 10)
    {
        var safeLimit = Math.Min(limit, DefaultTickerLimit);
        var url = $"{BaseUrl}/reference/tickers?market=stocks&exchange={exchange}&active=true&limit={safeLimit}";

        var response = await SendRequestWithRetryAsync(url);
        if (response == null) return new List<MassiveTicker>();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<MassiveTickersResponse>(content);

        _logger.LogInformation("Fetched {Count} tickers from {Exchange}", result?.Results?.Count ?? 0, exchange);
        return result?.Results ?? new List<MassiveTicker>();
    }

    #endregion

    #region Ticker Details

    public async Task<TickerDetailsResponse?> GetTickerDetailsAsync(string ticker)
    {
        var url = $"{BaseUrl}/reference/tickers/{ticker.ToUpper()}";

        var response = await SendRequestWithRetryAsync(url);
        if (response == null) return null;

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<TickerDetailsApiResponse>(content);

        _logger.LogInformation("Fetched details for {Ticker}: {Name}", ticker, result?.Results?.Name);
        return result?.Results;
    }

    #endregion

    #region Aggregates

    public async Task<MassiveAggsResponse?> GetAggregatesAsync(string ticker, DateTime from, DateTime to)
    {
        var fromStr = from.ToString("yyyy-MM-dd");
        var toStr = to.ToString("yyyy-MM-dd");
        var url = $"{BaseUrlV2}/aggs/ticker/{ticker}/range/1/day/{fromStr}/{toStr}?adjusted=true&sort=asc";

        var response = await SendRequestWithRetryAsync(url);
        if (response == null) return null;

        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<MassiveAggsResponse>(content);
    }

    #endregion

    #region Snapshots

    public async Task<MassiveTickerSnapshot?> GetSnapshotAsync(string ticker)
    {
        var url = $"{BaseUrlV2}/snapshot/locale/us/markets/stocks/tickers/{ticker}";

        var response = await SendRequestWithRetryAsync(url);
        if (response == null) return null;

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<MassiveSnapshotResponse>(content);

        _logger.LogInformation("Fetched snapshot for {Ticker}", ticker);
        return result?.Ticker;
    }

    public async Task<List<MassiveTickerSnapshot>> GetSnapshotsAsync(IEnumerable<string> tickers)
    {
        var allSnapshots = new List<MassiveTickerSnapshot>();
        var tickerList = tickers.ToList();

        // Batch tickers into groups of 10
        var batches = tickerList
            .Select((ticker, index) => new { ticker, index })
            .GroupBy(x => x.index / DefaultTickerLimit)
            .Select(g => g.Select(x => x.ticker).ToList())
            .ToList();

        _logger.LogInformation("Fetching snapshots for {Total} tickers in {BatchCount} batches of {BatchSize}",
            tickerList.Count, batches.Count, DefaultTickerLimit);

        foreach (var batch in batches)
        {
            var tickersParam = string.Join(",", batch);
            var url = $"{BaseUrlV2}/snapshot/locale/us/markets/stocks/tickers?tickers={tickersParam}";

            var response = await SendRequestWithRetryAsync(url);
            if (response == null)
            {
                _logger.LogWarning("Failed to fetch snapshots for batch: {Tickers}", tickersParam);
                continue;
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<MassiveFullMarketSnapshotResponse>(content);

            if (result?.Tickers != null)
            {
                allSnapshots.AddRange(result.Tickers);
                _logger.LogInformation("Fetched {Count} snapshots in batch", result.Tickers.Count);
            }

            await Task.Delay(RateLimitDelayMs);
        }

        return allSnapshots;
    }

    public async Task<List<MassiveTickerSnapshot>> GetTopMoversAsync(string direction = "gainers")
    {
        var url = $"{BaseUrlV2}/snapshot/locale/us/markets/stocks/{direction}";

        var response = await SendRequestWithRetryAsync(url);
        if (response == null) return new List<MassiveTickerSnapshot>();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<MassiveTopMoversResponse>(content);

        _logger.LogInformation("Fetched {Count} top {Direction}", result?.Tickers?.Count ?? 0, direction);
        return result?.Tickers ?? new List<MassiveTickerSnapshot>();
    }

    #endregion

    #region Trades & Quotes

    public async Task<MassiveTradesResponse?> GetTradesAsync(string ticker, string? timestamp = null, int limit = 10)
    {
        var safeLimit = Math.Min(limit, DefaultTickerLimit);
        var url = $"{BaseUrl}/trades/{ticker}?limit={safeLimit}";

        if (!string.IsNullOrEmpty(timestamp))
        {
            url += $"&timestamp={timestamp}";
        }

        var response = await SendRequestWithRetryAsync(url);
        if (response == null) return null;

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<MassiveTradesResponse>(content);

        _logger.LogInformation("Fetched {Count} trades for {Ticker}", result?.Results?.Count ?? 0, ticker);
        return result;
    }

    public async Task<MassiveLastTrade?> GetLastTradeAsync(string ticker)
    {
        var url = $"{BaseUrlV2}/last/trade/{ticker}";

        var response = await SendRequestWithRetryAsync(url);
        if (response == null) return null;

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<MassiveLastTradeResponse>(content);

        _logger.LogInformation("Fetched last trade for {Ticker}: {Price}", ticker, result?.Results?.Price);
        return result?.Results;
    }

    public async Task<MassiveQuotesResponse?> GetQuotesAsync(string ticker, string? timestamp = null, int limit = 10)
    {
        var safeLimit = Math.Min(limit, DefaultTickerLimit);
        var url = $"{BaseUrl}/quotes/{ticker}?limit={safeLimit}";

        if (!string.IsNullOrEmpty(timestamp))
        {
            url += $"&timestamp={timestamp}";
        }

        var response = await SendRequestWithRetryAsync(url);
        if (response == null) return null;

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<MassiveQuotesResponse>(content);

        _logger.LogInformation("Fetched {Count} quotes for {Ticker}", result?.Results?.Count ?? 0, ticker);
        return result;
    }

    public async Task<MassiveLastQuote?> GetLastQuoteAsync(string ticker)
    {
        var url = $"{BaseUrlV2}/last/nbbo/{ticker}";

        var response = await SendRequestWithRetryAsync(url);
        if (response == null) return null;

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<MassiveLastQuoteResponse>(content);

        _logger.LogInformation("Fetched last quote for {Ticker}: Bid={Bid}, Ask={Ask}",
            ticker, result?.Results?.BidPrice, result?.Results?.AskPrice);
        return result?.Results;
    }

    #endregion

    #region Technical Indicators

    public async Task<MassiveIndicatorResponse?> GetSmaAsync(string ticker, int window = 20, string timespan = "day", int limit = 10)
    {
        var safeLimit = Math.Min(limit, DefaultTickerLimit);
        var url = $"{BaseUrlV1}/indicators/sma/{ticker}?timespan={timespan}&window={window}&series_type=close&limit={safeLimit}";

        var response = await SendRequestWithRetryAsync(url);
        if (response == null) return null;

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<MassiveIndicatorResponse>(content);

        _logger.LogInformation("Fetched {Count} SMA values for {Ticker} (window={Window})",
            result?.Results?.Values?.Count ?? 0, ticker, window);
        return result;
    }

    public async Task<MassiveIndicatorResponse?> GetEmaAsync(string ticker, int window = 20, string timespan = "day", int limit = 10)
    {
        var safeLimit = Math.Min(limit, DefaultTickerLimit);
        var url = $"{BaseUrlV1}/indicators/ema/{ticker}?timespan={timespan}&window={window}&series_type=close&limit={safeLimit}";

        var response = await SendRequestWithRetryAsync(url);
        if (response == null) return null;

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<MassiveIndicatorResponse>(content);

        _logger.LogInformation("Fetched {Count} EMA values for {Ticker} (window={Window})",
            result?.Results?.Values?.Count ?? 0, ticker, window);
        return result;
    }

    public async Task<MassiveMacdResponse?> GetMacdAsync(string ticker, string timespan = "day", int limit = 10)
    {
        var safeLimit = Math.Min(limit, DefaultTickerLimit);
        var url = $"{BaseUrlV1}/indicators/macd/{ticker}?timespan={timespan}&series_type=close&limit={safeLimit}";

        var response = await SendRequestWithRetryAsync(url);
        if (response == null) return null;

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<MassiveMacdResponse>(content);

        _logger.LogInformation("Fetched {Count} MACD values for {Ticker}",
            result?.Results?.Values?.Count ?? 0, ticker);
        return result;
    }

    public async Task<MassiveIndicatorResponse?> GetRsiAsync(string ticker, int window = 14, string timespan = "day", int limit = 10)
    {
        var safeLimit = Math.Min(limit, DefaultTickerLimit);
        var url = $"{BaseUrlV1}/indicators/rsi/{ticker}?timespan={timespan}&window={window}&series_type=close&limit={safeLimit}";

        var response = await SendRequestWithRetryAsync(url);
        if (response == null) return null;

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<MassiveIndicatorResponse>(content);

        _logger.LogInformation("Fetched {Count} RSI values for {Ticker} (window={Window})",
            result?.Results?.Values?.Count ?? 0, ticker, window);
        return result;
    }

    #endregion

    #region Private Helpers

    private async Task<HttpResponseMessage?> SendRequestWithRetryAsync(string url)
    {
        if (!_isEnabled)
        {
            _logger.LogDebug("Polygon API disabled - skipping request to {Url}", url);
            return null;
        }

        try
        {
            var urlWithKey = AddApiKey(url);
            _logger.LogInformation("Making request to: {Url}", url);
            var response = await _httpClient.GetAsync(urlWithKey);

            _logger.LogInformation("Response status: {StatusCode} for {Url}", response.StatusCode, url);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rate limited, skipping: {Url}", url);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("API error {StatusCode}: {Error} for {Url}", response.StatusCode, errorContent, url);
                return null;
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request failed: {Message}, URL: {Url}", ex.Message, url);
            return null;
        }
    }

    #endregion
}

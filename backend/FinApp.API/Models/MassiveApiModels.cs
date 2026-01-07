using Newtonsoft.Json;

namespace FinApp.API.Models;

public class MassiveTickersResponse
{
    [JsonProperty("results")]
    public List<MassiveTicker> Results { get; set; } = new();

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("count")]
    public int Count { get; set; }

    [JsonProperty("next_url")]
    public string? NextUrl { get; set; }
}

public class MassiveTicker
{
    [JsonProperty("ticker")]
    public string Ticker { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("market")]
    public string Market { get; set; } = string.Empty;

    [JsonProperty("locale")]
    public string Locale { get; set; } = string.Empty;

    [JsonProperty("primary_exchange")]
    public string PrimaryExchange { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("active")]
    public bool Active { get; set; }

    [JsonProperty("currency_name")]
    public string CurrencyName { get; set; } = string.Empty;

    [JsonProperty("market_cap")]
    public decimal? MarketCap { get; set; }
}

public class MassiveAggsResponse
{
    [JsonProperty("ticker")]
    public string Ticker { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("resultsCount")]
    public int ResultsCount { get; set; }

    [JsonProperty("results")]
    public List<MassiveAggBar>? Results { get; set; }
}

public class MassiveAggBar
{
    [JsonProperty("o")]
    public decimal Open { get; set; }

    [JsonProperty("h")]
    public decimal High { get; set; }

    [JsonProperty("l")]
    public decimal Low { get; set; }

    [JsonProperty("c")]
    public decimal Close { get; set; }

    [JsonProperty("v")]
    public long Volume { get; set; }

    [JsonProperty("t")]
    public long Timestamp { get; set; }
}

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

#region Snapshot Models

public class MassiveSnapshotResponse
{
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonProperty("ticker")]
    public MassiveTickerSnapshot? Ticker { get; set; }
}

public class MassiveFullMarketSnapshotResponse
{
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("count")]
    public int Count { get; set; }

    [JsonProperty("tickers")]
    public List<MassiveTickerSnapshot> Tickers { get; set; } = new();
}

public class MassiveTickerSnapshot
{
    [JsonProperty("ticker")]
    public string Ticker { get; set; } = string.Empty;

    [JsonProperty("todaysChange")]
    public decimal TodaysChange { get; set; }

    [JsonProperty("todaysChangePerc")]
    public decimal TodaysChangePerc { get; set; }

    [JsonProperty("updated")]
    public long Updated { get; set; }

    [JsonProperty("day")]
    public MassiveSnapshotBar? Day { get; set; }

    [JsonProperty("prevDay")]
    public MassiveSnapshotBar? PrevDay { get; set; }

    [JsonProperty("min")]
    public MassiveSnapshotBar? Min { get; set; }

    [JsonProperty("lastTrade")]
    public MassiveLastTrade? LastTrade { get; set; }

    [JsonProperty("lastQuote")]
    public MassiveLastQuote? LastQuote { get; set; }
}

public class MassiveSnapshotBar
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

    [JsonProperty("vw")]
    public decimal? VolumeWeightedAvgPrice { get; set; }
}

public class MassiveTopMoversResponse
{
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonProperty("tickers")]
    public List<MassiveTickerSnapshot> Tickers { get; set; } = new();
}

#endregion

#region Trades & Quotes Models

public class MassiveTradesResponse
{
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonProperty("results")]
    public List<MassiveTrade> Results { get; set; } = new();

    [JsonProperty("next_url")]
    public string? NextUrl { get; set; }
}

public class MassiveTrade
{
    [JsonProperty("i")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("p")]
    public decimal Price { get; set; }

    [JsonProperty("s")]
    public decimal Size { get; set; }

    [JsonProperty("x")]
    public int Exchange { get; set; }

    [JsonProperty("t")]
    public long SipTimestamp { get; set; }

    [JsonProperty("y")]
    public long ParticipantTimestamp { get; set; }

    [JsonProperty("c")]
    public List<int>? Conditions { get; set; }

    [JsonProperty("z")]
    public int? Tape { get; set; }
}

public class MassiveLastTradeResponse
{
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonProperty("results")]
    public MassiveLastTrade? Results { get; set; }
}

public class MassiveLastTrade
{
    [JsonProperty("T")]
    public string Ticker { get; set; } = string.Empty;

    [JsonProperty("i")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("p")]
    public decimal Price { get; set; }

    [JsonProperty("s")]
    public decimal? Size { get; set; }

    [JsonProperty("x")]
    public int Exchange { get; set; }

    [JsonProperty("t")]
    public long Timestamp { get; set; }

    [JsonProperty("c")]
    public List<int>? Conditions { get; set; }
}

public class MassiveQuotesResponse
{
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonProperty("results")]
    public List<MassiveQuote> Results { get; set; } = new();

    [JsonProperty("next_url")]
    public string? NextUrl { get; set; }
}

public class MassiveQuote
{
    [JsonProperty("ask_price")]
    public decimal AskPrice { get; set; }

    [JsonProperty("ask_size")]
    public decimal AskSize { get; set; }

    [JsonProperty("ask_exchange")]
    public int AskExchange { get; set; }

    [JsonProperty("bid_price")]
    public decimal BidPrice { get; set; }

    [JsonProperty("bid_size")]
    public decimal BidSize { get; set; }

    [JsonProperty("bid_exchange")]
    public int BidExchange { get; set; }

    [JsonProperty("sip_timestamp")]
    public long SipTimestamp { get; set; }

    [JsonProperty("participant_timestamp")]
    public long ParticipantTimestamp { get; set; }

    [JsonProperty("conditions")]
    public List<int>? Conditions { get; set; }

    [JsonProperty("tape")]
    public int? Tape { get; set; }
}

public class MassiveLastQuoteResponse
{
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonProperty("results")]
    public MassiveLastQuote? Results { get; set; }
}

public class MassiveLastQuote
{
    [JsonProperty("T")]
    public string Ticker { get; set; } = string.Empty;

    [JsonProperty("P")]
    public decimal AskPrice { get; set; }

    [JsonProperty("S")]
    public decimal AskSize { get; set; }

    [JsonProperty("p")]
    public decimal BidPrice { get; set; }

    [JsonProperty("s")]
    public decimal BidSize { get; set; }

    [JsonProperty("t")]
    public long Timestamp { get; set; }
}

#endregion

#region Technical Indicators Models

public class MassiveIndicatorResponse
{
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonProperty("results")]
    public MassiveIndicatorResults? Results { get; set; }

    [JsonProperty("next_url")]
    public string? NextUrl { get; set; }
}

public class MassiveIndicatorResults
{
    [JsonProperty("underlying")]
    public MassiveIndicatorUnderlying? Underlying { get; set; }

    [JsonProperty("values")]
    public List<MassiveIndicatorValue> Values { get; set; } = new();
}

public class MassiveIndicatorUnderlying
{
    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;
}

public class MassiveIndicatorValue
{
    [JsonProperty("timestamp")]
    public long Timestamp { get; set; }

    [JsonProperty("value")]
    public decimal Value { get; set; }
}

public class MassiveMacdResponse
{
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonProperty("results")]
    public MassiveMacdResults? Results { get; set; }

    [JsonProperty("next_url")]
    public string? NextUrl { get; set; }
}

public class MassiveMacdResults
{
    [JsonProperty("underlying")]
    public MassiveIndicatorUnderlying? Underlying { get; set; }

    [JsonProperty("values")]
    public List<MassiveMacdValue> Values { get; set; } = new();
}

public class MassiveMacdValue
{
    [JsonProperty("timestamp")]
    public long Timestamp { get; set; }

    [JsonProperty("value")]
    public decimal Value { get; set; }

    [JsonProperty("signal")]
    public decimal Signal { get; set; }

    [JsonProperty("histogram")]
    public decimal Histogram { get; set; }
}

#endregion

#region Ticker Details Models

public class TickerDetailsApiResponse
{
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonProperty("results")]
    public TickerDetailsResponse? Results { get; set; }
}

public class TickerDetailsResponse
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

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("homepage_url")]
    public string? HomepageUrl { get; set; }

    [JsonProperty("total_employees")]
    public int? TotalEmployees { get; set; }

    [JsonProperty("list_date")]
    public string? ListDate { get; set; }

    [JsonProperty("market_cap")]
    public decimal? MarketCap { get; set; }

    [JsonProperty("share_class_shares_outstanding")]
    public long? SharesOutstanding { get; set; }

    [JsonProperty("branding")]
    public TickerBranding? Branding { get; set; }
}

public class TickerBranding
{
    [JsonProperty("logo_url")]
    public string? LogoUrl { get; set; }

    [JsonProperty("icon_url")]
    public string? IconUrl { get; set; }
}

#endregion

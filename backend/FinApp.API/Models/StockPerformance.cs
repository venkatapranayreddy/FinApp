namespace FinApp.API.Models;

public class StockPerformance
{
    public string Ticker { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public decimal StartPrice { get; set; }
    public decimal EndPrice { get; set; }
    public decimal PercentChange { get; set; }
    public bool IsProfit { get; set; }
    public long Volume { get; set; }
    public decimal? MarketCap { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
}

public class StockPerformanceResponse
{
    public List<StockPerformance> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

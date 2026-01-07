namespace FinApp.API.Models;

public class StockDto
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public decimal? MarketCap { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static StockDto FromStock(Stock stock)
    {
        return new StockDto
        {
            Id = stock.Id,
            Ticker = stock.Ticker,
            Name = stock.Name,
            Exchange = stock.Exchange,
            MarketCap = stock.MarketCap,
            CreatedAt = stock.CreatedAt,
            UpdatedAt = stock.UpdatedAt
        };
    }
}

using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FinApp.API.Models;

[Table("stocks")]
public class Stock : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("ticker")]
    public string Ticker { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("exchange")]
    public string Exchange { get; set; } = string.Empty;

    [Column("market_cap")]
    public decimal? MarketCap { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

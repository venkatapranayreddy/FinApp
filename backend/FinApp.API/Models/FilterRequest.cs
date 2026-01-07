namespace FinApp.API.Models;

public class FilterRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Exchange { get; set; }
    public bool? ProfitOnly { get; set; }
    public bool? LossOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

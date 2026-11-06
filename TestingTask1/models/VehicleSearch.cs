namespace ShowroomTask.Models;

public class VehicleSearch
{
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Color { get; set; }
    public AvailabilityStatus? Status { get; set; }
    public string SortBy { get; set; } = "price";
    public string SortOrder { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
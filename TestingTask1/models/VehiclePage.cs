namespace ShowroomTask.Models;

public class VehiclePage
{
    public List<Vehicle> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
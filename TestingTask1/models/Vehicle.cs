namespace ShowroomTask.Models;

public class Vehicle
{
    public int Id { get; set; }
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int ManufacturingYear { get; set; }
    public decimal Price { get; set; }
    public string Color { get; set; } = null!;
    public int Mileage { get; set; }
    public AvailabilityStatus AvailabilityStatus { get; set; } = AvailabilityStatus.Available;
}
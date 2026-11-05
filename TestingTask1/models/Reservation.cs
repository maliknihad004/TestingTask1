namespace ShowroomTask.Models;

public class Reservation
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public DateTime ReservationDate { get; set; }
    public DateTime ExpectedPurchaseDate { get; set; }
    public ReservationStatus ReservationStatus { get; set; } = ReservationStatus.Active;
}

using ShowroomTask.Models;
using ShowroomTask.Repositories;

namespace ShowroomTask.Services;

public class ReservationService
{
    private readonly ReservationRepository _repository;
    private readonly CustomerRepository _customers;
    private readonly VehicleRepository _vehicles;
    private readonly VehicleCache? _cache;

    public ReservationService(
        ReservationRepository repository,
        CustomerRepository customers,
        VehicleRepository vehicles,
        VehicleCache? cache = null)
    {
        _repository = repository;
        _customers = customers;
        _vehicles = vehicles;
        _cache = cache;
    }

    public Task<List<Reservation>> GetAllAsync(int userId, string role)
    {
        if (role == "Customer")
            return _repository.GetByUserIdAsync(userId);

        return _repository.GetAllAsync();
    }

    public async Task<Reservation?> GetByIdAsync(int id, int userId, string role)
    {
        var reservation = await _repository.GetByIdAsync(id);

        if (reservation == null)
            return null;

        if (role == "Customer" && reservation.Customer.UserId != userId)
            return null;

        return reservation;
    }

    public async Task<int> AddAsync(int customerId, int vehicleId, DateTime expectedPurchaseDate, int userId, string role)
    {
        if (expectedPurchaseDate.Date < DateTime.UtcNow.Date)
            return 0;

        var customer = await _customers.GetByIdAsync(customerId);

        if (customer == null || customer.User.Role != "Customer")
            return 0;

        if (role == "Customer" && customer.UserId != userId)
            return -1;

        var vehicle = await _vehicles.GetByIdAsync(vehicleId);

        if (vehicle == null || vehicle.AvailabilityStatus != AvailabilityStatus.Available)
            return 0;

        var reservation = new Reservation
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            ReservationDate = DateTime.UtcNow,
            ExpectedPurchaseDate = DateTime.SpecifyKind(expectedPurchaseDate, DateTimeKind.Utc),
            ReservationStatus = ReservationStatus.Active
        };

        vehicle.AvailabilityStatus = AvailabilityStatus.Reserved;
        await _repository.AddAsync(reservation);
        await _repository.SaveChangesAsync();

        _cache?.Invalidate();

        return reservation.Id;
    }

    public async Task<int> CancelAsync(int id, int userId, string role)
    {
        var reservation = await _repository.GetByIdAsync(id);

        if (reservation == null)
            return 0;

        if (role == "Customer" && reservation.Customer.UserId != userId)
            return -1;

        if (reservation.ReservationStatus == ReservationStatus.Cancelled)
            return 0;

        reservation.ReservationStatus = ReservationStatus.Cancelled;

        if (reservation.Vehicle.AvailabilityStatus == AvailabilityStatus.Reserved)
            reservation.Vehicle.AvailabilityStatus = AvailabilityStatus.Available;

        await _repository.SaveChangesAsync();

        _cache?.Invalidate();

        return 1;
    }
}

using Moq;
using ShowroomTask.Data;
using ShowroomTask.Models;
using ShowroomTask.Repositories;
using ShowroomTask.Services;
using Xunit;

namespace TestingTask1.Tests;

public class ReservationServiceTests
{
    private readonly Mock<ReservationRepository> _reservations = new((AppDbContext)null!);
    private readonly Mock<CustomerRepository> _customers = new((AppDbContext)null!);
    private readonly Mock<VehicleRepository> _vehicles = new((AppDbContext)null!);

    private ReservationService NewService()
    {
        return new ReservationService(_reservations.Object, _customers.Object, _vehicles.Object);
    }

    private static Customer NewCustomer()
    {
        return new Customer
        {
            Id = 1,
            UserId = 2,
            User = new User { Id = 2, Role = "Customer" }
        };
    }

    private static Vehicle NewVehicle()
    {
        return new Vehicle { Id = 3, AvailabilityStatus = AvailabilityStatus.Available };
    }

    private static Reservation NewReservation()
    {
        return new Reservation
        {
            Id = 4,
            CustomerId = 1,
            Customer = NewCustomer(),
            VehicleId = 3,
            Vehicle = new Vehicle { Id = 3, AvailabilityStatus = AvailabilityStatus.Reserved },
            ReservationStatus = ReservationStatus.Active
        };
    }

    [Fact]
    public async Task Add_ValidReservation()
    {
        var customer = NewCustomer();
        var vehicle = NewVehicle();
        _customers.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);
        _vehicles.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(vehicle);
        _reservations.Setup(r => r.AddAsync(It.IsAny<Reservation>()))
            .Callback<Reservation>(r => r.Id = 4)
            .Returns(Task.CompletedTask);
        var service = NewService();

        var result = await service.AddAsync(1, 3, DateTime.UtcNow.AddDays(5), 2, "Customer");

        Assert.Equal(4, result);
        Assert.Equal(AvailabilityStatus.Reserved, vehicle.AvailabilityStatus);
        _reservations.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Add_PastPurchaseDate()
    {
        var service = NewService();

        var result = await service.AddAsync(1, 3, DateTime.UtcNow.AddDays(-5), 2, "Customer");

        Assert.Equal(0, result);
        _reservations.Verify(r => r.AddAsync(It.IsAny<Reservation>()), Times.Never);
    }

    [Fact]
    public async Task Add_MissingCustomer()
    {
        var service = NewService();

        var result = await service.AddAsync(99, 3, DateTime.UtcNow.AddDays(5), 2, "Customer");

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Add_OtherCustomersReservation()
    {
        _customers.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(NewCustomer());
        var service = NewService();

        var result = await service.AddAsync(1, 3, DateTime.UtcNow.AddDays(5), 9, "Customer");

        Assert.Equal(-1, result);
    }

    [Fact]
    public async Task Add_UnavailableVehicle()
    {
        var vehicle = NewVehicle();
        vehicle.AvailabilityStatus = AvailabilityStatus.Sold;
        _customers.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(NewCustomer());
        _vehicles.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(vehicle);
        var service = NewService();

        var result = await service.AddAsync(1, 3, DateTime.UtcNow.AddDays(5), 2, "Customer");

        Assert.Equal(0, result);
        _reservations.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Add_MissingVehicle()
    {
        _customers.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(NewCustomer());
        var service = NewService();

        var result = await service.AddAsync(1, 99, DateTime.UtcNow.AddDays(5), 2, "Customer");

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetAll_CustomerOnlySeesOwnReservations()
    {
        var list = new List<Reservation> { NewReservation() };
        _reservations.Setup(r => r.GetByUserIdAsync(2)).ReturnsAsync(list);
        var service = NewService();

        var result = await service.GetAllAsync(2, "Customer");

        Assert.Single(result);
        _reservations.Verify(r => r.GetByUserIdAsync(2), Times.Once);
    }

    [Fact]
    public async Task GetAll_AdminSeesAllReservations()
    {
        var list = new List<Reservation> { NewReservation() };
        _reservations.Setup(r => r.GetAllAsync()).ReturnsAsync(list);
        var service = NewService();

        var result = await service.GetAllAsync(9, "Admin");

        Assert.Single(result);
        _reservations.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetById_OwnReservation()
    {
        var reservation = NewReservation();
        _reservations.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(reservation);
        var service = NewService();

        var result = await service.GetByIdAsync(4, 2, "Customer");

        Assert.Same(reservation, result);
    }

    [Fact]
    public async Task GetById_OtherCustomersReservation()
    {
        _reservations.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(NewReservation());
        var service = NewService();

        var result = await service.GetByIdAsync(4, 9, "Customer");

        Assert.Null(result);
    }

    [Fact]
    public async Task Cancel_ExistingReservation()
    {
        var reservation = NewReservation();
        _reservations.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(reservation);
        var service = NewService();

        var result = await service.CancelAsync(4, 2, "Customer");

        Assert.Equal(1, result);
        Assert.Equal(ReservationStatus.Cancelled, reservation.ReservationStatus);
        Assert.Equal(AvailabilityStatus.Available, reservation.Vehicle.AvailabilityStatus);
        _reservations.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Cancel_OtherCustomersReservation()
    {
        var reservation = NewReservation();
        _reservations.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(reservation);
        var service = NewService();

        var result = await service.CancelAsync(4, 9, "Customer");

        Assert.Equal(-1, result);
        Assert.Equal(ReservationStatus.Active, reservation.ReservationStatus);
        _reservations.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled()
    {
        var reservation = NewReservation();
        reservation.ReservationStatus = ReservationStatus.Cancelled;
        _reservations.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(reservation);
        var service = NewService();

        var result = await service.CancelAsync(4, 2, "Customer");

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Cancel_MissingReservation()
    {
        var service = NewService();

        var result = await service.CancelAsync(99, 2, "Customer");

        Assert.Equal(0, result);
    }
}

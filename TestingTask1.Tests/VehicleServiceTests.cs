using Moq;
using ShowroomTask.Data;
using ShowroomTask.Models;
using ShowroomTask.Repositories;
using ShowroomTask.Services;
using Xunit;

namespace TestingTask1.Tests;

public class VehicleServiceTests
{
    private static Vehicle NewVehicle()
    {
        return new Vehicle
        {
            Brand = "Chery",
            Model = "Tiggo 7",
            ManufacturingYear = 2024,
            Price = 100000,
            Color = "White",
            Mileage = 0
        };
    }

    private static Mock<VehicleRepository> NewMock()
    {
        return new Mock<VehicleRepository>((AppDbContext)null!);
    }

    [Fact]
    public async Task Add_ValidVehicle()
    {
        var mock = NewMock();
        var service = new VehicleService(mock.Object);
        var vehicle = NewVehicle();

        var result = await service.AddAsync(vehicle);

        Assert.True(result);
        mock.Verify(r => r.AddAsync(vehicle), Times.Once);
        mock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Add_NegativePrice()
    {
        var mock = NewMock();
        var service = new VehicleService(mock.Object);
        var vehicle = NewVehicle();
        vehicle.Price = -10;

        var result = await service.AddAsync(vehicle);

        Assert.False(result);
        mock.Verify(r => r.AddAsync(It.IsAny<Vehicle>()), Times.Never);
    }

    [Fact]
    public async Task Add_MissingModel()
    {
        var mock = NewMock();
        var service = new VehicleService(mock.Object);
        var vehicle = NewVehicle();
        vehicle.Model = "";

        var result = await service.AddAsync(vehicle);

        Assert.False(result);
    }

    [Fact]
    public async Task Add_InvalidBrand()
    {
        var mock = NewMock();
        var service = new VehicleService(mock.Object);
        var vehicle = NewVehicle();
        vehicle.Brand = "Other";

        var result = await service.AddAsync(vehicle);

        Assert.False(result);
    }

    [Fact]
    public async Task Update_ExistingVehicle()
    {
        var mock = NewMock();
        var existing = NewVehicle();
        var changes = NewVehicle();
        changes.Price = 90000;
        mock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        var service = new VehicleService(mock.Object);

        var result = await service.UpdateAsync(1, changes);

        Assert.True(result);
        Assert.Equal(90000, existing.Price);
        mock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Update_MissingVehicle()
    {
        var mock = NewMock();
        var service = new VehicleService(mock.Object);

        var result = await service.UpdateAsync(99, NewVehicle());

        Assert.False(result);
    }

    [Fact]
    public async Task Delete_ExistingVehicle()
    {
        var mock = NewMock();
        var vehicle = NewVehicle();
        mock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicle);
        var service = new VehicleService(mock.Object);

        var result = await service.DeleteAsync(1);

        Assert.True(result);
        mock.Verify(r => r.Delete(vehicle), Times.Once);
        mock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Delete_MissingVehicle()
    {
        var mock = NewMock();
        var service = new VehicleService(mock.Object);

        var result = await service.DeleteAsync(99);

        Assert.False(result);
    }

    [Fact]
    public async Task GetById_ExistingVehicle()
    {
        var mock = NewMock();
        var vehicle = NewVehicle();
        mock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicle);
        var service = new VehicleService(mock.Object);

        var result = await service.GetByIdAsync(1);

        Assert.Same(vehicle, result);
    }

    [Fact]
    public async Task GetById_MissingVehicle()
    {
        var mock = NewMock();
        var service = new VehicleService(mock.Object);

        var result = await service.GetByIdAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task Search_AvailableVehicle()
    {
        var mock = NewMock();
        mock.Setup(r => r.GetAvailableAsync("Chery", "Tiggo 7"))
            .ReturnsAsync(new List<Vehicle> { NewVehicle() });
        var service = new VehicleService(mock.Object);

        var result = await service.GetAvailableAsync("Chery", "Tiggo 7");

        Assert.Single(result);
    }

    [Fact]
    public async Task Search_NoAvailableVehicles()
    {
        var mock = NewMock();
        mock.Setup(r => r.GetAvailableAsync("Chery", "Unknown"))
            .ReturnsAsync(new List<Vehicle>());
        var service = new VehicleService(mock.Object);

        var result = await service.GetAvailableAsync("Chery", "Unknown");

        Assert.Empty(result);
    }

    [Fact]
    public async Task ChangeAvailability_ExistingVehicle()
    {
        var mock = NewMock();
        var vehicle = NewVehicle();
        mock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicle);
        var service = new VehicleService(mock.Object);

        var result = await service.UpdateAvailabilityAsync(1, AvailabilityStatus.Sold);

        Assert.True(result);
        Assert.Equal(AvailabilityStatus.Sold, vehicle.AvailabilityStatus);
        mock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ChangeAvailability_InvalidStatus()
    {
        var mock = NewMock();
        mock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(NewVehicle());
        var service = new VehicleService(mock.Object);

        var result = await service.UpdateAvailabilityAsync(1, (AvailabilityStatus)99);

        Assert.False(result);
    }

    [Fact]
    public async Task ChangeAvailability_MissingVehicle()
    {
        var mock = NewMock();
        var service = new VehicleService(mock.Object);

        var result = await service.UpdateAvailabilityAsync(99, AvailabilityStatus.Sold);

        Assert.False(result);
    }
}

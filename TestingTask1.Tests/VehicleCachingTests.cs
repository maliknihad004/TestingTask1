using Microsoft.Extensions.Caching.Memory;
using Moq;
using ShowroomTask.Data;
using ShowroomTask.Models;
using ShowroomTask.Repositories;
using ShowroomTask.Services;
using Xunit;

namespace TestingTask1.Tests;

public class VehicleCachingTests
{
    private static Vehicle NewVehicle()
    {
        return new Vehicle
        {
            Id = 1,
            Brand = "Chery",
            Model = "Tiggo 7",
            ManufacturingYear = 2025,
            Price = 100000,
            Color = "White"
        };
    }

    private static VehicleCache NewCache()
    {
        return new VehicleCache(new MemoryCache(new MemoryCacheOptions()));
    }

    [Fact]
    public async Task List_CacheMissAndHit()
    {
        var repository = new Mock<VehicleRepository>((AppDbContext)null!);
        repository.Setup(r => r.SearchAsync(It.IsAny<VehicleSearch>()))
            .ReturnsAsync(new VehiclePage { Items = new List<Vehicle> { NewVehicle() } });
        var service = new VehicleService(repository.Object, NewCache());

        var first = await service.SearchAsync(new VehicleSearch());
        var second = await service.SearchAsync(new VehicleSearch());

        Assert.Single(first.Items);
        Assert.Single(second.Items);
        repository.Verify(r => r.SearchAsync(It.IsAny<VehicleSearch>()), Times.Once);
    }

    [Fact]
    public async Task Detail_CacheMissAndHit()
    {
        var repository = new Mock<VehicleRepository>((AppDbContext)null!);
        repository.Setup(r => r.GetDetailsAsync(1)).ReturnsAsync(NewVehicle());
        var service = new VehicleService(repository.Object, NewCache());

        var first = await service.GetDetailsAsync(1);
        var second = await service.GetDetailsAsync(1);

        Assert.NotNull(first);
        Assert.NotNull(second);
        repository.Verify(r => r.GetDetailsAsync(1), Times.Once);
    }

    [Fact]
    public async Task Expiration_ReloadsFromRepository()
    {
        var repository = new Mock<VehicleRepository>((AppDbContext)null!);
        repository.Setup(r => r.GetDetailsAsync(1)).ReturnsAsync(NewVehicle());
        var cache = new VehicleCache(new MemoryCache(new MemoryCacheOptions()),
            TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100));
        var service = new VehicleService(repository.Object, cache);

        await service.GetDetailsAsync(1);
        await Task.Delay(180);
        await service.GetDetailsAsync(1);

        repository.Verify(r => r.GetDetailsAsync(1), Times.Exactly(2));
    }

    [Fact]
    public async Task SlidingExpiration_ReloadsFromRepository()
    {
        var repository = new Mock<VehicleRepository>((AppDbContext)null!);
        repository.Setup(r => r.GetDetailsAsync(1)).ReturnsAsync(NewVehicle());
        var cache = new VehicleCache(new MemoryCache(new MemoryCacheOptions()),
            TimeSpan.FromMilliseconds(60), TimeSpan.FromSeconds(2));
        var service = new VehicleService(repository.Object, cache);

        await service.GetDetailsAsync(1);
        await Task.Delay(150);
        await service.GetDetailsAsync(1);

        repository.Verify(r => r.GetDetailsAsync(1), Times.Exactly(2));
    }

    [Fact]
    public async Task AddingVehicle_InvalidatesListCache()
    {
        var repository = new Mock<VehicleRepository>((AppDbContext)null!);
        repository.Setup(r => r.SearchAsync(It.IsAny<VehicleSearch>()))
            .ReturnsAsync(new VehiclePage());
        var service = new VehicleService(repository.Object, NewCache());
        await service.SearchAsync(new VehicleSearch());

        var saved = await service.AddAsync(NewVehicle());
        await service.SearchAsync(new VehicleSearch());

        Assert.True(saved);
        repository.Verify(r => r.SearchAsync(It.IsAny<VehicleSearch>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdatingVehicle_InvalidatesCache()
    {
        var repository = new Mock<VehicleRepository>((AppDbContext)null!);
        repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(NewVehicle());
        repository.Setup(r => r.GetDetailsAsync(1)).ReturnsAsync(NewVehicle());
        var service = new VehicleService(repository.Object, NewCache());
        await service.GetDetailsAsync(1);

        var updated = await service.UpdateAsync(1, NewVehicle());
        await service.GetDetailsAsync(1);

        Assert.True(updated);
        repository.Verify(r => r.GetDetailsAsync(1), Times.Exactly(2));
    }

    [Fact]
    public async Task DeletingVehicle_InvalidatesCache()
    {
        var repository = new Mock<VehicleRepository>((AppDbContext)null!);
        repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(NewVehicle());
        repository.Setup(r => r.GetDetailsAsync(1)).ReturnsAsync(NewVehicle());
        var service = new VehicleService(repository.Object, NewCache());
        await service.GetDetailsAsync(1);

        var deleted = await service.DeleteAsync(1);
        await service.GetDetailsAsync(1);

        Assert.True(deleted);
        repository.Verify(r => r.GetDetailsAsync(1), Times.Exactly(2));
    }

    [Fact]
    public async Task ChangingStatus_InvalidatesListCache()
    {
        var repository = new Mock<VehicleRepository>((AppDbContext)null!);
        repository.Setup(r => r.SearchAsync(It.IsAny<VehicleSearch>()))
            .ReturnsAsync(new VehiclePage());
        repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(NewVehicle());
        var service = new VehicleService(repository.Object, NewCache());
        await service.SearchAsync(new VehicleSearch());

        var changed = await service.UpdateAvailabilityAsync(1, AvailabilityStatus.Reserved);
        await service.SearchAsync(new VehicleSearch());

        Assert.True(changed);
        repository.Verify(r => r.SearchAsync(It.IsAny<VehicleSearch>()), Times.Exactly(2));
    }

    [Fact]
    public async Task InvalidVehicle_DoesNotClearCache()
    {
        var repository = new Mock<VehicleRepository>((AppDbContext)null!);
        repository.Setup(r => r.SearchAsync(It.IsAny<VehicleSearch>()))
            .ReturnsAsync(new VehiclePage());
        var service = new VehicleService(repository.Object, NewCache());
        await service.SearchAsync(new VehicleSearch());
        var invalid = NewVehicle();
        invalid.Price = -1;

        var saved = await service.AddAsync(invalid);
        await service.SearchAsync(new VehicleSearch());

        Assert.False(saved);
        repository.Verify(r => r.SearchAsync(It.IsAny<VehicleSearch>()), Times.Once);
    }
}
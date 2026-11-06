using Microsoft.EntityFrameworkCore;
using ShowroomTask.Data;
using ShowroomTask.Models;
using ShowroomTask.Repositories;
using Xunit;

namespace TestingTask1.Tests;

public class VehicleSearchRepositoryTests
{
    private static AppDbContext NewDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    private static async Task AddVehicles(AppDbContext db)
    {
        db.Vehicles.AddRange(
            new Vehicle { Brand = "Chery", Model = "Tiggo 7", ManufacturingYear = 2025, Price = 100000, Color = "White" },
            new Vehicle { Brand = "Chery", Model = "Tiggo 8", ManufacturingYear = 2024, Price = 140000, Color = "Black" },
            new Vehicle { Brand = "OMODA", Model = "C5", ManufacturingYear = 2025, Price = 90000, Color = "White" },
            new Vehicle { Brand = "JAECOO", Model = "J7", ManufacturingYear = 2025, Price = 120000, Color = "White", AvailabilityStatus = AvailabilityStatus.Sold });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CombinedFilters_ReturnOneVehicle()
    {
        using var db = NewDatabase();
        await AddVehicles(db);
        var repository = new VehicleRepository(db);

        var result = await repository.SearchAsync(new VehicleSearch
        {
            Brand = "Chery", Model = "Tiggo", Year = 2025,
            MinPrice = 80000, MaxPrice = 120000, Color = "White"
        });

        Assert.Single(result.Items);
        Assert.Equal("Tiggo 7", result.Items[0].Model);
    }

    [Fact]
    public async Task StatusFilter_ReturnSoldVehicle()
    {
        using var db = NewDatabase();
        await AddVehicles(db);
        var repository = new VehicleRepository(db);

        var result = await repository.SearchAsync(new VehicleSearch { Status = AvailabilityStatus.Sold });

        Assert.Single(result.Items);
        Assert.Equal("JAECOO", result.Items[0].Brand);
    }

    [Fact]
    public async Task SortingAndPagination_ReturnCorrectPage()
    {
        using var db = NewDatabase();
        await AddVehicles(db);
        var repository = new VehicleRepository(db);

        var result = await repository.SearchAsync(new VehicleSearch
        {
            Page = 2, PageSize = 1, SortBy = "price", SortOrder = "desc"
        });

        Assert.Single(result.Items);
        Assert.Equal(100000, result.Items[0].Price);
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task NoMatches_ReturnEmptyPage()
    {
        using var db = NewDatabase();
        await AddVehicles(db);
        var repository = new VehicleRepository(db);

        var result = await repository.SearchAsync(new VehicleSearch { Brand = "Chery", Year = 2020 });

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }
}
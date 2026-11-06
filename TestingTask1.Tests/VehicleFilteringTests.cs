using Microsoft.Extensions.DependencyInjection;
using ShowroomTask.Data;
using ShowroomTask.Models;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace TestingTask1.Tests;

public class VehicleFilteringTests
{
    private static async Task Add(TestWebApplicationFactory factory,
        string brand, string model, int year, decimal price, string color,
        AvailabilityStatus status = AvailabilityStatus.Available)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Vehicles.Add(new Vehicle
        {
            Brand = brand,
            Model = model,
            ManufacturingYear = year,
            Price = price,
            Color = color,
            AvailabilityStatus = status
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task FilterByBrand()
    {
        using var factory = new TestWebApplicationFactory();
        await Add(factory, "Chery", "Tiggo 7", 2025, 110000, "White");
        await Add(factory, "OMODA", "C5", 2025, 120000, "Black");
        using var client = factory.CreateClient();

        var result = await client.GetFromJsonAsync<VehiclePage>("/api/vehicles?brand=Chery");

        Assert.Single(result!.Items);
        Assert.Equal("Chery", result.Items[0].Brand);
    }

    [Fact]
    public async Task FilterByModelAndYear()
    {
        using var factory = new TestWebApplicationFactory();
        await Add(factory, "Chery", "Tiggo 7", 2024, 100000, "White");
        await Add(factory, "Chery", "Tiggo 8", 2025, 130000, "White");
        using var client = factory.CreateClient();

        var result = await client.GetFromJsonAsync<VehiclePage>("/api/vehicles?model=Tiggo%208&year=2025");

        Assert.Single(result!.Items);
        Assert.Equal(2025, result.Items[0].ManufacturingYear);
    }

    [Fact]
    public async Task FilterByPriceColorAndStatus()
    {
        using var factory = new TestWebApplicationFactory();
        await Add(factory, "Chery", "A", 2025, 110000, "Black", AvailabilityStatus.Reserved);
        await Add(factory, "Chery", "B", 2025, 190000, "Black", AvailabilityStatus.Reserved);
        await Add(factory, "Chery", "C", 2025, 110000, "White", AvailabilityStatus.Reserved);
        using var client = factory.CreateClient();

        var result = await client.GetFromJsonAsync<VehiclePage>(
            "/api/vehicles?minPrice=80000&maxPrice=150000&color=Black&status=Reserved");

        Assert.Single(result!.Items);
        Assert.Equal("A", result.Items[0].Model);
    }

    [Fact]
    public async Task CombinedFilters()
    {
        using var factory = new TestWebApplicationFactory();
        await Add(factory, "Chery", "Tiggo 7", 2025, 110000, "White");
        await Add(factory, "Chery", "Tiggo 7", 2024, 110000, "White");
        await Add(factory, "OMODA", "C5", 2025, 110000, "White");
        using var client = factory.CreateClient();

        var result = await client.GetFromJsonAsync<VehiclePage>(
            "/api/vehicles?brand=Chery&model=Tiggo&year=2025&minPrice=80000&maxPrice=150000&color=White&page=1&pageSize=10&sortBy=price&sortOrder=asc");

        Assert.Single(result!.Items);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task EmptyResult()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var result = await client.GetFromJsonAsync<VehiclePage>("/api/vehicles?brand=JAECOO");

        Assert.Empty(result!.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=-1")]
    [InlineData("pageSize=101")]
    [InlineData("minPrice=-50")]
    [InlineData("minPrice=200&maxPrice=100")]
    [InlineData("status=99")]
    [InlineData("sortBy=bad")]
    [InlineData("sortOrder=wrong")]
    [InlineData("year=10")]
    public async Task InvalidFilter_Returns400(string query)
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/vehicles?" + query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SortAscendingAndDescending()
    {
        using var factory = new TestWebApplicationFactory();
        await Add(factory, "Chery", "A", 2024, 70000, "White");
        await Add(factory, "Chery", "B", 2025, 150000, "White");
        using var client = factory.CreateClient();

        var asc = await client.GetFromJsonAsync<VehiclePage>("/api/vehicles?sortBy=price&sortOrder=asc");
        var desc = await client.GetFromJsonAsync<VehiclePage>("/api/vehicles?sortBy=price&sortOrder=desc");

        Assert.Equal(70000, asc!.Items[0].Price);
        Assert.Equal(150000, desc!.Items[0].Price);
    }

    [Fact]
    public async Task SortByModelAndYear()
    {
        using var factory = new TestWebApplicationFactory();
        await Add(factory, "Chery", "Z", 2024, 70000, "White");
        await Add(factory, "Chery", "A", 2025, 150000, "White");
        using var client = factory.CreateClient();

        var models = await client.GetFromJsonAsync<VehiclePage>("/api/vehicles?sortBy=model");
        var years = await client.GetFromJsonAsync<VehiclePage>("/api/vehicles?sortBy=manufacturingyear&sortOrder=desc");

        Assert.Equal("A", models!.Items[0].Model);
        Assert.Equal(2025, years!.Items[0].ManufacturingYear);
    }

    [Fact]
    public async Task PaginationReturnsMetadata()
    {
        using var factory = new TestWebApplicationFactory();
        await Add(factory, "Chery", "A", 2024, 70000, "White");
        await Add(factory, "Chery", "B", 2025, 90000, "White");
        await Add(factory, "Chery", "C", 2025, 110000, "White");
        using var client = factory.CreateClient();

        var result = await client.GetFromJsonAsync<VehiclePage>("/api/vehicles?page=2&pageSize=2");

        Assert.Single(result!.Items);
        Assert.Equal("C", result.Items[0].Model);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
    }
}
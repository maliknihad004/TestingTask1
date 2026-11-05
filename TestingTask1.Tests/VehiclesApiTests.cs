using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ShowroomTask.Data;
using ShowroomTask.Models;
using Xunit;

namespace TestingTask1.Tests;

public class VehiclesApiTests
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

    private static async Task<int> SaveVehicle(TestWebApplicationFactory factory, Vehicle vehicle)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle.Id;
    }

    private static async Task<HttpClient> Login(TestWebApplicationFactory factory, string role)
    {
        var client = factory.CreateClient();
        var user = new User
        {
            FullName = "Test User",
            Email = "test@test.com",
            Role = role
        };
        user.Password = new PasswordHasher<User>().HashPassword(user, "123456");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = user.Email,
            Password = "123456"
        });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result!.Token);
        return client;
    }

    private record TokenResponse(string Token);

    [Fact]
    public async Task GetVehicles_ReturnsAvailable()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        await SaveVehicle(factory, NewVehicle());
        var reserved = NewVehicle();
        reserved.AvailabilityStatus = AvailabilityStatus.Reserved;
        await SaveVehicle(factory, reserved);

        var response = await client.GetAsync("/api/vehicles");
        var vehicles = await response.Content.ReadFromJsonAsync<List<Vehicle>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(vehicles!);
    }

    [Fact]
    public async Task GetVehicles_Filtered_Returns200()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        await SaveVehicle(factory, NewVehicle());
        var reserved = NewVehicle();
        reserved.AvailabilityStatus = AvailabilityStatus.Reserved;
        await SaveVehicle(factory, reserved);

        var response = await client.GetAsync("/api/vehicles?brand=Chery&model=Tiggo%207");
        var vehicles = await response.Content.ReadFromJsonAsync<List<Vehicle>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(vehicles!);
    }

    [Fact]
    public async Task GetVehicle_Existing_Returns200()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var id = await SaveVehicle(factory, NewVehicle());

        var response = await client.GetAsync($"/api/vehicles/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetVehicle_Missing_Returns404()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/vehicles/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Add_Admin_Returns201()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = await Login(factory, "Admin");

        var response = await client.PostAsJsonAsync("/api/vehicles", NewVehicle());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Add_InvalidVehicle_Returns400()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = await Login(factory, "Admin");
        var vehicle = NewVehicle();
        vehicle.Price = -1;

        var response = await client.PostAsJsonAsync("/api/vehicles", vehicle);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Existing_Returns200()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = await Login(factory, "SalesRepresentative");
        var id = await SaveVehicle(factory, NewVehicle());
        var vehicle = NewVehicle();
        vehicle.Price = 90000;

        var response = await client.PutAsJsonAsync($"/api/vehicles/{id}", vehicle);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_Missing_Returns404()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = await Login(factory, "Admin");

        var response = await client.PutAsJsonAsync("/api/vehicles/999", NewVehicle());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Existing_Returns200()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = await Login(factory, "Admin");
        var id = await SaveVehicle(factory, NewVehicle());

        var response = await client.DeleteAsync($"/api/vehicles/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/vehicles/{id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_Missing_Returns404()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = await Login(factory, "Admin");

        var response = await client.DeleteAsync("/api/vehicles/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Availability_Existing_Returns200()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = await Login(factory, "SalesRepresentative");
        var id = await SaveVehicle(factory, NewVehicle());

        var response = await client.PatchAsync($"/api/vehicles/{id}/availability?status=2", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Availability_Invalid_Returns400()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = await Login(factory, "Admin");
        var id = await SaveVehicle(factory, NewVehicle());

        var response = await client.PatchAsync($"/api/vehicles/{id}/availability?status=99", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Add_NoToken_Returns401()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/vehicles", NewVehicle());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Add_Customer_Returns403()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = await Login(factory, "Customer");

        var response = await client.PostAsJsonAsync("/api/vehicles", NewVehicle());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Customer_Returns403()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = await Login(factory, "Customer");
        var id = await SaveVehicle(factory, NewVehicle());

        var response = await client.DeleteAsync($"/api/vehicles/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

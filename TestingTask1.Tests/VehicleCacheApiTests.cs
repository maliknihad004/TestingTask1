using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ShowroomTask.Data;
using ShowroomTask.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace TestingTask1.Tests;

public class VehicleCacheApiTests
{
    private record TokenResponse(string Token);

    private static Vehicle NewVehicle(decimal price = 100000)
    {
        return new Vehicle
        {
            Brand = "Chery",
            Model = "Tiggo 7",
            ManufacturingYear = 2025,
            Price = price,
            Color = "White"
        };
    }

    private static async Task<HttpClient> AdminClient(TestWebApplicationFactory factory)
    {
        var user = new User { FullName = "Admin", Email = "admin@test.com", Role = "Admin" };
        user.Password = new PasswordHasher<User>().HashPassword(user, "123456");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = user.Email,
            Password = "123456"
        });
        login.EnsureSuccessStatusCode();
        var token = await login.Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token!.Token);
        return client;
    }

    [Fact]
    public async Task CreateVehicle_RefreshesCachedList()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = await AdminClient(factory);
        var before = await client.GetFromJsonAsync<VehiclePage>("/api/vehicles");

        var created = await client.PostAsJsonAsync("/api/vehicles", NewVehicle());
        var after = await client.GetFromJsonAsync<VehiclePage>("/api/vehicles");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(0, before!.TotalCount);
        Assert.Equal(1, after!.TotalCount);
    }

    [Fact]
    public async Task UpdateVehicle_RefreshesCachedDetail()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = await AdminClient(factory);
        var created = await client.PostAsJsonAsync("/api/vehicles", NewVehicle());
        var original = await created.Content.ReadFromJsonAsync<Vehicle>();
        var before = await client.GetFromJsonAsync<Vehicle>($"/api/vehicles/{original!.Id}");

        var response = await client.PutAsJsonAsync($"/api/vehicles/{original.Id}", NewVehicle(90000));
        var after = await client.GetFromJsonAsync<Vehicle>($"/api/vehicles/{original.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(100000, before!.Price);
        Assert.Equal(90000, after!.Price);
    }

    [Fact]
    public async Task DeleteVehicle_RefreshesCachedListAndDetail()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = await AdminClient(factory);
        var created = await client.PostAsJsonAsync("/api/vehicles", NewVehicle());
        var original = await created.Content.ReadFromJsonAsync<Vehicle>();
        await client.GetAsync($"/api/vehicles/{original!.Id}");
        await client.GetAsync("/api/vehicles");

        var deleted = await client.DeleteAsync($"/api/vehicles/{original.Id}");
        var afterDetail = await client.GetAsync($"/api/vehicles/{original.Id}");
        var afterList = await client.GetFromJsonAsync<VehiclePage>("/api/vehicles");

        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDetail.StatusCode);
        Assert.Equal(0, afterList!.TotalCount);
    }

    [Fact]
    public async Task ReservationAndCancellation_RefreshCachedList()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = await AdminClient(factory);
        var created = await client.PostAsJsonAsync("/api/vehicles", NewVehicle());
        var vehicle = await created.Content.ReadFromJsonAsync<Vehicle>();

        int customerId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User { FullName = "Customer", Email = "customer@test.com", Role = "Customer" };
            user.Password = new PasswordHasher<User>().HashPassword(user, "123456");
            db.Users.Add(user);
            await db.SaveChangesAsync();
            var customer = new Customer { UserId = user.Id };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            customerId = customer.Id;
        }

        var before = await client.GetFromJsonAsync<VehiclePage>("/api/vehicles");
        var booking = await client.PostAsJsonAsync("/api/reservations", new
        {
            CustomerId = customerId,
            VehicleId = vehicle!.Id,
            ExpectedPurchaseDate = DateTime.UtcNow.AddDays(5)
        });
        var afterBooking = await client.GetFromJsonAsync<VehiclePage>("/api/vehicles");
        var body = await booking.Content.ReadFromJsonAsync<Reservation>();
        var cancellation = await client.PatchAsync($"/api/reservations/{body!.Id}/cancel", null);
        var afterCancellation = await client.GetFromJsonAsync<VehiclePage>("/api/vehicles");

        Assert.Equal(1, before!.TotalCount);
        Assert.Equal(HttpStatusCode.Created, booking.StatusCode);
        Assert.Equal(0, afterBooking!.TotalCount);
        Assert.Equal(HttpStatusCode.OK, cancellation.StatusCode);
        Assert.Equal(1, afterCancellation!.TotalCount);
    }

    [Fact]
    public async Task ChangeStatus_RefreshesCachedList()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = await AdminClient(factory);
        var created = await client.PostAsJsonAsync("/api/vehicles", NewVehicle());
        var original = await created.Content.ReadFromJsonAsync<Vehicle>();
        var before = await client.GetFromJsonAsync<VehiclePage>("/api/vehicles");

        var response = await client.PatchAsync($"/api/vehicles/{original!.Id}/availability?status=Reserved", null);
        var available = await client.GetFromJsonAsync<VehiclePage>("/api/vehicles");
        var reserved = await client.GetFromJsonAsync<VehiclePage>("/api/vehicles?status=Reserved");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, before!.TotalCount);
        Assert.Equal(0, available!.TotalCount);
        Assert.Equal(1, reserved!.TotalCount);
    }
}
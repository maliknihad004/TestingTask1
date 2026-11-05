using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ShowroomTask.Data;
using ShowroomTask.Models;
using Xunit;

namespace TestingTask1.Tests;

public class ReservationsApiTests
{
    private record TokenResponse(string Token);
    private record ReservationResponse(int Id, int CustomerId, int VehicleId, ReservationStatus ReservationStatus);

    private static async Task<User> AddUser(TestWebApplicationFactory factory, string role, string email)
    {
        var user = new User { FullName = "Test User", Email = email, Role = role };
        user.Password = new PasswordHasher<User>().HashPassword(user, "123456");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Customer> AddCustomer(TestWebApplicationFactory factory, User user)
    {
        var customer = new Customer { UserId = user.Id };

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    private static async Task<Vehicle> AddVehicle(TestWebApplicationFactory factory,
        AvailabilityStatus status = AvailabilityStatus.Available)
    {
        var vehicle = new Vehicle
        {
            Brand = "Chery",
            Model = "Tiggo 7",
            ManufacturingYear = 2024,
            Price = 100000,
            Color = "White",
            Mileage = 0,
            AvailabilityStatus = status
        };

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle;
    }

    private static async Task<Reservation> AddReservation(TestWebApplicationFactory factory,
        Customer customer, Vehicle vehicle, ReservationStatus status = ReservationStatus.Active)
    {
        var reservation = new Reservation
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            ReservationDate = DateTime.UtcNow,
            ExpectedPurchaseDate = DateTime.UtcNow.AddDays(5),
            ReservationStatus = status
        };

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();
        return reservation;
    }

    private static async Task<HttpClient> Login(TestWebApplicationFactory factory, User user)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            user.Email,
            Password = "123456"
        });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result!.Token);
        return client;
    }

    [Fact]
    public async Task GetAll_Admin_ReturnsAll()
    {
        using var factory = new TestWebApplicationFactory();
        var admin = await AddUser(factory, "Admin", "admin@test.com");
        var user = await AddUser(factory, "Customer", "customer@test.com");
        var customer = await AddCustomer(factory, user);
        var vehicle = await AddVehicle(factory, AvailabilityStatus.Reserved);
        await AddReservation(factory, customer, vehicle);
        using var client = await Login(factory, admin);

        var response = await client.GetAsync("/api/reservations");
        var reservations = await response.Content.ReadFromJsonAsync<List<ReservationResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(reservations!);
    }

    [Fact]
    public async Task GetAll_Customer_OnlyOwn()
    {
        using var factory = new TestWebApplicationFactory();
        var user1 = await AddUser(factory, "Customer", "one@test.com");
        var user2 = await AddUser(factory, "Customer", "two@test.com");
        var customer1 = await AddCustomer(factory, user1);
        var customer2 = await AddCustomer(factory, user2);
        await AddReservation(factory, customer1, await AddVehicle(factory, AvailabilityStatus.Reserved));
        await AddReservation(factory, customer2, await AddVehicle(factory, AvailabilityStatus.Reserved));
        using var client = await Login(factory, user1);

        var response = await client.GetAsync("/api/reservations");
        var reservations = await response.Content.ReadFromJsonAsync<List<ReservationResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(reservations!);
        Assert.Equal(customer1.Id, reservations![0].CustomerId);
    }

    [Fact]
    public async Task GetById_OwnReservation_Returns200()
    {
        using var factory = new TestWebApplicationFactory();
        var user = await AddUser(factory, "Customer", "customer@test.com");
        var customer = await AddCustomer(factory, user);
        var vehicle = await AddVehicle(factory, AvailabilityStatus.Reserved);
        var reservation = await AddReservation(factory, customer, vehicle);
        using var client = await Login(factory, user);

        var response = await client.GetAsync($"/api/reservations/{reservation.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_OtherCustomer_Returns404()
    {
        using var factory = new TestWebApplicationFactory();
        var user1 = await AddUser(factory, "Customer", "one@test.com");
        var user2 = await AddUser(factory, "Customer", "two@test.com");
        var customer2 = await AddCustomer(factory, user2);
        var vehicle = await AddVehicle(factory, AvailabilityStatus.Reserved);
        var reservation = await AddReservation(factory, customer2, vehicle);
        using var client = await Login(factory, user1);

        var response = await client.GetAsync($"/api/reservations/{reservation.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Missing_Returns404()
    {
        using var factory = new TestWebApplicationFactory();
        var admin = await AddUser(factory, "Admin", "admin@test.com");
        using var client = await Login(factory, admin);

        var response = await client.GetAsync("/api/reservations/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Add_ValidReservation_Returns201()
    {
        using var factory = new TestWebApplicationFactory();
        var user = await AddUser(factory, "Customer", "customer@test.com");
        var customer = await AddCustomer(factory, user);
        var vehicle = await AddVehicle(factory);
        using var client = await Login(factory, user);

        var response = await client.PostAsJsonAsync("/api/reservations", new
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            ExpectedPurchaseDate = DateTime.UtcNow.AddDays(5)
        });
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = db.Reservations.Single();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(customer.Id, saved.CustomerId);
        Assert.Equal(AvailabilityStatus.Reserved, db.Vehicles.Single().AvailabilityStatus);
    }

    [Fact]
    public async Task Add_SalesRepresentative_Returns201()
    {
        using var factory = new TestWebApplicationFactory();
        var sales = await AddUser(factory, "SalesRepresentative", "sales@test.com");
        var user = await AddUser(factory, "Customer", "customer@test.com");
        var customer = await AddCustomer(factory, user);
        var vehicle = await AddVehicle(factory);
        using var client = await Login(factory, sales);

        var response = await client.PostAsJsonAsync("/api/reservations", new
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            ExpectedPurchaseDate = DateTime.UtcNow.AddDays(5)
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Add_UnavailableVehicle_Returns400()
    {
        using var factory = new TestWebApplicationFactory();
        var user = await AddUser(factory, "Customer", "customer@test.com");
        var customer = await AddCustomer(factory, user);
        var vehicle = await AddVehicle(factory, AvailabilityStatus.Sold);
        using var client = await Login(factory, user);

        var response = await client.PostAsJsonAsync("/api/reservations", new
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            ExpectedPurchaseDate = DateTime.UtcNow.AddDays(5)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Add_PastDate_Returns400()
    {
        using var factory = new TestWebApplicationFactory();
        var user = await AddUser(factory, "Customer", "customer@test.com");
        var customer = await AddCustomer(factory, user);
        var vehicle = await AddVehicle(factory);
        using var client = await Login(factory, user);

        var response = await client.PostAsJsonAsync("/api/reservations", new
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            ExpectedPurchaseDate = DateTime.UtcNow.AddDays(-1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Add_OtherCustomersReservation_Returns403()
    {
        using var factory = new TestWebApplicationFactory();
        var user1 = await AddUser(factory, "Customer", "one@test.com");
        var user2 = await AddUser(factory, "Customer", "two@test.com");
        var customer2 = await AddCustomer(factory, user2);
        var vehicle = await AddVehicle(factory);
        using var client = await Login(factory, user1);

        var response = await client.PostAsJsonAsync("/api/reservations", new
        {
            CustomerId = customer2.Id,
            VehicleId = vehicle.Id,
            ExpectedPurchaseDate = DateTime.UtcNow.AddDays(5)
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_OwnReservation_Returns200()
    {
        using var factory = new TestWebApplicationFactory();
        var user = await AddUser(factory, "Customer", "customer@test.com");
        var customer = await AddCustomer(factory, user);
        var vehicle = await AddVehicle(factory, AvailabilityStatus.Reserved);
        var reservation = await AddReservation(factory, customer, vehicle);
        using var client = await Login(factory, user);

        var response = await client.PatchAsync($"/api/reservations/{reservation.Id}/cancel", null);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ReservationStatus.Cancelled, db.Reservations.Single().ReservationStatus);
        Assert.Equal(AvailabilityStatus.Available, db.Vehicles.Single().AvailabilityStatus);
    }

    [Fact]
    public async Task Cancel_OtherCustomer_Returns404()
    {
        using var factory = new TestWebApplicationFactory();
        var user1 = await AddUser(factory, "Customer", "one@test.com");
        var user2 = await AddUser(factory, "Customer", "two@test.com");
        var customer2 = await AddCustomer(factory, user2);
        var vehicle = await AddVehicle(factory, AvailabilityStatus.Reserved);
        var reservation = await AddReservation(factory, customer2, vehicle);
        using var client = await Login(factory, user1);

        var response = await client.PatchAsync($"/api/reservations/{reservation.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled_Returns400()
    {
        using var factory = new TestWebApplicationFactory();
        var user = await AddUser(factory, "Customer", "customer@test.com");
        var customer = await AddCustomer(factory, user);
        var vehicle = await AddVehicle(factory);
        var reservation = await AddReservation(factory, customer, vehicle, ReservationStatus.Cancelled);
        using var client = await Login(factory, user);

        var response = await client.PatchAsync($"/api/reservations/{reservation.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_NoToken_Returns401()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/reservations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Add_NoToken_Returns401()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/reservations", new
        {
            CustomerId = 1,
            VehicleId = 1,
            ExpectedPurchaseDate = DateTime.UtcNow.AddDays(5)
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

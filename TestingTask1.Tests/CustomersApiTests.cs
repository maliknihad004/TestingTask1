using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ShowroomTask.Data;
using ShowroomTask.Models;
using Xunit;

namespace TestingTask1.Tests;

public class CustomersApiTests
{
    private record TokenResponse(string Token);
    private record CustomerResponse(int Id, int UserId, string FullName, string Email);

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
    public async Task GetAll_Admin_ReturnsCustomers()
    {
        using var factory = new TestWebApplicationFactory();
        var admin = await AddUser(factory, "Admin", "admin@test.com");
        var user = await AddUser(factory, "Customer", "customer@test.com");
        await AddCustomer(factory, user);
        using var client = await Login(factory, admin);

        var response = await client.GetAsync("/api/customers");
        var customers = await response.Content.ReadFromJsonAsync<List<CustomerResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(customers!);
        Assert.Equal(user.Email, customers![0].Email);
    }

    [Fact]
    public async Task GetById_SalesRepresentative_Returns200()
    {
        using var factory = new TestWebApplicationFactory();
        var sales = await AddUser(factory, "SalesRepresentative", "sales@test.com");
        var user = await AddUser(factory, "Customer", "customer@test.com");
        var customer = await AddCustomer(factory, user);
        using var client = await Login(factory, sales);

        var response = await client.GetAsync($"/api/customers/{customer.Id}");
        var result = await response.Content.ReadFromJsonAsync<CustomerResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(user.Id, result!.UserId);
    }

    [Fact]
    public async Task GetById_Missing_Returns404()
    {
        using var factory = new TestWebApplicationFactory();
        var admin = await AddUser(factory, "Admin", "admin@test.com");
        using var client = await Login(factory, admin);

        var response = await client.GetAsync("/api/customers/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Add_ValidCustomer_Returns201()
    {
        using var factory = new TestWebApplicationFactory();
        var admin = await AddUser(factory, "Admin", "admin@test.com");
        var user = await AddUser(factory, "Customer", "customer@test.com");
        using var client = await Login(factory, admin);

        var response = await client.PostAsJsonAsync("/api/customers", new { UserId = user.Id });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Add_MissingUser_Returns400()
    {
        using var factory = new TestWebApplicationFactory();
        var admin = await AddUser(factory, "Admin", "admin@test.com");
        using var client = await Login(factory, admin);

        var response = await client.PostAsJsonAsync("/api/customers", new { UserId = 999 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Add_DuplicateCustomer_Returns400()
    {
        using var factory = new TestWebApplicationFactory();
        var admin = await AddUser(factory, "Admin", "admin@test.com");
        var user = await AddUser(factory, "Customer", "customer@test.com");
        await AddCustomer(factory, user);
        using var client = await Login(factory, admin);

        var response = await client.PostAsJsonAsync("/api/customers", new { UserId = user.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Valid_Returns200()
    {
        using var factory = new TestWebApplicationFactory();
        var admin = await AddUser(factory, "Admin", "admin@test.com");
        var user = await AddUser(factory, "Customer", "customer@test.com");
        var customer = await AddCustomer(factory, user);
        using var client = await Login(factory, admin);

        var response = await client.PutAsJsonAsync($"/api/customers/{customer.Id}", new
        {
            FullName = "New Name",
            Email = "new@test.com"
        });
        var getResponse = await client.GetAsync($"/api/customers/{customer.Id}");
        var result = await getResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("New Name", result!.FullName);
        Assert.Equal("new@test.com", result.Email);
    }

    [Fact]
    public async Task Update_Missing_Returns404()
    {
        using var factory = new TestWebApplicationFactory();
        var admin = await AddUser(factory, "Admin", "admin@test.com");
        using var client = await Login(factory, admin);

        var response = await client.PutAsJsonAsync("/api/customers/999", new
        {
            FullName = "New Name",
            Email = "new@test.com"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_InvalidName_Returns400()
    {
        using var factory = new TestWebApplicationFactory();
        var admin = await AddUser(factory, "Admin", "admin@test.com");
        var user = await AddUser(factory, "Customer", "customer@test.com");
        var customer = await AddCustomer(factory, user);
        using var client = await Login(factory, admin);

        var response = await client.PutAsJsonAsync($"/api/customers/{customer.Id}", new
        {
            FullName = "",
            Email = "new@test.com"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_DuplicateEmail_Returns400()
    {
        using var factory = new TestWebApplicationFactory();
        var admin = await AddUser(factory, "Admin", "admin@test.com");
        var user1 = await AddUser(factory, "Customer", "one@test.com");
        await AddUser(factory, "Customer", "two@test.com");
        var customer = await AddCustomer(factory, user1);
        using var client = await Login(factory, admin);

        var response = await client.PutAsJsonAsync($"/api/customers/{customer.Id}", new
        {
            FullName = "New Name",
            Email = "two@test.com"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        using var factory = new TestWebApplicationFactory();
        var user = await AddUser(factory, "Customer", "customer@test.com");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            user.Email,
            Password = "wrong"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_NoToken_Returns401()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_Customer_Returns403()
    {
        using var factory = new TestWebApplicationFactory();
        var user = await AddUser(factory, "Customer", "customer@test.com");
        using var client = await Login(factory, user);

        var response = await client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Add_Customer_Returns403()
    {
        using var factory = new TestWebApplicationFactory();
        var user = await AddUser(factory, "Customer", "customer@test.com");
        using var client = await Login(factory, user);

        var response = await client.PostAsJsonAsync("/api/customers", new { UserId = user.Id });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

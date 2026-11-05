
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShowroomTask.Services;

namespace ShowroomTask.Controllers;

public record AddCustomerRequest(int UserId);
public record UpdateCustomerRequest(string FullName, string Email);

[ApiController]
[Route("api/customers")]
[Authorize(Roles = "Admin,SalesRepresentative")]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _service;

    public CustomersController(CustomerService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _service.GetAllAsync();

        return Ok(customers.Select(c => new
        {
            c.Id,
            c.UserId,
            c.User.FullName,
            c.User.Email
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _service.GetByIdAsync(id);

        if (customer == null)
            return NotFound();

        return Ok(new
        {
            customer.Id,
            customer.UserId,
            customer.User.FullName,
            customer.User.Email
        });
    }

    [HttpPost]
    public async Task<IActionResult> Add(AddCustomerRequest request)
    {
        if (!await _service.AddAsync(request.UserId))
            return BadRequest("Invalid customer or already exists");

        return StatusCode(201, "Customer added");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateCustomerRequest request)
    {
        var customer = await _service.GetByIdAsync(id);

        if (customer == null)
            return NotFound();

        if (!await _service.UpdateAsync(id, request.FullName, request.Email))
            return BadRequest();

        return Ok("Customer updated");
    }
}

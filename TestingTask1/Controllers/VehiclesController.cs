using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShowroomTask.Models;
using ShowroomTask.Services;

namespace ShowroomTask.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly VehicleService _service;

    public VehiclesController(VehicleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailable([FromQuery] VehicleSearch search)
    {
        if (!_service.IsValidSearch(search))
            return BadRequest("Invalid filter parameters");

        return Ok(await _service.SearchAsync(search));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var vehicle = await _service.GetDetailsAsync(id);
        if (vehicle == null)
            return NotFound();

        return Ok(vehicle);
    }

    [Authorize(Roles = "Admin,SalesRepresentative")]
    [HttpPost]
    public async Task<IActionResult> Add(Vehicle vehicle)
    {
        if (!await _service.AddAsync(vehicle))
            return BadRequest();

        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicle);
    }

    [Authorize(Roles = "Admin,SalesRepresentative")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Vehicle vehicle)
    {
        if (!_service.IsValid(vehicle))
            return BadRequest();

        if (!await _service.UpdateAsync(id, vehicle))
            return NotFound();

        return Ok("Vehicle updated");
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _service.DeleteAsync(id))
            return NotFound();

        return Ok("Vehicle deleted");
    }

    [Authorize(Roles = "Admin,SalesRepresentative")]
    [HttpPatch("{id}/availability")]
    public async Task<IActionResult> UpdateAvailability(int id, [FromQuery] AvailabilityStatus? status)
    {
        if (status == null || !Enum.IsDefined(status.Value))
            return BadRequest();

        if (!await _service.UpdateAvailabilityAsync(id, status.Value))
            return NotFound();

        return Ok("Availability updated");
    }
}
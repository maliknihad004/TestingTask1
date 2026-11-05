
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShowroomTask.Services;
using System.Security.Claims;

namespace ShowroomTask.Controllers;

public record CreateReservationRequest(int CustomerId, int VehicleId, DateTime ExpectedPurchaseDate);

[ApiController]
[Route("api/reservations")]
[Authorize(Roles = "Admin,SalesRepresentative,Customer")]
public class ReservationsController : ControllerBase
{
    private readonly ReservationService _service;

    public ReservationsController(ReservationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            return Unauthorized();

        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var reservations = await _service.GetAllAsync(userId, role);

        return Ok(reservations.Select(r => new
        {
            r.Id,
            r.CustomerId,
            r.VehicleId,
            r.ReservationDate,
            r.ExpectedPurchaseDate,
            r.ReservationStatus
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            return Unauthorized();

        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var reservation = await _service.GetByIdAsync(id, userId, role);

        if (reservation == null)
            return NotFound();

        return Ok(new
        {
            reservation.Id,
            reservation.CustomerId,
            reservation.VehicleId,
            reservation.ReservationDate,
            reservation.ExpectedPurchaseDate,
            reservation.ReservationStatus
        });
    }

    [HttpPost]
    public async Task<IActionResult> Add(CreateReservationRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            return Unauthorized();

        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var id = await _service.AddAsync(request.CustomerId, request.VehicleId,
            request.ExpectedPurchaseDate, userId, role);

        if (id == -1)
            return Forbid();

        if (id == 0)
            return BadRequest("Invalid reservation or vehicle unavailable");

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            return Unauthorized();

        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var reservation = await _service.GetByIdAsync(id, userId, role);

        if (reservation == null)
            return NotFound();

        var result = await _service.CancelAsync(id, userId, role);

        if (result == -1)
            return Forbid();

        if (result == 0)
            return BadRequest("Reservation already cancelled");

        return Ok("Reservation cancelled");
    }
}

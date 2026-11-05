
using Microsoft.EntityFrameworkCore;
using ShowroomTask.Data;
using ShowroomTask.Models;

namespace ShowroomTask.Repositories;

public class VehicleRepository
{
    private readonly AppDbContext _context;

    public VehicleRepository(AppDbContext context)
    {
        _context = context;
    }

    public virtual async Task<Vehicle?> GetByIdAsync(int id)
    {
        return await _context.Vehicles.FindAsync(id);
    }

    public virtual async Task<List<Vehicle>> GetAvailableAsync(string? brand, string? model)
    {
        var query = _context.Vehicles
            .Where(v => v.AvailabilityStatus == AvailabilityStatus.Available);

        if (!string.IsNullOrWhiteSpace(brand))
            query = query.Where(v => v.Brand == brand);

        if (!string.IsNullOrWhiteSpace(model))
            query = query.Where(v => v.Model == model);

        return await query.ToListAsync();
    }

    public virtual async Task AddAsync(Vehicle vehicle)
    {
        await _context.Vehicles.AddAsync(vehicle);
    }

    public virtual void Delete(Vehicle vehicle)
    {
        _context.Vehicles.Remove(vehicle);
    }

    public virtual async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

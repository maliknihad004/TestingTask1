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

    public virtual async Task<Vehicle?> GetDetailsAsync(int id)
    {
        return await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
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

    public virtual async Task<VehiclePage> SearchAsync(VehicleSearch search)
    {
        IQueryable<Vehicle> query = _context.Vehicles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search.Brand))
            query = query.Where(v => v.Brand == search.Brand);

        if (!string.IsNullOrWhiteSpace(search.Model))
            query = query.Where(v => v.Model.Contains(search.Model));

        if (search.Year.HasValue)
            query = query.Where(v => v.ManufacturingYear == search.Year.Value);

        if (search.MinPrice.HasValue)
            query = query.Where(v => v.Price >= search.MinPrice.Value);

        if (search.MaxPrice.HasValue)
            query = query.Where(v => v.Price <= search.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(search.Color))
            query = query.Where(v => v.Color == search.Color);

        query = query.Where(v => v.AvailabilityStatus ==
            (search.Status ?? AvailabilityStatus.Available));

        var total = await query.CountAsync();
        bool descending = search.SortOrder.ToLower() == "desc";

        switch (search.SortBy.ToLower())
        {
            case "model":
                query = descending ? query.OrderByDescending(v => v.Model).ThenBy(v => v.Id)
                    : query.OrderBy(v => v.Model).ThenBy(v => v.Id);
                break;
            case "manufacturingyear":
                query = descending ? query.OrderByDescending(v => v.ManufacturingYear).ThenBy(v => v.Id)
                    : query.OrderBy(v => v.ManufacturingYear).ThenBy(v => v.Id);
                break;
            default:
                query = descending ? query.OrderByDescending(v => v.Price).ThenBy(v => v.Id)
                    : query.OrderBy(v => v.Price).ThenBy(v => v.Id);
                break;
        }

        var items = await query.Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize).ToListAsync();

        return new VehiclePage
        {
            Items = items,
            TotalCount = total,
            Page = search.Page,
            PageSize = search.PageSize
        };
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
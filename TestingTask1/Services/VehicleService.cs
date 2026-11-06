
using ShowroomTask.Models;
using ShowroomTask.Repositories;
using System.Text.Json;

namespace ShowroomTask.Services;

public class VehicleService
{
    private readonly VehicleRepository _repository;
    private readonly VehicleCache? _cache;

    public VehicleService(VehicleRepository repository, VehicleCache? cache = null)
    {
        _repository = repository;
        _cache = cache;
    }

    public Task<Vehicle?> GetByIdAsync(int id)
    {
        return _repository.GetByIdAsync(id);
    }

    public Task<List<Vehicle>> GetAvailableAsync(string? brand, string? model)
    {
        return _repository.GetAvailableAsync(brand, model);
    }

    public Task<Vehicle?> GetDetailsAsync(int id)
    {
        if (_cache == null || Environment.GetEnvironmentVariable("VEHICLE_CACHE_ENABLED") == "false")
            return _repository.GetDetailsAsync(id);

        return _cache.GetOrCreateAsync($"detail:{id}",
            () => _repository.GetDetailsAsync(id));
    }

    public Task<VehiclePage> SearchAsync(VehicleSearch search)
    {
        if (_cache == null || Environment.GetEnvironmentVariable("VEHICLE_CACHE_ENABLED") == "false")
            return _repository.SearchAsync(search);

        string key = "list:" + JsonSerializer.Serialize(search);

        return _cache.GetOrCreateAsync(key,
            () => _repository.SearchAsync(search));
    }

    public bool IsValidSearch(VehicleSearch search)
    {
        if (search.Page < 1 || search.PageSize < 1 || search.PageSize > 100)
            return false;

        if (search.Page > int.MaxValue / search.PageSize)
            return false;

        if (search.Year.HasValue &&
            (search.Year < 1000 || search.Year > 9999))
            return false;

        if (search.MinPrice < 0 || search.MaxPrice < 0 ||
            search.MinPrice > search.MaxPrice)
            return false;

        if (search.Status.HasValue && !Enum.IsDefined(search.Status.Value))
            return false;

        if (!string.IsNullOrWhiteSpace(search.Brand) &&
            search.Brand != "Chery" &&
            search.Brand != "OMODA" &&
            search.Brand != "JAECOO")
            return false;

        if (string.IsNullOrWhiteSpace(search.SortBy) ||
            string.IsNullOrWhiteSpace(search.SortOrder))
            return false;

        string sortBy = search.SortBy.ToLower();
        string sortOrder = search.SortOrder.ToLower();

        if (sortBy != "price" &&
            sortBy != "model" &&
            sortBy != "manufacturingyear")
            return false;

        if (sortOrder != "asc" && sortOrder != "desc")
            return false;

        return true;
    }

    public async Task<bool> AddAsync(Vehicle vehicle)
    {
        if (!IsValid(vehicle))
            return false;

        await _repository.AddAsync(vehicle);
        await _repository.SaveChangesAsync();
        _cache?.Invalidate();

        return true;
    }

    public async Task<bool> UpdateAsync(int id, Vehicle details)
    {
        var vehicle = await _repository.GetByIdAsync(id);

        if (vehicle == null || !IsValid(details))
            return false;

        vehicle.Brand = details.Brand;
        vehicle.Model = details.Model;
        vehicle.ManufacturingYear = details.ManufacturingYear;
        vehicle.Price = details.Price;
        vehicle.Color = details.Color;
        vehicle.Mileage = details.Mileage;

        await _repository.SaveChangesAsync();
        _cache?.Invalidate();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var vehicle = await _repository.GetByIdAsync(id);

        if (vehicle == null)
            return false;

        _repository.Delete(vehicle);
        await _repository.SaveChangesAsync();
        _cache?.Invalidate();

        return true;
    }

    public async Task<bool> UpdateAvailabilityAsync(int id, AvailabilityStatus status)
    {
        var vehicle = await _repository.GetByIdAsync(id);

        if (vehicle == null || !Enum.IsDefined(status))
            return false;

        vehicle.AvailabilityStatus = status;

        await _repository.SaveChangesAsync();
        _cache?.Invalidate();

        return true;
    }

    public bool IsValid(Vehicle vehicle)
    {
        if (string.IsNullOrWhiteSpace(vehicle.Brand) ||
            string.IsNullOrWhiteSpace(vehicle.Model) ||
            string.IsNullOrWhiteSpace(vehicle.Color))
            return false;

        if (vehicle.Brand != "Chery" &&
            vehicle.Brand != "OMODA" &&
            vehicle.Brand != "JAECOO")
            return false;

        if (vehicle.Price < 0 || vehicle.Mileage < 0)
            return false;

        if (vehicle.ManufacturingYear < 1000 ||
            vehicle.ManufacturingYear > 9999)
            return false;

        if (!Enum.IsDefined(vehicle.AvailabilityStatus))
            return false;

        return true;
    }
}

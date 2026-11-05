
using ShowroomTask.Models;
using ShowroomTask.Repositories;

namespace ShowroomTask.Services;

public class VehicleService
{
    private readonly VehicleRepository _repository;

    public VehicleService(VehicleRepository repository)
    {
        _repository = repository;
    }

    public Task<Vehicle?> GetByIdAsync(int id)
    {
        return _repository.GetByIdAsync(id);
    }

    public Task<List<Vehicle>> GetAvailableAsync(string? brand, string? model)
    {
        return _repository.GetAvailableAsync(brand, model);
    }

    public async Task<bool> AddAsync(Vehicle vehicle)
    {
        if (!IsValid(vehicle))
            return false;

        await _repository.AddAsync(vehicle);
        await _repository.SaveChangesAsync();
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
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var vehicle = await _repository.GetByIdAsync(id);

        if (vehicle == null)
            return false;

        _repository.Delete(vehicle);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateAvailabilityAsync(int id, AvailabilityStatus status)
    {
        var vehicle = await _repository.GetByIdAsync(id);

        if (vehicle == null || !Enum.IsDefined(status))
            return false;

        vehicle.AvailabilityStatus = status;
        await _repository.SaveChangesAsync();
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

        if (vehicle.ManufacturingYear < 1000 || vehicle.ManufacturingYear > 9999)
            return false;

        if (!Enum.IsDefined(vehicle.AvailabilityStatus))
            return false;

        return true;
    }
}

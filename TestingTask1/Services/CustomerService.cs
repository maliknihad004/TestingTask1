
using ShowroomTask.Models;
using ShowroomTask.Repositories;

namespace ShowroomTask.Services;

public class CustomerService
{
    private readonly CustomerRepository _repository;

    public CustomerService(CustomerRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Customer>> GetAllAsync()
    {
        return _repository.GetAllAsync();
    }

    public Task<Customer?> GetByIdAsync(int id)
    {
        return _repository.GetByIdAsync(id);
    }

    public async Task<bool> AddAsync(int userId)
    {
        var user = await _repository.GetUserByIdAsync(userId);

        if (user == null || user.Role != "Customer")
            return false;

        var customer = await _repository.GetByUserIdAsync(userId);

        if (customer != null)
            return false;

        await _repository.AddAsync(new Customer { UserId = userId });
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateAsync(int id, string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            return false;

        var customer = await _repository.GetByIdAsync(id);

        if (customer == null)
            return false;

        if (await _repository.EmailExistsAsync(email, customer.UserId))
            return false;

        customer.User.FullName = fullName;
        customer.User.Email = email;

        await _repository.SaveChangesAsync();
        return true;
    }
}

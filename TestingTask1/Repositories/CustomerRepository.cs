using Microsoft.EntityFrameworkCore;
using ShowroomTask.Data;
using ShowroomTask.Models;

namespace ShowroomTask.Repositories;

public class CustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public virtual async Task<List<Customer>> GetAllAsync()
    {
        return await _context.Customers.Include(c => c.User).ToListAsync();
    }

    public virtual async Task<Customer?> GetByIdAsync(int id)
    {
        return await _context.Customers.Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public virtual async Task<Customer?> GetByUserIdAsync(int userId)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public virtual async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public virtual async Task<bool> EmailExistsAsync(string email, int userId)
    {
        return await _context.Users.AnyAsync(u => u.Email == email && u.Id != userId);
    }

    public virtual async Task AddAsync(Customer customer)
    {
        await _context.Customers.AddAsync(customer);
    }

    public virtual async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

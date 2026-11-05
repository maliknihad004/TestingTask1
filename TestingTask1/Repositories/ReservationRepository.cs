
using Microsoft.EntityFrameworkCore;
using ShowroomTask.Data;
using ShowroomTask.Models;

namespace ShowroomTask.Repositories;

public class ReservationRepository
{
    private readonly AppDbContext _context;

    public ReservationRepository(AppDbContext context)
    {
        _context = context;
    }

    public virtual async Task<List<Reservation>> GetAllAsync()
    {
        return await _context.Reservations
            .Include(r => r.Customer).ThenInclude(c => c.User)
            .Include(r => r.Vehicle)
            .ToListAsync();
    }

    public virtual async Task<List<Reservation>> GetByUserIdAsync(int userId)
    {
        return await _context.Reservations
            .Include(r => r.Customer).ThenInclude(c => c.User)
            .Include(r => r.Vehicle)
            .Where(r => r.Customer.UserId == userId)
            .ToListAsync();
    }

    public virtual async Task<Reservation?> GetByIdAsync(int id)
    {
        return await _context.Reservations
            .Include(r => r.Customer).ThenInclude(c => c.User)
            .Include(r => r.Vehicle)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public virtual async Task AddAsync(Reservation reservation)
    {
        await _context.Reservations.AddAsync(reservation);
    }

    public virtual async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

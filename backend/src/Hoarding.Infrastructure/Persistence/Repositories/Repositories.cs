using Hoarding.Application.Common.Interfaces;
using Hoarding.Domain.Entities;
using Hoarding.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hoarding.Infrastructure.Persistence.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly HoardingDbContext _db;
    public BookingRepository(HoardingDbContext db) => _db = db;

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Bookings
            .Include(b => b.InventoryUnit)
            .Include(b => b.Campaign)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<Booking?> GetByRefAsync(string bookingRef, CancellationToken ct = default)
        => await _db.Bookings
            .Include(b => b.InventoryUnit)
            .FirstOrDefaultAsync(b => b.BookingRef == bookingRef, ct);

    public async Task<bool> HasConflictingBookingAsync(
        Guid inventoryUnitId, DateOnly start, DateOnly end, Guid? excludeBookingId = null, CancellationToken ct = default)
    {
        return await _db.Bookings.AnyAsync(b =>
            b.InventoryUnitId == inventoryUnitId &&
            (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Active) &&
            (!excludeBookingId.HasValue || b.Id != excludeBookingId.Value) &&
            b.StartDate <= end && b.EndDate >= start, ct);
    }

    public async Task<IEnumerable<Booking>> GetByCampaignAsync(Guid campaignId, CancellationToken ct = default)
        => await _db.Bookings
            .Include(b => b.InventoryUnit)
            .Where(b => b.CampaignId == campaignId)
            .ToListAsync(ct);

    public async Task<IEnumerable<Booking>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await _db.Bookings
            .Include(b => b.InventoryUnit)
            .Where(b => b.BookedBy == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Booking booking, CancellationToken ct = default)
        => await _db.Bookings.AddAsync(booking, ct);

    public Task UpdateAsync(Booking booking, CancellationToken ct = default)
    {
        _db.Bookings.Update(booking);
        return Task.CompletedTask;
    }
}

public class UserRepository : IUserRepository
{
    private readonly HoardingDbContext _db;
    public UserRepository(HoardingDbContext db) => _db = db;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default)
        => await _db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Phone == phone, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await _db.Users.AddAsync(user, ct);

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        return Task.CompletedTask;
    }
}

public class UnitOfWork : IUnitOfWork
{
    private readonly HoardingDbContext _db;
    public UnitOfWork(HoardingDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

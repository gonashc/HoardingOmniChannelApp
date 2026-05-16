using Hoarding.Domain.Entities;
using Hoarding.Domain.Enums;

namespace Hoarding.Application.Common.Interfaces;

/// <summary>
/// Channel-agnostic inventory repository. The same calls work for hoardings,
/// influencers, and any future channel. Channel-specific filters live on the criteria object.
/// </summary>
public interface IInventoryRepository
{
    Task<InventoryUnit?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<InventoryUnit?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<(IEnumerable<InventoryUnit> Items, int Total)> SearchAsync(InventorySearchCriteria criteria, CancellationToken ct = default);
    Task<IEnumerable<InventoryUnit>> GetTrendingAsync(ChannelType? channel, int? cityId, int limit, CancellationToken ct = default);
    Task<IEnumerable<InventoryUnit>> GetAvailableAsync(DateOnly start, DateOnly end, ChannelType? channel, int? cityId, CancellationToken ct = default);
    Task AddAsync(InventoryUnit unit, CancellationToken ct = default);
    Task UpdateAsync(InventoryUnit unit, CancellationToken ct = default);
    Task IncrementViewCountAsync(Guid id, CancellationToken ct = default);
}

public class InventorySearchCriteria
{
    // ---- Channel-agnostic ----
    public ChannelType? Channel { get; set; }
    public string? Query { get; set; }
    public DateOnly? AvailableFrom { get; set; }
    public DateOnly? AvailableTo { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; }                  // price_asc | price_desc | popularity | newest | reach
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    // ---- Hoarding-specific ----
    public int? CityId { get; set; }
    public int? AreaId { get; set; }
    public string? Pincode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? RadiusKm { get; set; }
    public List<string>? HoardingTypes { get; set; }
    public decimal? MinWidth { get; set; }
    public decimal? MaxWidth { get; set; }
    public decimal? MinHeight { get; set; }
    public decimal? MaxHeight { get; set; }
    public int? MinTraffic { get; set; }
    public string? IlluminationType { get; set; }

    // ---- Influencer-specific ----
    public List<string>? Platforms { get; set; }            // 'instagram', 'youtube', ...
    public List<string>? Tiers { get; set; }                // 'nano', 'micro', ...
    public long? MinFollowers { get; set; }
    public long? MaxFollowers { get; set; }
    public decimal? MinEngagementRate { get; set; }
    public List<string>? Categories { get; set; }           // 'food', 'tech', ...
    public List<string>? Languages { get; set; }
    public int? AudienceCityId { get; set; }
    public bool? PlatformVerifiedOnly { get; set; }
    public List<string>? RequiredDeliverables { get; set; } // 'reel', 'video', ...
    public string? ExcludesCategory { get; set; }           // exclude creators who block the brand's category
}

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Booking?> GetByRefAsync(string bookingRef, CancellationToken ct = default);
    Task<bool> HasConflictingBookingAsync(Guid inventoryUnitId, DateOnly start, DateOnly end, Guid? excludeBookingId = null, CancellationToken ct = default);
    Task<IEnumerable<Booking>> GetByCampaignAsync(Guid campaignId, CancellationToken ct = default);
    Task<IEnumerable<Booking>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Booking booking, CancellationToken ct = default);
    Task UpdateAsync(Booking booking, CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}

/// <summary>
/// Pluggable per-channel pricing engine. Each channel implements its own.
/// </summary>
public interface IPricingStrategy
{
    ChannelType Channel { get; }

    /// <summary>
    /// Calculate base price for a date range (and optional deliverable spec for creator channels).
    /// </summary>
    decimal CalculateBasePrice(InventoryUnit unit, DateOnly start, DateOnly end, string? deliverableSpecJson);
}

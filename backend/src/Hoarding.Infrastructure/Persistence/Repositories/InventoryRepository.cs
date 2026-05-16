using Hoarding.Application.Common.Interfaces;
using Hoarding.Domain.Entities;
using Hoarding.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Hoarding.Infrastructure.Persistence.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly HoardingDbContext _db;
    private static readonly GeometryFactory _geom = new(new PrecisionModel(), 4326);

    public InventoryRepository(HoardingDbContext db) => _db = db;

    public async Task<InventoryUnit?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.InventoryUnits
            .Include(u => u.Owner)
            .Include(u => u.Hoarding)
                .ThenInclude(h => h!.Area)
                    .ThenInclude(a => a!.City)
                        .ThenInclude(c => c!.State)
            .Include(u => u.Influencer)
                .ThenInclude(i => i!.PrimaryAudienceCity)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<InventoryUnit?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _db.InventoryUnits
            .Include(u => u.Hoarding).ThenInclude(h => h!.Area).ThenInclude(a => a!.City)
            .Include(u => u.Influencer).ThenInclude(i => i!.PrimaryAudienceCity)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Code == code, ct);
    }

    public async Task<(IEnumerable<InventoryUnit> Items, int Total)> SearchAsync(
        InventorySearchCriteria c, CancellationToken ct = default)
    {
        var q = _db.InventoryUnits
            .Include(u => u.Hoarding).ThenInclude(h => h!.Area).ThenInclude(a => a!.City)
            .Include(u => u.Influencer).ThenInclude(i => i!.PrimaryAudienceCity)
            .Where(u => u.Status == ListingStatus.Active);

        // ---------- channel filter ----------
        if (c.Channel.HasValue)
            q = q.Where(u => u.Channel == c.Channel.Value);

        // ---------- text query ----------
        if (!string.IsNullOrWhiteSpace(c.Query))
        {
            var txt = c.Query.Trim().ToLower();
            q = q.Where(u =>
                u.Name.ToLower().Contains(txt) ||
                (u.Description != null && u.Description.ToLower().Contains(txt)) ||
                (u.Hoarding != null && u.Hoarding.Address != null && u.Hoarding.Address.ToLower().Contains(txt)) ||
                (u.Influencer != null && u.Influencer.Handle.ToLower().Contains(txt)));
        }

        // ---------- HOARDING filters ----------
        if (c.CityId.HasValue)
            q = q.Where(u => u.Hoarding != null && u.Hoarding.Area != null && u.Hoarding.Area.CityId == c.CityId.Value);

        if (c.AreaId.HasValue)
            q = q.Where(u => u.Hoarding != null && u.Hoarding.AreaId == c.AreaId.Value);

        if (!string.IsNullOrWhiteSpace(c.Pincode))
            q = q.Where(u => u.Hoarding != null && u.Hoarding.Area != null && u.Hoarding.Area.Pincode == c.Pincode);

        if (c.Latitude.HasValue && c.Longitude.HasValue && c.RadiusKm.HasValue)
        {
            var center = _geom.CreatePoint(new Coordinate(c.Longitude.Value, c.Latitude.Value));
            center.SRID = 4326;
            var radiusM = c.RadiusKm.Value * 1000;
            q = q.Where(u => u.Hoarding != null && u.Hoarding.Location.IsWithinDistance(center, radiusM));
        }

        if (c.HoardingTypes != null && c.HoardingTypes.Count > 0)
        {
            var types = c.HoardingTypes
                .Select(t => Enum.TryParse<HoardingType>(t, true, out var p) ? (HoardingType?)p : null)
                .Where(t => t.HasValue).Select(t => t!.Value).ToList();
            q = q.Where(u => u.Hoarding != null && types.Contains(u.Hoarding.Type));
        }

        if (c.MinWidth.HasValue)
            q = q.Where(u => u.Hoarding != null && u.Hoarding.WidthFt >= c.MinWidth.Value);
        if (c.MaxWidth.HasValue)
            q = q.Where(u => u.Hoarding != null && u.Hoarding.WidthFt <= c.MaxWidth.Value);

        if (c.MinTraffic.HasValue)
            q = q.Where(u => u.EstimatedReachDaily >= c.MinTraffic.Value);

        if (!string.IsNullOrWhiteSpace(c.IlluminationType))
        {
            if (Enum.TryParse<IlluminationType>(c.IlluminationType, true, out var ill))
                q = q.Where(u => u.Hoarding != null && u.Hoarding.Illumination == ill);
        }

        // ---------- INFLUENCER filters ----------
        if (c.Platforms != null && c.Platforms.Count > 0)
        {
            var plats = c.Platforms
                .Select(p => Enum.TryParse<SocialPlatform>(p, true, out var pp) ? (SocialPlatform?)pp : null)
                .Where(p => p.HasValue).Select(p => p!.Value).ToList();
            q = q.Where(u => u.Influencer != null && plats.Contains(u.Influencer.PrimaryPlatform));
        }

        if (c.Tiers != null && c.Tiers.Count > 0)
        {
            var tiers = c.Tiers
                .Select(t => Enum.TryParse<CreatorTier>(t, true, out var tt) ? (CreatorTier?)tt : null)
                .Where(t => t.HasValue).Select(t => t!.Value).ToList();
            q = q.Where(u => u.Influencer != null && tiers.Contains(u.Influencer.Tier));
        }

        if (c.MinFollowers.HasValue)
            q = q.Where(u => u.Influencer != null && u.Influencer.FollowerCount >= c.MinFollowers.Value);
        if (c.MaxFollowers.HasValue)
            q = q.Where(u => u.Influencer != null && u.Influencer.FollowerCount <= c.MaxFollowers.Value);

        if (c.MinEngagementRate.HasValue)
            q = q.Where(u => u.Influencer != null && u.Influencer.EngagementRate >= c.MinEngagementRate.Value);

        if (c.Categories != null && c.Categories.Count > 0)
        {
            // creator must have at least one of the requested categories
            q = q.Where(u => u.Influencer != null &&
                u.Influencer.ContentCategories.Any(cat => c.Categories.Contains(cat)));
        }

        if (c.Languages != null && c.Languages.Count > 0)
        {
            q = q.Where(u => u.Influencer != null &&
                u.Influencer.Languages.Any(lang => c.Languages.Contains(lang)));
        }

        if (c.AudienceCityId.HasValue)
            q = q.Where(u => u.Influencer != null && u.Influencer.PrimaryAudienceCityId == c.AudienceCityId.Value);

        if (c.PlatformVerifiedOnly == true)
            q = q.Where(u => u.Influencer != null && u.Influencer.IsPlatformVerified);

        if (c.RequiredDeliverables != null && c.RequiredDeliverables.Count > 0)
        {
            var dels = c.RequiredDeliverables
                .Select(d => Enum.TryParse<DeliverableType>(d, true, out var dd) ? (DeliverableType?)dd : null)
                .Where(d => d.HasValue).Select(d => d!.Value).ToList();
            q = q.Where(u => u.Influencer != null &&
                dels.All(d => u.Influencer.AvailableDeliverables.Contains(d)));
        }

        if (!string.IsNullOrWhiteSpace(c.ExcludesCategory))
        {
            var excl = c.ExcludesCategory.ToLowerInvariant();
            q = q.Where(u => u.Influencer == null ||
                !u.Influencer.ExcludesCategories.Contains(excl));
        }

        // ---------- shared filters ----------
        if (c.MinPrice.HasValue)
        {
            q = q.Where(u =>
                (u.BasePriceMonthly ?? 0) >= c.MinPrice.Value ||
                (u.BasePricePerUnit ?? 0) >= c.MinPrice.Value);
        }
        if (c.MaxPrice.HasValue)
        {
            q = q.Where(u =>
                (u.BasePriceMonthly != null && u.BasePriceMonthly <= c.MaxPrice.Value) ||
                (u.BasePricePerUnit != null && u.BasePricePerUnit <= c.MaxPrice.Value));
        }

        // ---------- availability ----------
        if (c.AvailableFrom.HasValue && c.AvailableTo.HasValue)
        {
            var from = c.AvailableFrom.Value;
            var to = c.AvailableTo.Value;
            q = q.Where(u => !_db.Bookings.Any(b =>
                b.InventoryUnitId == u.Id &&
                (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Active) &&
                b.StartDate <= to && b.EndDate >= from));
        }

        var total = await q.CountAsync(ct);

        // ---------- sort ----------
        q = c.SortBy switch
        {
            "price_asc"   => q.OrderBy(u => u.BasePriceMonthly ?? u.BasePricePerUnit ?? 0),
            "price_desc"  => q.OrderByDescending(u => u.BasePriceMonthly ?? u.BasePricePerUnit ?? 0),
            "newest"      => q.OrderByDescending(u => u.CreatedAt),
            "reach"       => q.OrderByDescending(u => u.EstimatedReachDaily ?? 0),
            "popularity"  => q.OrderByDescending(u => u.BookingCount * 3 + u.ViewCount),
            _             => q.OrderByDescending(u => u.BookingCount * 3 + u.ViewCount)
        };

        var items = await q
            .Skip((c.Page - 1) * c.PageSize)
            .Take(c.PageSize)
            .AsSplitQuery()
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IEnumerable<InventoryUnit>> GetTrendingAsync(
        ChannelType? channel, int? cityId, int limit, CancellationToken ct = default)
    {
        var q = _db.InventoryUnits
            .Include(u => u.Hoarding).ThenInclude(h => h!.Area).ThenInclude(a => a!.City)
            .Include(u => u.Influencer).ThenInclude(i => i!.PrimaryAudienceCity)
            .Where(u => u.Status == ListingStatus.Active);

        if (channel.HasValue) q = q.Where(u => u.Channel == channel.Value);
        if (cityId.HasValue)
            q = q.Where(u =>
                (u.Hoarding != null && u.Hoarding.Area != null && u.Hoarding.Area.CityId == cityId) ||
                (u.Influencer != null && u.Influencer.PrimaryAudienceCityId == cityId));

        return await q.OrderByDescending(u => u.BookingCount * 3 + u.ViewCount)
            .Take(limit)
            .AsSplitQuery()
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<InventoryUnit>> GetAvailableAsync(
        DateOnly start, DateOnly end, ChannelType? channel, int? cityId, CancellationToken ct = default)
    {
        var (items, _) = await SearchAsync(new InventorySearchCriteria
        {
            Channel = channel,
            CityId = cityId,
            AvailableFrom = start,
            AvailableTo = end,
            PageSize = 200
        }, ct);
        return items;
    }

    public async Task AddAsync(InventoryUnit unit, CancellationToken ct = default)
    {
        await _db.InventoryUnits.AddAsync(unit, ct);
    }

    public Task UpdateAsync(InventoryUnit unit, CancellationToken ct = default)
    {
        _db.InventoryUnits.Update(unit);
        return Task.CompletedTask;
    }

    public async Task IncrementViewCountAsync(Guid id, CancellationToken ct = default)
    {
        await _db.InventoryUnits
            .Where(u => u.Id == id)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.ViewCount, x => x.ViewCount + 1), ct);
    }
}

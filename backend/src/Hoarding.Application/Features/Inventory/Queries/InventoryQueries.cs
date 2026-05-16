using Hoarding.Application.Common.Interfaces;
using Hoarding.Application.Features.Inventory.DTOs;
using Hoarding.Domain.Entities;
using Hoarding.Domain.Enums;
using MediatR;
using System.Text.Json;

namespace Hoarding.Application.Features.Inventory.Queries;

public record SearchInventoryQuery(
    string? Channel = null,
    string? Query = null,

    // Hoarding filters
    int? CityId = null,
    int? AreaId = null,
    string? Pincode = null,
    double? Latitude = null,
    double? Longitude = null,
    double? RadiusKm = null,
    List<string>? HoardingTypes = null,
    decimal? MinWidth = null,
    decimal? MaxWidth = null,
    int? MinTraffic = null,
    string? IlluminationType = null,

    // Influencer filters
    List<string>? Platforms = null,
    List<string>? Tiers = null,
    long? MinFollowers = null,
    long? MaxFollowers = null,
    decimal? MinEngagementRate = null,
    List<string>? Categories = null,
    List<string>? Languages = null,
    int? AudienceCityId = null,
    bool? PlatformVerifiedOnly = null,
    List<string>? RequiredDeliverables = null,
    string? ExcludesCategory = null,

    // Shared
    DateOnly? AvailableFrom = null,
    DateOnly? AvailableTo = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? SortBy = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<InventoryListDto>>;

public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize);

public class SearchInventoryQueryHandler : IRequestHandler<SearchInventoryQuery, PagedResult<InventoryListDto>>
{
    private readonly IInventoryRepository _repo;

    public SearchInventoryQueryHandler(IInventoryRepository repo)
    {
        _repo = repo;
    }

    public async Task<PagedResult<InventoryListDto>> Handle(SearchInventoryQuery req, CancellationToken ct)
    {
        var c = new InventorySearchCriteria
        {
            Channel = ParseChannel(req.Channel),
            Query = req.Query,
            CityId = req.CityId,
            AreaId = req.AreaId,
            Pincode = req.Pincode,
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            RadiusKm = req.RadiusKm,
            HoardingTypes = req.HoardingTypes,
            MinWidth = req.MinWidth,
            MaxWidth = req.MaxWidth,
            MinTraffic = req.MinTraffic,
            IlluminationType = req.IlluminationType,
            Platforms = req.Platforms,
            Tiers = req.Tiers,
            MinFollowers = req.MinFollowers,
            MaxFollowers = req.MaxFollowers,
            MinEngagementRate = req.MinEngagementRate,
            Categories = req.Categories,
            Languages = req.Languages,
            AudienceCityId = req.AudienceCityId,
            PlatformVerifiedOnly = req.PlatformVerifiedOnly,
            RequiredDeliverables = req.RequiredDeliverables,
            ExcludesCategory = req.ExcludesCategory,
            AvailableFrom = req.AvailableFrom,
            AvailableTo = req.AvailableTo,
            MinPrice = req.MinPrice,
            MaxPrice = req.MaxPrice,
            SortBy = req.SortBy,
            Page = req.Page,
            PageSize = req.PageSize
        };

        var (items, total) = await _repo.SearchAsync(c, ct);
        var dtos = items.Select(MapList);
        return new PagedResult<InventoryListDto>(dtos, total, req.Page, req.PageSize);
    }

    public static ChannelType? ParseChannel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return Enum.TryParse<ChannelType>(raw, true, out var c) ? c : null;
    }

    public static InventoryListDto MapList(InventoryUnit u)
    {
        HoardingListExt? hExt = null;
        InfluencerListExt? iExt = null;

        if (u.Channel == ChannelType.Hoarding && u.Hoarding != null)
        {
            hExt = new HoardingListExt(
                u.Hoarding.Type.ToString(),
                u.Hoarding.Illumination.ToString(),
                u.Hoarding.Area?.City?.Name,
                u.Hoarding.Area?.Name,
                (decimal)u.Hoarding.Location.Y,
                (decimal)u.Hoarding.Location.X,
                u.Hoarding.WidthFt,
                u.Hoarding.HeightFt,
                u.Hoarding.VisibilityScore);
        }
        else if (u.Channel == ChannelType.Influencer && u.Influencer != null)
        {
            iExt = new InfluencerListExt(
                u.Influencer.PrimaryPlatform.ToString(),
                u.Influencer.Handle,
                u.Influencer.IsPlatformVerified,
                u.Influencer.FollowerCount,
                u.Influencer.EngagementRate,
                u.Influencer.Tier.ToString(),
                u.Influencer.ContentCategories,
                u.Influencer.Languages,
                u.Influencer.PrimaryAudienceCity?.Name);
        }

        return new InventoryListDto(
            u.Id, u.Code, u.Channel.ToString(), u.Name, u.Description,
            u.BasePriceMonthly, u.BasePricePerUnit, u.UnitLabel, u.Currency,
            u.EstimatedReachDaily, u.EstimatedImpressionsDaily,
            u.AvgRating, u.RatingCount,
            u.CoverImageUrl ?? ExtractFirstImage(u.ImagesJson),
            true, hExt, iExt);
    }

    private static string? ExtractFirstImage(string imagesJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(imagesJson);
            var first = doc.RootElement.EnumerateArray().FirstOrDefault();
            if (first.ValueKind != JsonValueKind.Undefined && first.TryGetProperty("url", out var u))
                return u.GetString();
        }
        catch { }
        return null;
    }
}

// ============================================================
// DETAIL & TRENDING
// ============================================================
public record GetInventoryDetailQuery(Guid Id) : IRequest<InventoryDetailDto?>;

public class GetInventoryDetailQueryHandler : IRequestHandler<GetInventoryDetailQuery, InventoryDetailDto?>
{
    private readonly IInventoryRepository _repo;
    public GetInventoryDetailQueryHandler(IInventoryRepository repo) => _repo = repo;

    public async Task<InventoryDetailDto?> Handle(GetInventoryDetailQuery req, CancellationToken ct)
    {
        var u = await _repo.GetByIdAsync(req.Id, ct);
        if (u == null) return null;

        await _repo.IncrementViewCountAsync(u.Id, ct);

        HoardingDetailExt? hExt = null;
        InfluencerDetailExt? iExt = null;

        if (u.Channel == ChannelType.Hoarding && u.Hoarding != null)
        {
            hExt = new HoardingDetailExt(
                u.Hoarding.Type.ToString(),
                u.Hoarding.Illumination.ToString(),
                new LocationDto(
                    u.Hoarding.AreaId, u.Hoarding.Area?.Name,
                    u.Hoarding.Area?.CityId, u.Hoarding.Area?.City?.Name,
                    u.Hoarding.Area?.City?.State?.Name,
                    u.Hoarding.Address, u.Hoarding.Landmark,
                    (decimal)u.Hoarding.Location.Y, (decimal)u.Hoarding.Location.X,
                    u.Hoarding.Area?.Pincode),
                u.Hoarding.WidthFt, u.Hoarding.HeightFt,
                u.Hoarding.FacingDirection,
                u.Hoarding.VisibilityScore,
                u.Hoarding.Panorama360Url, u.Hoarding.StreetViewUrl,
                u.Hoarding.PrintingCost, u.Hoarding.InstallationCost);
        }
        else if (u.Channel == ChannelType.Influencer && u.Influencer != null)
        {
            iExt = new InfluencerDetailExt(
                u.Influencer.PrimaryPlatform.ToString(),
                u.Influencer.Handle, u.Influencer.ProfileUrl,
                u.Influencer.IsPlatformVerified,
                u.Influencer.FollowerCount, u.Influencer.AvgViewsPerPost, u.Influencer.AvgLikesPerPost,
                u.Influencer.EngagementRate, u.Influencer.MetricsVerifiedAt,
                u.Influencer.Tier.ToString(),
                u.Influencer.ContentCategories, u.Influencer.Languages,
                u.Influencer.AudienceGeoSplitJson,
                u.Influencer.AudienceAgeSplitJson,
                u.Influencer.AudienceGenderSplitJson,
                u.Influencer.DeliverablePricingJson,
                u.Influencer.AvailableDeliverables.Select(d => d.ToString()).ToArray(),
                u.Influencer.ExcludesCategories,
                u.Influencer.RequiresPaidPartnershipTag,
                u.Influencer.TypicalTurnaroundDays,
                u.Influencer.SampleWorkUrlsJson);
        }

        return new InventoryDetailDto(
            u.Id, u.Code, u.Channel.ToString(), u.Name, u.Description, u.Status.ToString(),
            new PricingDto(
                u.BasePriceMonthly, u.BasePriceWeekly, u.BasePriceDaily,
                u.BasePricePerUnit, u.UnitLabel,
                u.SetupCost, u.GstPercentage, u.Currency),
            u.EstimatedReachDaily, u.EstimatedImpressionsDaily,
            u.AudienceProfileJson, u.AvgRating, u.RatingCount, u.BookingCount,
            ParseImages(u.ImagesJson), u.CoverImageUrl,
            hExt, iExt);
    }

    private static List<MediaImageDto> ParseImages(string imagesJson)
    {
        var result = new List<MediaImageDto>();
        try
        {
            using var doc = JsonDocument.Parse(imagesJson);
            int order = 0;
            foreach (var img in doc.RootElement.EnumerateArray())
            {
                result.Add(new MediaImageDto(
                    img.GetProperty("url").GetString() ?? "",
                    img.TryGetProperty("caption", out var cap) ? cap.GetString() : null,
                    img.TryGetProperty("is_primary", out var p) && p.GetBoolean(),
                    img.TryGetProperty("order", out var o) ? o.GetInt32() : order++));
            }
        }
        catch { }
        return result;
    }
}

public record GetTrendingInventoryQuery(string? Channel = null, int? CityId = null, int Limit = 10)
    : IRequest<IEnumerable<InventoryListDto>>;

public class GetTrendingInventoryQueryHandler : IRequestHandler<GetTrendingInventoryQuery, IEnumerable<InventoryListDto>>
{
    private readonly IInventoryRepository _repo;
    public GetTrendingInventoryQueryHandler(IInventoryRepository repo) => _repo = repo;

    public async Task<IEnumerable<InventoryListDto>> Handle(GetTrendingInventoryQuery req, CancellationToken ct)
    {
        var ch = SearchInventoryQueryHandler.ParseChannel(req.Channel);
        var items = await _repo.GetTrendingAsync(ch, req.CityId, req.Limit, ct);
        return items.Select(SearchInventoryQueryHandler.MapList);
    }
}

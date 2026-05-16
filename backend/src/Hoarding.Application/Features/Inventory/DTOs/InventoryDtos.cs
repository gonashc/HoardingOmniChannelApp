namespace Hoarding.Application.Features.Inventory.DTOs;

/// <summary>
/// Channel-agnostic list-card DTO. Channel-specific fields are nested.
/// </summary>
public record InventoryListDto(
    Guid Id,
    string Code,
    string Channel,                       // 'Hoarding' | 'Influencer' | ...
    string Name,
    string? Description,
    decimal? BasePriceMonthly,
    decimal? BasePricePerUnit,
    string? UnitLabel,
    string Currency,
    long? EstimatedReachDaily,
    long? EstimatedImpressionsDaily,
    decimal? AvgRating,
    int RatingCount,
    string? CoverImageUrl,
    bool IsAvailable,

    // Channel-specific extensions (only one populated)
    HoardingListExt? Hoarding,
    InfluencerListExt? Influencer
);

public record HoardingListExt(
    string Type,
    string Illumination,
    string? CityName,
    string? AreaName,
    decimal Latitude,
    decimal Longitude,
    decimal WidthFt,
    decimal HeightFt,
    short? VisibilityScore
);

public record InfluencerListExt(
    string Platform,
    string Handle,
    bool IsPlatformVerified,
    long FollowerCount,
    decimal? EngagementRate,
    string Tier,
    string[] Categories,
    string[] Languages,
    string? AudienceCityName
);

/// <summary>
/// Detailed inventory unit. The channel-specific extension carries the rich data.
/// </summary>
public record InventoryDetailDto(
    Guid Id,
    string Code,
    string Channel,
    string Name,
    string? Description,
    string Status,
    PricingDto Pricing,
    long? EstimatedReachDaily,
    long? EstimatedImpressionsDaily,
    string? AudienceProfileJson,
    decimal? AvgRating,
    int RatingCount,
    int BookingCount,
    List<MediaImageDto> Images,
    string? CoverImageUrl,

    HoardingDetailExt? Hoarding,
    InfluencerDetailExt? Influencer
);

public record HoardingDetailExt(
    string Type,
    string Illumination,
    LocationDto Location,
    decimal WidthFt,
    decimal HeightFt,
    string? FacingDirection,
    short? VisibilityScore,
    string? Panorama360Url,
    string? StreetViewUrl,
    decimal PrintingCost,
    decimal InstallationCost
);

public record InfluencerDetailExt(
    string Platform,
    string Handle,
    string? ProfileUrl,
    bool IsPlatformVerified,
    long FollowerCount,
    long? AvgViewsPerPost,
    long? AvgLikesPerPost,
    decimal? EngagementRate,
    DateTime? MetricsVerifiedAt,
    string Tier,
    string[] ContentCategories,
    string[] Languages,
    string? AudienceGeoSplitJson,
    string? AudienceAgeSplitJson,
    string? AudienceGenderSplitJson,
    string? DeliverablePricingJson,
    string[] AvailableDeliverables,
    string[] ExcludesCategories,
    bool RequiresPaidPartnershipTag,
    int TypicalTurnaroundDays,
    string? SampleWorkUrlsJson
);

public record LocationDto(
    int? AreaId,
    string? AreaName,
    int? CityId,
    string? CityName,
    string? StateName,
    string? Address,
    string? Landmark,
    decimal Latitude,
    decimal Longitude,
    string? Pincode
);

public record PricingDto(
    decimal? MonthlyRate,
    decimal? WeeklyRate,
    decimal? DailyRate,
    decimal? PricePerUnit,
    string? UnitLabel,
    decimal SetupCost,
    decimal GstPercentage,
    string Currency
);

public record MediaImageDto(string Url, string? Caption, bool IsPrimary, int Order);

public record QuoteResponseDto(
    Guid InventoryUnitId,
    string Channel,
    DateOnly StartDate,
    DateOnly EndDate,
    int DurationDays,
    decimal BaseAmount,
    decimal SetupCost,
    decimal Subtotal,
    decimal GstAmount,
    decimal TotalAmount,
    string Currency,
    string Breakdown
);

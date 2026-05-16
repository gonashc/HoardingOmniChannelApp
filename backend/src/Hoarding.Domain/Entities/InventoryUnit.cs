using Hoarding.Domain.Common;
using Hoarding.Domain.Enums;
using NetTopologySuite.Geometries;

namespace Hoarding.Domain.Entities;

/// <summary>
/// Channel-agnostic inventory unit. Each unit has a channel-specific
/// extension (HoardingSpec, InfluencerSpec, etc.) accessed via 1:1 navigation.
/// </summary>
public class InventoryUnit : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public ChannelType Channel { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.PendingApproval;

    public Guid? OwnerId { get; set; }

    // Pricing (one or more of these will be populated depending on channel)
    public decimal? BasePriceMonthly { get; set; }
    public decimal? BasePriceWeekly { get; set; }
    public decimal? BasePriceDaily { get; set; }
    public decimal? BasePricePerUnit { get; set; }       // per post / per spot / per episode
    public string? UnitLabel { get; set; }                // 'month' | 'post' | 'spot' | 'episode'
    public decimal SetupCost { get; set; }
    public decimal GstPercentage { get; set; } = 18.00m;
    public string Currency { get; set; } = "INR";

    // Audience signals (channel-agnostic)
    public long? EstimatedReachDaily { get; set; }
    public long? EstimatedImpressionsDaily { get; set; }
    public string AudienceProfileJson { get; set; } = "{}";

    // Trending / popularity (UC-10)
    public int BookingCount { get; set; }
    public int ViewCount { get; set; }
    public decimal? AvgRating { get; set; }
    public int RatingCount { get; set; }

    // Media
    public string ImagesJson { get; set; } = "[]";
    public string? CoverImageUrl { get; set; }

    // Channel-specific extensions (1:1, only one will be non-null)
    public HoardingSpec? Hoarding { get; set; }
    public InfluencerSpec? Influencer { get; set; }

    // Owner navigation
    public User? Owner { get; set; }
}

/// <summary>
/// Channel-specific data for hoardings (billboards, unipoles, gantries, DOOH, etc.)
/// </summary>
public class HoardingSpec
{
    public Guid InventoryUnitId { get; set; }
    public HoardingType Type { get; set; }
    public IlluminationType Illumination { get; set; } = IlluminationType.Frontlit;

    public int? AreaId { get; set; }
    public string? Address { get; set; }
    public string? Landmark { get; set; }
    public Point Location { get; set; } = null!;       // PostGIS geography point
    public string? GooglePlaceId { get; set; }
    public string? StreetViewUrl { get; set; }

    public decimal WidthFt { get; set; }
    public decimal HeightFt { get; set; }
    public string? FacingDirection { get; set; }

    public short? VisibilityScore { get; set; }
    public string? Panorama360Url { get; set; }
    public string PastBrandLogosJson { get; set; } = "[]";
    public decimal PrintingCost { get; set; }
    public decimal InstallationCost { get; set; }

    // Navigation
    public InventoryUnit InventoryUnit { get; set; } = null!;
    public Area? Area { get; set; }
}

/// <summary>
/// Channel-specific data for influencers / creators.
/// </summary>
public class InfluencerSpec
{
    public Guid InventoryUnitId { get; set; }

    public SocialPlatform PrimaryPlatform { get; set; }
    public string Handle { get; set; } = string.Empty;
    public string? ProfileUrl { get; set; }
    public bool IsPlatformVerified { get; set; }
    public string? PlatformUserId { get; set; }

    public long FollowerCount { get; set; }
    public long? AvgViewsPerPost { get; set; }
    public long? AvgLikesPerPost { get; set; }
    public long? AvgCommentsPerPost { get; set; }
    public decimal? EngagementRate { get; set; }
    public DateTime? MetricsVerifiedAt { get; set; }

    public CreatorTier Tier { get; set; }
    public string[] ContentCategories { get; set; } = Array.Empty<string>();
    public string[] Languages { get; set; } = Array.Empty<string>();

    public int? PrimaryAudienceCityId { get; set; }
    public string AudienceGeoSplitJson { get; set; } = "{}";
    public string AudienceAgeSplitJson { get; set; } = "{}";
    public string AudienceGenderSplitJson { get; set; } = "{}";

    public string DeliverablePricingJson { get; set; } = "{}";
    public DeliverableType[] AvailableDeliverables { get; set; } = Array.Empty<DeliverableType>();

    public string[] AcceptsCategories { get; set; } = Array.Empty<string>();
    public string[] ExcludesCategories { get; set; } = Array.Empty<string>();
    public bool RequiresPaidPartnershipTag { get; set; } = true;
    public int TypicalTurnaroundDays { get; set; } = 7;

    public string SampleWorkUrlsJson { get; set; } = "[]";

    // Navigation
    public InventoryUnit InventoryUnit { get; set; } = null!;
    public City? PrimaryAudienceCity { get; set; }
}

// Geo entities unchanged
public class Area
{
    public int Id { get; set; }
    public int CityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Pincode { get; set; }
    public Point? Centroid { get; set; }
    public City City { get; set; } = null!;
}

public class City
{
    public int Id { get; set; }
    public int StateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public short Tier { get; set; } = 1;
    public Point? Centroid { get; set; }
    public State State { get; set; } = null!;
}

public class State
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

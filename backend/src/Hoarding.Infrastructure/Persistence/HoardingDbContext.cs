using Hoarding.Domain.Entities;
using Hoarding.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.RegularExpressions;

namespace Hoarding.Infrastructure.Persistence;

public class HoardingDbContext : DbContext
{
    public HoardingDbContext(DbContextOptions<HoardingDbContext> options) : base(options) { }

    // ============================================================
    // Snake-case converters for Postgres ENUMs
    // ============================================================
    // The Postgres ENUMs in the schema (listing_status, channel_type,
    // hoarding_type, illumination_type, social_platform, creator_tier,
    // deliverable_type, booking_status, campaign_status, creative_status,
    // payment_status, user_role) use lowercase snake_case values such as
    // 'pending_approval', 'dooh', 'twitter_x'. C# enum names are PascalCase
    // ('PendingApproval', 'Dooh', 'TwitterX'). Without these converters EF
    // emits 'Active' and Postgres rejects it because the ENUM has 'active'.

    private static string ToSnake(string name) =>
        Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();

    private static ValueConverter<TEnum, string> SnakeEnumConverter<TEnum>() where TEnum : struct, Enum =>
        new(v => ToSnake(v.ToString()),
            s => (TEnum)Enum.Parse(typeof(TEnum), s.Replace("_", ""), true));

    private static readonly ValueConverter<UserRole, string>         UserRoleConv         = SnakeEnumConverter<UserRole>();
    private static readonly ValueConverter<ChannelType, string>      ChannelTypeConv      = SnakeEnumConverter<ChannelType>();
    private static readonly ValueConverter<HoardingType, string>     HoardingTypeConv     = SnakeEnumConverter<HoardingType>();
    private static readonly ValueConverter<IlluminationType, string> IlluminationConv     = SnakeEnumConverter<IlluminationType>();
    private static readonly ValueConverter<SocialPlatform, string>   SocialPlatformConv   = SnakeEnumConverter<SocialPlatform>();
    private static readonly ValueConverter<CreatorTier, string>      CreatorTierConv      = SnakeEnumConverter<CreatorTier>();
    private static readonly ValueConverter<DeliverableType, string>  DeliverableTypeConv  = SnakeEnumConverter<DeliverableType>();
    private static readonly ValueConverter<ListingStatus, string>    ListingStatusConv    = SnakeEnumConverter<ListingStatus>();
    private static readonly ValueConverter<BookingStatus, string>    BookingStatusConv    = SnakeEnumConverter<BookingStatus>();
    private static readonly ValueConverter<CampaignStatus, string>   CampaignStatusConv   = SnakeEnumConverter<CampaignStatus>();
    private static readonly ValueConverter<CreativeStatus, string>   CreativeStatusConv   = SnakeEnumConverter<CreativeStatus>();
    private static readonly ValueConverter<PaymentStatus, string>    PaymentStatusConv    = SnakeEnumConverter<PaymentStatus>();

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<InventoryUnit> InventoryUnits => Set<InventoryUnit>();
    public DbSet<HoardingSpec> HoardingSpecs => Set<HoardingSpec>();
    public DbSet<InfluencerSpec> InfluencerSpecs => Set<InfluencerSpec>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<State> States => Set<State>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<MediaPlan> MediaPlans => Set<MediaPlan>();
    public DbSet<MediaPlanItem> MediaPlanItems => Set<MediaPlanItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.HasPostgresExtension("uuid-ossp");
        modelBuilder.HasPostgresExtension("pg_trgm");

        // ---------- USER ----------
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20);
            b.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
            b.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(255).IsRequired();
            b.Property(x => x.Role).HasColumnName("role").HasConversion(UserRoleConv);
            b.Property(x => x.AuthProvider).HasColumnName("auth_provider").HasMaxLength(50);
            b.Property(x => x.AuthProviderId).HasColumnName("auth_provider_id").HasMaxLength(255);
            b.Property(x => x.EmailVerified).HasColumnName("email_verified");
            b.Property(x => x.PhoneVerified).HasColumnName("phone_verified");
            b.Property(x => x.IsActive).HasColumnName("is_active");
            b.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            b.HasOne(x => x.Profile).WithOne(p => p.User).HasForeignKey<UserProfile>(p => p.UserId);
        });

        modelBuilder.Entity<UserProfile>(b =>
        {
            b.ToTable("user_profiles");
            b.HasKey(x => x.UserId);
            b.Property(x => x.UserId).HasColumnName("user_id");
            b.Property(x => x.CompanyName).HasColumnName("company_name").HasMaxLength(255);
            b.Property(x => x.Gstin).HasColumnName("gstin").HasMaxLength(20);
            b.Property(x => x.PanNumber).HasColumnName("pan_number").HasMaxLength(10);
            b.Property(x => x.BillingAddress).HasColumnName("billing_address");
            b.Property(x => x.BillingCity).HasColumnName("billing_city").HasMaxLength(100);
            b.Property(x => x.BillingState).HasColumnName("billing_state").HasMaxLength(100);
            b.Property(x => x.BillingPincode).HasColumnName("billing_pincode").HasMaxLength(10);
            b.Property(x => x.ProfileImageUrl).HasColumnName("profile_image_url");
            b.Property(x => x.Bio).HasColumnName("bio");
            b.Property(x => x.Website).HasColumnName("website").HasMaxLength(255);
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        // ---------- GEO ----------
        modelBuilder.Entity<State>(b =>
        {
            b.ToTable("states");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(5);
        });

        modelBuilder.Entity<City>(b =>
        {
            b.ToTable("cities");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.StateId).HasColumnName("state_id");
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
            b.Property(x => x.Tier).HasColumnName("tier");
            b.Property(x => x.Centroid).HasColumnName("centroid").HasColumnType("geography(Point,4326)");
            b.HasOne(x => x.State).WithMany().HasForeignKey(x => x.StateId);
        });

        modelBuilder.Entity<Area>(b =>
        {
            b.ToTable("areas");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.CityId).HasColumnName("city_id");
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(150);
            b.Property(x => x.Pincode).HasColumnName("pincode").HasMaxLength(10);
            b.Property(x => x.Centroid).HasColumnName("centroid").HasColumnType("geography(Point,4326)");
            b.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId);
        });

        // ---------- INVENTORY UNIT ----------
        modelBuilder.Entity<InventoryUnit>(b =>
        {
            b.ToTable("inventory_units");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Channel).HasColumnName("channel").HasConversion(ChannelTypeConv);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.Status).HasColumnName("status").HasConversion(ListingStatusConv);
            b.Property(x => x.OwnerId).HasColumnName("owner_id");
            b.Property(x => x.BasePriceMonthly).HasColumnName("base_price_monthly").HasPrecision(12, 2);
            b.Property(x => x.BasePriceWeekly).HasColumnName("base_price_weekly").HasPrecision(12, 2);
            b.Property(x => x.BasePriceDaily).HasColumnName("base_price_daily").HasPrecision(12, 2);
            b.Property(x => x.BasePricePerUnit).HasColumnName("base_price_per_unit").HasPrecision(12, 2);
            b.Property(x => x.UnitLabel).HasColumnName("unit_label").HasMaxLength(50);
            b.Property(x => x.SetupCost).HasColumnName("setup_cost").HasPrecision(10, 2);
            b.Property(x => x.GstPercentage).HasColumnName("gst_percentage").HasPrecision(5, 2);
            b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
            b.Property(x => x.EstimatedReachDaily).HasColumnName("estimated_reach_daily");
            b.Property(x => x.EstimatedImpressionsDaily).HasColumnName("estimated_impressions_daily");
            b.Property(x => x.AudienceProfileJson).HasColumnName("audience_profile").HasColumnType("jsonb");
            b.Property(x => x.BookingCount).HasColumnName("booking_count");
            b.Property(x => x.ViewCount).HasColumnName("view_count");
            b.Property(x => x.AvgRating).HasColumnName("avg_rating").HasPrecision(3, 2);
            b.Property(x => x.RatingCount).HasColumnName("rating_count");
            b.Property(x => x.ImagesJson).HasColumnName("images").HasColumnType("jsonb");
            b.Property(x => x.CoverImageUrl).HasColumnName("cover_image_url");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            b.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId);
            b.HasOne(x => x.Hoarding).WithOne(h => h.InventoryUnit).HasForeignKey<HoardingSpec>(h => h.InventoryUnitId);
            b.HasOne(x => x.Influencer).WithOne(i => i.InventoryUnit).HasForeignKey<InfluencerSpec>(i => i.InventoryUnitId);
        });

        // ---------- HOARDING SPEC ----------
        modelBuilder.Entity<HoardingSpec>(b =>
        {
            b.ToTable("hoarding_specs");
            b.HasKey(x => x.InventoryUnitId);
            b.Property(x => x.InventoryUnitId).HasColumnName("inventory_unit_id");
            b.Property(x => x.Type).HasColumnName("type").HasConversion(HoardingTypeConv);
            b.Property(x => x.Illumination).HasColumnName("illumination").HasConversion(IlluminationConv);
            b.Property(x => x.AreaId).HasColumnName("area_id");
            b.Property(x => x.Address).HasColumnName("address");
            b.Property(x => x.Landmark).HasColumnName("landmark").HasMaxLength(255);
            b.Property(x => x.Location).HasColumnName("location").HasColumnType("geography(Point,4326)").IsRequired();
            b.Property(x => x.GooglePlaceId).HasColumnName("google_place_id").HasMaxLength(255);
            b.Property(x => x.StreetViewUrl).HasColumnName("street_view_url");
            b.Property(x => x.WidthFt).HasColumnName("width_ft").HasPrecision(8, 2);
            b.Property(x => x.HeightFt).HasColumnName("height_ft").HasPrecision(8, 2);
            b.Property(x => x.FacingDirection).HasColumnName("facing_direction").HasMaxLength(50);
            b.Property(x => x.VisibilityScore).HasColumnName("visibility_score");
            b.Property(x => x.Panorama360Url).HasColumnName("panorama_360_url");
            b.Property(x => x.PastBrandLogosJson).HasColumnName("past_brand_logos").HasColumnType("jsonb");
            b.Property(x => x.PrintingCost).HasColumnName("printing_cost").HasPrecision(10, 2);
            b.Property(x => x.InstallationCost).HasColumnName("installation_cost").HasPrecision(10, 2);
            b.HasOne(x => x.Area).WithMany().HasForeignKey(x => x.AreaId);
        });

        // ---------- INFLUENCER SPEC ----------
        modelBuilder.Entity<InfluencerSpec>(b =>
        {
            b.ToTable("influencer_specs");
            b.HasKey(x => x.InventoryUnitId);
            b.Property(x => x.InventoryUnitId).HasColumnName("inventory_unit_id");
            b.Property(x => x.PrimaryPlatform).HasColumnName("primary_platform").HasConversion(SocialPlatformConv);
            b.Property(x => x.Handle).HasColumnName("handle").HasMaxLength(255).IsRequired();
            b.Property(x => x.ProfileUrl).HasColumnName("profile_url");
            b.Property(x => x.IsPlatformVerified).HasColumnName("is_platform_verified");
            b.Property(x => x.PlatformUserId).HasColumnName("platform_user_id").HasMaxLength(255);
            b.Property(x => x.FollowerCount).HasColumnName("follower_count");
            b.Property(x => x.AvgViewsPerPost).HasColumnName("avg_views_per_post");
            b.Property(x => x.AvgLikesPerPost).HasColumnName("avg_likes_per_post");
            b.Property(x => x.AvgCommentsPerPost).HasColumnName("avg_comments_per_post");
            b.Property(x => x.EngagementRate).HasColumnName("engagement_rate").HasPrecision(5, 2);
            b.Property(x => x.MetricsVerifiedAt).HasColumnName("metrics_verified_at");
            b.Property(x => x.Tier).HasColumnName("tier").HasConversion(CreatorTierConv);
            b.Property(x => x.ContentCategories).HasColumnName("content_categories");
            b.Property(x => x.Languages).HasColumnName("languages");
            b.Property(x => x.PrimaryAudienceCityId).HasColumnName("primary_audience_city_id");
            b.Property(x => x.AudienceGeoSplitJson).HasColumnName("audience_geo_split").HasColumnType("jsonb");
            b.Property(x => x.AudienceAgeSplitJson).HasColumnName("audience_age_split").HasColumnType("jsonb");
            b.Property(x => x.AudienceGenderSplitJson).HasColumnName("audience_gender_split").HasColumnType("jsonb");
            b.Property(x => x.DeliverablePricingJson).HasColumnName("deliverable_pricing").HasColumnType("jsonb");
            b.Property(x => x.AvailableDeliverables).HasColumnName("available_deliverables")
                .HasConversion(
                    v => v.Select(d => ToSnake(d.ToString())).ToArray(),
                    v => v.Select(s => (DeliverableType)Enum.Parse(typeof(DeliverableType), s.Replace("_", ""), true)).ToArray());
            b.Property(x => x.AcceptsCategories).HasColumnName("accepts_categories");
            b.Property(x => x.ExcludesCategories).HasColumnName("excludes_categories");
            b.Property(x => x.RequiresPaidPartnershipTag).HasColumnName("requires_paid_partnership_tag");
            b.Property(x => x.TypicalTurnaroundDays).HasColumnName("typical_turnaround_days");
            b.Property(x => x.SampleWorkUrlsJson).HasColumnName("sample_work_urls").HasColumnType("jsonb");
            b.HasOne(x => x.PrimaryAudienceCity).WithMany().HasForeignKey(x => x.PrimaryAudienceCityId);
        });

        // ---------- CAMPAIGN ----------
        modelBuilder.Entity<Campaign>(b =>
        {
            b.ToTable("campaigns");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(50);
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
            b.Property(x => x.AdvertiserId).HasColumnName("advertiser_id");
            b.Property(x => x.PlannerId).HasColumnName("planner_id");
            b.Property(x => x.Status).HasColumnName("status").HasConversion(CampaignStatusConv);
            b.Property(x => x.Objective).HasColumnName("objective").HasMaxLength(255);
            b.Property(x => x.Brief).HasColumnName("brief");
            b.Property(x => x.TargetChannels).HasColumnName("target_channels")
                .HasConversion(
                    v => v.Select(c => ToSnake(c.ToString())).ToArray(),
                    v => v.Select(s => (ChannelType)Enum.Parse(typeof(ChannelType), s.Replace("_", ""), true)).ToArray());
            b.Property(x => x.Budget).HasColumnName("budget").HasPrecision(14, 2);
            b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
            b.Property(x => x.StartDate).HasColumnName("start_date");
            b.Property(x => x.EndDate).HasColumnName("end_date");
            b.Property(x => x.Notes).HasColumnName("notes");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            b.HasOne(x => x.Advertiser).WithMany().HasForeignKey(x => x.AdvertiserId);
        });

        // ---------- BOOKING ----------
        modelBuilder.Entity<Booking>(b =>
        {
            b.ToTable("bookings");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.BookingRef).HasColumnName("booking_ref").HasMaxLength(20);
            b.HasIndex(x => x.BookingRef).IsUnique();
            b.Property(x => x.CampaignId).HasColumnName("campaign_id");
            b.Property(x => x.InventoryUnitId).HasColumnName("inventory_unit_id").IsRequired();
            b.Property(x => x.Channel).HasColumnName("channel").HasConversion(ChannelTypeConv);
            b.Property(x => x.BookedBy).HasColumnName("booked_by");
            b.Property(x => x.Status).HasColumnName("status").HasConversion(BookingStatusConv);
            b.Property(x => x.StartDate).HasColumnName("start_date");
            b.Property(x => x.EndDate).HasColumnName("end_date");
            b.Property(x => x.DeliverableSpecJson).HasColumnName("deliverable_spec").HasColumnType("jsonb");
            b.Property(x => x.BasePrice).HasColumnName("base_price").HasPrecision(12, 2);
            b.Property(x => x.SetupCost).HasColumnName("setup_cost").HasPrecision(10, 2);
            b.Property(x => x.DiscountPct).HasColumnName("discount_pct").HasPrecision(5, 2);
            b.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasPrecision(12, 2);
            b.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(12, 2);
            b.Property(x => x.GstPercentage).HasColumnName("gst_percentage").HasPrecision(5, 2);
            b.Property(x => x.GstAmount).HasColumnName("gst_amount").HasPrecision(12, 2);
            b.Property(x => x.TotalAmount).HasColumnName("total_amount").HasPrecision(12, 2);
            b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
            b.Property(x => x.ApprovedBy).HasColumnName("approved_by");
            b.Property(x => x.ApprovedAt).HasColumnName("approved_at");
            b.Property(x => x.RejectionReason).HasColumnName("rejection_reason");
            b.Property(x => x.CancelledAt).HasColumnName("cancelled_at");
            b.Property(x => x.CancellationReason).HasColumnName("cancellation_reason");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            b.HasOne(x => x.Campaign).WithMany(c => c.Bookings).HasForeignKey(x => x.CampaignId);
            b.HasOne(x => x.InventoryUnit).WithMany().HasForeignKey(x => x.InventoryUnitId);
            b.HasOne(x => x.Booker).WithMany().HasForeignKey(x => x.BookedBy);
            b.Ignore(x => x.DurationDays);
        });

        // ---------- MEDIA PLAN ----------
        modelBuilder.Entity<MediaPlan>(b =>
        {
            b.ToTable("media_plans");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.UserId).HasColumnName("user_id");
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
            b.Property(x => x.IsActiveCart).HasColumnName("is_active_cart");
            b.Property(x => x.ShareToken).HasColumnName("share_token").HasMaxLength(64);
            b.Property(x => x.IsShared).HasColumnName("is_shared");
            b.Property(x => x.Notes).HasColumnName("notes");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            b.HasMany(x => x.Items).WithOne(i => i.MediaPlan).HasForeignKey(i => i.MediaPlanId);
        });

        modelBuilder.Entity<MediaPlanItem>(b =>
        {
            b.ToTable("media_plan_items");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.MediaPlanId).HasColumnName("media_plan_id");
            b.Property(x => x.InventoryUnitId).HasColumnName("inventory_unit_id");
            b.Property(x => x.StartDate).HasColumnName("start_date");
            b.Property(x => x.EndDate).HasColumnName("end_date");
            b.Property(x => x.DeliverableSpecJson).HasColumnName("deliverable_spec").HasColumnType("jsonb");
            b.Property(x => x.QuotedPrice).HasColumnName("quoted_price").HasPrecision(12, 2);
            b.Property(x => x.QuotedAt).HasColumnName("quoted_at");
            b.Property(x => x.Notes).HasColumnName("notes");
            b.HasOne(x => x.InventoryUnit).WithMany().HasForeignKey(x => x.InventoryUnitId);
        });

        base.OnModelCreating(modelBuilder);
    }
}

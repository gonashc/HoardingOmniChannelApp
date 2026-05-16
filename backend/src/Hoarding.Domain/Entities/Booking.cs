using Hoarding.Domain.Common;
using Hoarding.Domain.Enums;

namespace Hoarding.Domain.Entities;

public class Campaign : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? AdvertiserId { get; set; }
    public Guid? PlannerId { get; set; }
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public string? Objective { get; set; }
    public string? Brief { get; set; }
    public ChannelType[] TargetChannels { get; set; } = Array.Empty<ChannelType>();
    public decimal? Budget { get; set; }
    public string Currency { get; set; } = "INR";
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Notes { get; set; }

    public User? Advertiser { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

public class Booking : AuditableEntity
{
    public string BookingRef { get; set; } = string.Empty;
    public Guid? CampaignId { get; set; }
    public Guid InventoryUnitId { get; set; }
    public ChannelType Channel { get; set; }                    // denormalised for fast filtering
    public Guid? BookedBy { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Cart;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Channel-specific deliverable spec (JSONB).
    /// For hoardings: null. For creators: {"deliverable":"reel","quantity":2,"caption_brief":"..."}.
    /// </summary>
    public string? DeliverableSpecJson { get; set; }

    // Pricing
    public decimal BasePrice { get; set; }
    public decimal SetupCost { get; set; }
    public decimal DiscountPct { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal GstPercentage { get; set; } = 18.00m;
    public decimal GstAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "INR";

    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public Campaign? Campaign { get; set; }
    public InventoryUnit InventoryUnit { get; set; } = null!;
    public User? Booker { get; set; }

    public int DurationDays => EndDate.DayNumber - StartDate.DayNumber + 1;
}

public class MediaPlan : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = "My Media Plan";
    public bool IsActiveCart { get; set; } = true;
    public string? ShareToken { get; set; }
    public bool IsShared { get; set; }
    public string? Notes { get; set; }

    public ICollection<MediaPlanItem> Items { get; set; } = new List<MediaPlanItem>();
    public User User { get; set; } = null!;
}

public class MediaPlanItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MediaPlanId { get; set; }
    public Guid InventoryUnitId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? DeliverableSpecJson { get; set; }
    public decimal? QuotedPrice { get; set; }
    public DateTime QuotedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public MediaPlan MediaPlan { get; set; } = null!;
    public InventoryUnit InventoryUnit { get; set; } = null!;
}

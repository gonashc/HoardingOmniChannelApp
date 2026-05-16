namespace Hoarding.Domain.Enums;

public enum ChannelType
{
    Hoarding,
    Influencer,
    Radio,
    Print,
    DoohNetwork,
    Podcast,
    Tv,
    Cinema
}

public enum UserRole
{
    Admin,
    Advertiser,
    MediaPlanner,
    MediaOwner,
    Creator,
    FieldAgent
}

public enum HoardingType
{
    Billboard,
    Unipole,
    Gantry,
    BusShelter,
    MetroPillar,
    Dooh,
    WallMount,
    Other
}

public enum IlluminationType
{
    Frontlit,
    Backlit,
    Digital,
    NonLit
}

public enum SocialPlatform
{
    Instagram,
    Youtube,
    TwitterX,
    Linkedin,
    Tiktok,
    Facebook,
    Threads
}

public enum CreatorTier
{
    Nano,        // < 10K
    Micro,       // 10K – 100K
    Mid,         // 100K – 500K
    Macro,       // 500K – 1M
    Mega,        // 1M – 10M
    Celebrity    // 10M+
}

public enum DeliverableType
{
    Post, Story, Reel, Short, Video, Live, Tweet, Thread, Integration
}

public enum ListingStatus
{
    Draft, PendingApproval, Active, Inactive, Rejected
}

public enum BookingStatus
{
    Cart, PendingPayment, PendingApproval, Confirmed, Active, Completed, Cancelled
}

public enum CampaignStatus
{
    Draft, Scheduled, Active, Paused, Completed, Cancelled
}

public enum CreativeStatus
{
    Uploaded, UnderReview, Approved, Rejected, PrintReady, Published
}

public enum PaymentStatus
{
    Pending, Partial, Paid, Refunded, Failed
}

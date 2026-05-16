using Hoarding.Domain.Common;
using Hoarding.Domain.Enums;

namespace Hoarding.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PasswordHash { get; set; }
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Advertiser;
    public string AuthProvider { get; set; } = "local";
    public string? AuthProviderId { get; set; }
    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    // Navigation
    public UserProfile? Profile { get; set; }
}

public class UserProfile
{
    public Guid UserId { get; set; }
    public string? CompanyName { get; set; }
    public string? Gstin { get; set; }
    public string? PanNumber { get; set; }
    public string? BillingAddress { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingPincode { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
    public string? Website { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}

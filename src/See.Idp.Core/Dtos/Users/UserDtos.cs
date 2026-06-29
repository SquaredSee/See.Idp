namespace See.Idp.Core.Dtos.Users;

/// <summary>Represents a summarized view of a user account.</summary>
public sealed record UserSummaryDto(
    string UserId,
    string? UserName,
    string? Email,
    bool EmailConfirmed,
    bool IsAdmin,
    bool IsLockedOut
);

/// <summary>Represents the editable profile of a user account.</summary>
/// <param name="Email">The email address (read-only).</param>
/// <param name="PhoneNumber">The phone number.</param>
public sealed record UserProfileDto(string? Email, string? PhoneNumber);

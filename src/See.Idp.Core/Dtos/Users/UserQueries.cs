namespace See.Idp.Core.Dtos.Users;

/// <summary>
///     Represents a query to list users.
/// </summary>
/// <param name="SearchTerm">Optional search term applied to email and user name.</param>
/// <param name="Skip">Optional number of results to skip.</param>
/// <param name="Take">Optional maximum number of results to return.</param>
public sealed record ListUsersQuery(string? SearchTerm = null, int Skip = 0, int? Take = null);

/// <summary>
///     Represents a query to retrieve a user ID by email address.
/// </summary>
/// <param name="Email">The email address to look up.</param>
public sealed record FindUserByEmailQuery(string Email);

/// <summary>
///     Represents a query to retrieve a user's profile by their ID.
/// </summary>
/// <param name="UserId">The ID of the user.</param>
public sealed record GetUserProfileQuery(string UserId);

/// <summary>
///     Represents a query to generate an email confirmation token for a user.
/// </summary>
/// <param name="UserId">The ID of the user.</param>
public sealed record GenerateEmailConfirmationTokenQuery(string UserId);

/// <summary>
///     Represents a summarized view of a user account.
/// </summary>
/// <param name="UserId">The identifier of the user.</param>
/// <param name="UserName">The user name.</param>
/// <param name="Email">The email address.</param>
/// <param name="EmailConfirmed">Indicates whether the email is confirmed.</param>
/// <param name="IsAdmin">Indicates whether the user has administrator privileges.</param>
/// <param name="IsLockedOut">Indicates whether the user account is locked out.</param>
public sealed record UserSummaryDto(
    string UserId,
    string? UserName,
    string? Email,
    bool EmailConfirmed,
    bool IsAdmin,
    bool IsLockedOut
);

/// <summary>
///     Represents the editable profile of a user account.
/// </summary>
/// <param name="Email">The email address (read-only).</param>
/// <param name="PhoneNumber">The phone number.</param>
public sealed record UserProfileDto(string? Email, string? PhoneNumber);

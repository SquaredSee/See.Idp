namespace See.Idp.Core.Dtos.Users;

/// <summary>
///     Represents a query to list users.
/// </summary>
/// <param name="CurrentUserId">The identifier of the current user, used to mark self entries.</param>
public sealed record ListUsersQuery(string? CurrentUserId = null);

/// <summary>
///     Represents a summarized view of a user account.
/// </summary>
/// <param name="UserId">The identifier of the user.</param>
/// <param name="UserName">The user name.</param>
/// <param name="Email">The email address.</param>
/// <param name="EmailConfirmed">Indicates whether the email is confirmed.</param>
/// <param name="IsAdmin">Indicates whether the user has administrator privileges.</param>
/// <param name="IsLockedOut">Indicates whether the user account is locked out.</param>
/// <param name="IsCurrentUser">Indicates whether this user is the currently authenticated user.</param>
public sealed record UserSummaryDto(
    string UserId,
    string? UserName,
    string? Email,
    bool EmailConfirmed,
    bool IsAdmin,
    bool IsLockedOut,
    bool IsCurrentUser
);

namespace See.Idp.Core.Dtos.Users;

/// <summary>
///     Represents a command to create a role when it does not already exist.
/// </summary>
/// <param name="RoleName">The name of the role.</param>
public sealed record CreateRoleIfMissingCommand(string RoleName);

/// <summary>
///     Represents a command to create a user when an account with the same email does not already exist.
/// </summary>
/// <param name="Email">The email address of the user.</param>
/// <param name="Password">The password for the user, if one should be set.</param>
/// <param name="EmailConfirmed">Indicates whether the email should be marked as confirmed.</param>
public sealed record CreateUserIfMissingCommand(
    string Email,
    string? Password,
    bool EmailConfirmed = true
);

/// <summary>
///     Represents a command to add a user to a role when the membership does not already exist.
/// </summary>
/// <param name="UserId">The identifier of the user.</param>
/// <param name="RoleName">The name of the role.</param>
public sealed record AddUserToRoleIfMissingCommand(string UserId, string RoleName);

/// <summary>
///     Represents a command to grant or revoke administrator privileges for a user.
/// </summary>
/// <param name="TargetUserId">The identifier of the user being updated.</param>
/// <param name="CurrentUserId">The identifier of the user issuing the command.</param>
public sealed record ToggleUserAdminCommand(string TargetUserId, string? CurrentUserId);

/// <summary>
///     Represents a command to lock or unlock a user account.
/// </summary>
/// <param name="TargetUserId">The identifier of the user being updated.</param>
/// <param name="CurrentUserId">The identifier of the user issuing the command.</param>
public sealed record ToggleUserLockCommand(string TargetUserId, string? CurrentUserId);

/// <summary>
///     Represents a command to delete a user account.
/// </summary>
/// <param name="TargetUserId">The identifier of the user to delete.</param>
/// <param name="CurrentUserId">The identifier of the user issuing the command.</param>
public sealed record DeleteUserCommand(string TargetUserId, string? CurrentUserId);

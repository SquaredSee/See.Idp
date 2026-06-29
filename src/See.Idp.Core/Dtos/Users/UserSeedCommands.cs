namespace See.Idp.Core.Dtos.Users;

/// <summary>Represents a command to create a role when it does not already exist.</summary>
/// <param name="RoleName">The name of the role.</param>
public sealed record CreateRoleIfMissingCommand(string RoleName);

/// <summary>
///     Represents a command to create a user when an account with the same email does not
///     already exist.
/// </summary>
public sealed record CreateUserIfMissingCommand(
    string Email,
    string? Password,
    bool EmailConfirmed = true
);

/// <summary>
///     Represents a command to add a user to a role when the membership does not already
///     exist.
/// </summary>
public sealed record AddUserToRoleIfMissingCommand(string UserId, string RoleName);

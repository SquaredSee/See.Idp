namespace See.Idp.Core.Dtos.Users;

/// <summary>Represents a command to grant or revoke administrator privileges for a user.</summary>
public sealed record ToggleUserAdminCommand(string TargetUserId, string? CurrentUserId);

/// <summary>Represents a command to lock or unlock a user account.</summary>
public sealed record ToggleUserLockCommand(string TargetUserId, string? CurrentUserId);

/// <summary>Represents a command to delete a user account.</summary>
public sealed record DeleteUserCommand(string TargetUserId, string? CurrentUserId);

/// <summary>Represents a command to update a user's phone number.</summary>
/// <param name="UserId">The ID of the user to update.</param>
/// <param name="PhoneNumber">The new phone number, or <see langword="null"/> to clear it.</param>
public sealed record UpdatePhoneNumberCommand(string UserId, string? PhoneNumber);

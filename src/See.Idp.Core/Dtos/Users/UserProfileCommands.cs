namespace See.Idp.Core.Dtos.Users;

/// <summary>
///     Represents a command to update a user's phone number.
/// </summary>
/// <param name="UserId">The ID of the user to update.</param>
/// <param name="PhoneNumber">The new phone number, or <see langword="null"/> to clear it.</param>
public sealed record UpdatePhoneNumberCommand(string UserId, string? PhoneNumber);

/// <summary>
///     Represents a command to change a user's password.
/// </summary>
/// <param name="UserId">The ID of the user changing their password.</param>
/// <param name="OldPassword">The current password.</param>
/// <param name="NewPassword">The new password.</param>
public sealed record ChangePasswordCommand(string UserId, string OldPassword, string NewPassword);

/// <summary>
///     Represents a command to reset a user's password via a reset token.
/// </summary>
/// <param name="Email">The email address of the user resetting their password.</param>
/// <param name="Code">The Base64Url-encoded password reset token.</param>
/// <param name="NewPassword">The new password.</param>
public sealed record ResetPasswordCommand(string Email, string Code, string NewPassword);

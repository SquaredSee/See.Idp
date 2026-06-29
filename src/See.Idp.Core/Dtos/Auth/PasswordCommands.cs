namespace See.Idp.Core.Dtos.Auth;

/// <summary>Represents a command to change a user's password.</summary>
/// <param name="UserId">The ID of the user changing their password.</param>
/// <param name="OldPassword">The current password.</param>
/// <param name="NewPassword">The new password.</param>
public sealed record ChangePasswordCommand(string UserId, string OldPassword, string NewPassword);

/// <summary>Represents a command to reset a user's password via a reset token.</summary>
/// <param name="Email">The email address of the user resetting their password.</param>
/// <param name="Code">The Base64Url-encoded password reset token.</param>
/// <param name="NewPassword">The new password.</param>
public sealed record ResetPasswordCommand(string Email, string Code, string NewPassword);

/// <summary>Represents a command to generate a password reset token for a user.</summary>
/// <param name="Email">The email address of the user requesting a password reset.</param>
public sealed record GeneratePasswordResetTokenCommand(string Email);

/// <summary>Represents the result of generating a password reset token.</summary>
/// <param name="Succeeded">Whether the token was generated successfully.</param>
/// <param name="Token">The Base64Url-encoded token, when successful.</param>
/// <param name="Error">An optional error message.</param>
public sealed record GeneratePasswordResetTokenResult(
    bool Succeeded,
    string? Token,
    string? Error = null
)
{
    /// <summary>Creates a successful result containing the token.</summary>
    public static GeneratePasswordResetTokenResult Success(string token) => new(true, token);

    /// <summary>
    ///     Creates a failed result with no error exposed — used when the user was not found
    ///     or their email is unconfirmed, to prevent user enumeration.
    /// </summary>
    public static GeneratePasswordResetTokenResult Failed() => new(false, null);
}

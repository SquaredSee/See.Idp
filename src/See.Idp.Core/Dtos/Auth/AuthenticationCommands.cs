namespace See.Idp.Core.Dtos.Auth;

/// <summary>
///     Represents a command to sign in a user with a username and password.
/// </summary>
/// <param name="Email">The email address of the user.</param>
/// <param name="Password">The password of the user.</param>
/// <param name="RememberMe">Indicates whether the user should be remembered on the device.</param>
public sealed record PasswordSignInCommand(string Email, string Password, bool RememberMe);

/// <summary>
///     Represents the result of a password sign-in attempt.
/// </summary>
/// <param name="Succeeded">Indicates whether the sign-in attempt was successful.</param>
/// <param name="IsLockedOut">Indicates whether the user is locked out.</param>
/// <param name="Error">The error message, if any.</param>
public sealed record PasswordSignInResult(
    bool Succeeded,
    bool IsLockedOut,
    bool RequiresTwoFactor = false,
    string? Error = null
)
{
    public static PasswordSignInResult Success() => new(true, false, false, null);

    public static PasswordSignInResult LockedOut() => new(false, true, false, null);

    public static PasswordSignInResult TwoFactorRequired() => new(false, false, true, null);

    public static PasswordSignInResult Failure(string error) => new(false, false, false, error);
}

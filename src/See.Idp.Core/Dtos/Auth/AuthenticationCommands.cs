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
public sealed record PasswordSignInResult(bool Succeeded, bool IsLockedOut, string? Error = null)
{
    /// <summary>
    ///     Creates a successful sign-in result.
    /// </summary>
    /// <returns>A successful sign-in result.</returns>
    public static PasswordSignInResult Success() => new(true, false, null);

    /// <summary>
    ///     Creates a result indicating the account is locked out.
    /// </summary>
    /// <returns>A locked-out sign-in result.</returns>
    public static PasswordSignInResult LockedOut() => new(false, true, null);

    /// <summary>
    ///     Creates a failed sign-in result.
    /// </summary>
    /// <param name="error">The sign-in error message.</param>
    /// <returns>A failed sign-in result.</returns>
    public static PasswordSignInResult Failure(string error) => new(false, false, error);
}

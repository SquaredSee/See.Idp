namespace See.Idp.Core.Dtos.Auth;

/// <summary>Completes a TOTP 2FA sign-in with an authenticator code.</summary>
/// <param name="Code">The 6-digit TOTP code from the authenticator app.</param>
/// <param name="RememberMe">Whether the original login requested a persistent cookie.</param>
/// <param name="RememberClient">Whether to suppress future 2FA challenges on this device.</param>
public sealed record TwoFactorSignInCommand(string Code, bool RememberMe, bool RememberClient);

/// <summary>Completes a 2FA sign-in using a recovery code.</summary>
/// <param name="Code">The recovery code.</param>
public sealed record RecoveryCodeSignInCommand(string Code);

/// <summary>Result of a 2FA sign-in attempt.</summary>
public sealed record TwoFactorSignInResult(
    bool Succeeded,
    bool IsLockedOut,
    bool IsNotAllowed,
    string? Error = null
)
{
    public static TwoFactorSignInResult Success() => new(true, false, false);

    public static TwoFactorSignInResult LockedOut() => new(false, true, false);

    public static TwoFactorSignInResult NotAllowed() => new(false, false, true);

    public static TwoFactorSignInResult Failure(string error) => new(false, false, false, error);
}

/// <summary>Verifies a TOTP code and enables 2FA for a user.</summary>
/// <param name="UserId">The user's identifier.</param>
/// <param name="VerificationCode">The TOTP code to verify before enabling.</param>
public sealed record EnableTwoFactorCommand(string UserId, string VerificationCode);

/// <summary>Disables 2FA for a user.</summary>
/// <param name="UserId">The user's identifier.</param>
public sealed record DisableTwoFactorCommand(string UserId);

/// <summary>Resets the authenticator key and disables 2FA for a user.</summary>
/// <param name="UserId">The user's identifier.</param>
public sealed record ResetAuthenticatorKeyCommand(string UserId);

/// <summary>Generates a fresh set of recovery codes for a user.</summary>
/// <param name="UserId">The user's identifier.</param>
public sealed record GenerateRecoveryCodesCommand(string UserId);

/// <summary>Result of generating new recovery codes.</summary>
public sealed record GenerateRecoveryCodesResult(
    bool Succeeded,
    System.Collections.Generic.IEnumerable<string> Codes,
    string? Error = null
)
{
    public static GenerateRecoveryCodesResult Success(
        System.Collections.Generic.IEnumerable<string> codes
    ) => new(true, codes);

    public static GenerateRecoveryCodesResult Failure(string error) =>
        new(false, System.Array.Empty<string>(), error);
}

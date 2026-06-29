using System;
using System.Collections.Generic;

namespace See.Idp.Core.Dtos.Auth;

/// <summary>Completes a TOTP 2FA sign-in with an authenticator code.</summary>
/// <param name="Code">The 6-digit TOTP code from the authenticator app.</param>
/// <param name="RememberMe">Whether the original login requested a persistent cookie.</param>
/// <param name="RememberClient">Whether to suppress future 2FA challenges on this device.</param>
public sealed record TwoFactorSignInCommand(string Code, bool RememberMe, bool RememberClient);

/// <summary>Result of a TOTP 2FA sign-in attempt.</summary>
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

/// <summary>Completes a 2FA sign-in using a recovery code.</summary>
/// <param name="Code">The recovery code.</param>
public sealed record RecoveryCodeSignInCommand(string Code);

/// <summary>Result of a recovery code sign-in attempt.</summary>
public sealed record RecoveryCodeSignInResult(
    bool Succeeded,
    bool IsLockedOut,
    bool IsNotAllowed,
    string? Error = null
)
{
    public static RecoveryCodeSignInResult Success() => new(true, false, false);

    public static RecoveryCodeSignInResult LockedOut() => new(false, true, false);

    public static RecoveryCodeSignInResult NotAllowed() => new(false, false, true);

    public static RecoveryCodeSignInResult Failure(string error) => new(false, false, false, error);
}

/// <summary>Provisions an authenticator key for a user if one does not already exist.</summary>
/// <param name="UserId">The user's identifier.</param>
public sealed record ProvisionAuthenticatorKeyCommand(string UserId);

/// <summary>Verifies a TOTP code and enables 2FA for a user.</summary>
/// <param name="UserId">The user's identifier.</param>
/// <param name="VerificationCode">The TOTP code to verify before enabling.</param>
public sealed record EnableTwoFactorCommand(string UserId, string VerificationCode);

/// <summary>Result of enabling two-factor authentication.</summary>
public sealed record EnableTwoFactorResult(
    bool Succeeded,
    IEnumerable<string> RecoveryCodes,
    string? Error = null
)
{
    public static EnableTwoFactorResult Success(IEnumerable<string> codes) => new(true, codes);

    public static EnableTwoFactorResult Failure(string error) =>
        new(false, Array.Empty<string>(), error);
}

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
    IEnumerable<string> Codes,
    string? Error = null
)
{
    public static GenerateRecoveryCodesResult Success(IEnumerable<string> codes) =>
        new(true, codes);

    public static GenerateRecoveryCodesResult Failure(string error) =>
        new(false, Array.Empty<string>(), error);
}

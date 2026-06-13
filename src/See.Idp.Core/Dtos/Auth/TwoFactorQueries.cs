namespace See.Idp.Core.Dtos.Auth;

/// <summary>Retrieves 2FA status and settings for a user.</summary>
/// <param name="UserId">The user's identifier.</param>
public sealed record GetTwoFactorInfoQuery(string UserId);

/// <summary>2FA status and settings for a user.</summary>
public sealed record TwoFactorInfo(
    bool IsTwoFactorEnabled,
    bool HasAuthenticator,
    int RecoveryCodesLeft,
    bool IsMachineRemembered
);

/// <summary>Retrieves TOTP setup data (shared key + QR code URI) for a user.</summary>
/// <param name="UserId">The user's identifier.</param>
public sealed record GetAuthenticatorSetupQuery(string UserId);

/// <summary>TOTP authenticator setup information.</summary>
public sealed record AuthenticatorSetupInfo(string SharedKey, string AuthenticatorUri);

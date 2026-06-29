namespace See.Idp.Core.Dtos.Auth;

/// <summary>2FA status and settings for a user.</summary>
public sealed record TwoFactorInfo(
    bool IsTwoFactorEnabled,
    bool HasAuthenticator,
    int RecoveryCodesLeft,
    bool IsMachineRemembered
);

/// <summary>TOTP authenticator setup information.</summary>
public sealed record AuthenticatorSetupInfo(string SharedKey, string AuthenticatorUri);

using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos.Auth;

namespace See.Idp.Core.Services.Auth;

/// <summary>Provides queries for a user's two-factor authentication state and setup.</summary>
public interface ITwoFactorQueryService
{
    /// <summary>Returns 2FA status and settings for the given user.</summary>
    Task<TwoFactorInfo?> GetTwoFactorInfoAsync(
        GetTwoFactorInfoQuery query,
        CancellationToken ct = default
    );

    /// <summary>Returns the TOTP shared key and QR code URI for authenticator app setup.</summary>
    Task<AuthenticatorSetupInfo?> GetAuthenticatorSetupAsync(
        GetAuthenticatorSetupQuery query,
        CancellationToken ct = default
    );
}

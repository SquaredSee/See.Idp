using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos.Auth;

namespace See.Idp.Core.Services.Auth;

/// <summary>Provides commands for authenticating and signing out users.</summary>
public interface IAuthenticationCommandService
{
    /// <summary>Attempts to sign in a user with a username and password.</summary>
    Task<PasswordSignInResult> PasswordSignInAsync(
        PasswordSignInCommand command,
        CancellationToken ct = default
    );

    /// <summary>Completes a 2FA sign-in with a TOTP authenticator code.</summary>
    Task<TwoFactorSignInResult> TwoFactorSignInAsync(
        TwoFactorSignInCommand command,
        CancellationToken ct = default
    );

    /// <summary>Completes a 2FA sign-in with a recovery code.</summary>
    Task<TwoFactorSignInResult> RecoveryCodeSignInAsync(
        RecoveryCodeSignInCommand command,
        CancellationToken ct = default
    );

    /// <summary>Signs out the current user.</summary>
    Task SignOutAsync(CancellationToken ct = default);
}

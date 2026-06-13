using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos.Auth;

namespace See.Idp.Core.Services.Auth;

/// <summary>
///     Provides commands for authenticating and signing out users.
/// </summary>
public interface IUserAuthenticationCommandService
{
    /// <summary>
    ///     Attempts to sign in a user with a username and password.
    /// </summary>
    /// <param name="command">The command containing sign-in credentials and options.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The sign-in result.</returns>
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

    /// <summary>
    ///     Signs out the current user.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous sign-out operation.</returns>
    Task SignOutAsync(CancellationToken ct = default);

    /// <summary>
    ///     Refreshes the current user's sign-in cookie to reflect any security stamp changes.
    ///     This method requires an active HTTP request context and silently no-ops when no
    ///     authenticated user is found (e.g. in background jobs or unit tests).
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RefreshSignInAsync(CancellationToken ct = default);
}

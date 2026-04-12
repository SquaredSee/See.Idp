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

    /// <summary>
    ///     Signs out the current user.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous sign-out operation.</returns>
    Task SignOutAsync(CancellationToken ct = default);
}

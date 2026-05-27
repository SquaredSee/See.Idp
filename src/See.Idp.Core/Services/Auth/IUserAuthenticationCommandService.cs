using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Core.Dtos.Common;
using See.Idp.Core.Dtos.Users;

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

    /// <summary>
    ///     Generates a password reset token for the user with the given email address.
    ///     Returns <see langword="null"/> if the user does not exist or their email is not confirmed.
    /// </summary>
    /// <param name="email">The email address of the user requesting a password reset.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    ///     A Base64Url-encoded password reset token, or <see langword="null"/> if the user was
    ///     not found or their email has not been confirmed.
    /// </returns>
    Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken ct = default);

    /// <summary>
    ///     Resets a user's password using a previously issued reset token.
    /// </summary>
    /// <param name="command">The command containing the user's email, reset code, and new password.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result of the password reset.</returns>
    Task<CommandResult> ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Changes a user's password and refreshes their sign-in cookie.
    /// </summary>
    /// <param name="command">The command containing the user ID, old password, and new password.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result of the password change.</returns>
    Task<CommandResult> ChangePasswordAsync(
        ChangePasswordCommand command,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Refreshes the current user's sign-in cookie to reflect any security stamp changes.
    ///     This method requires an active HTTP request context and silently no-ops when no
    ///     authenticated user is found (e.g. in background jobs or unit tests).
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RefreshSignInAsync(CancellationToken ct = default);
}

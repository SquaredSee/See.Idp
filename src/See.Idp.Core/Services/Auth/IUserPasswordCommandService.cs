using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos.Common;
using See.Idp.Core.Dtos.Users;

namespace See.Idp.Core.Services.Auth;

/// <summary>
///     Provides commands for managing user passwords.
/// </summary>
public interface IUserPasswordCommandService
{
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
    ///     Changes a user's password.
    /// </summary>
    /// <param name="command">The command containing the user ID, old password, and new password.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result of the password change.</returns>
    Task<CommandResult> ChangePasswordAsync(
        ChangePasswordCommand command,
        CancellationToken ct = default
    );
}

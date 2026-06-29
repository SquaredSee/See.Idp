using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Core.Dtos.Common;

namespace See.Idp.Core.Services.Auth;

/// <summary>Provides commands for managing user passwords.</summary>
public interface IPasswordCommandService
{
    /// <summary>
    ///     Generates a password reset token for the user with the given email address.
    ///     Returns a failed result if the user does not exist or their email is not confirmed.
    /// </summary>
    Task<GeneratePasswordResetTokenResult> GeneratePasswordResetTokenAsync(
        GeneratePasswordResetTokenCommand command,
        CancellationToken ct = default
    );

    /// <summary>Resets a user's password using a previously issued reset token.</summary>
    Task<CommandResult> ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken ct = default
    );

    /// <summary>Changes a user's password.</summary>
    Task<CommandResult> ChangePasswordAsync(
        ChangePasswordCommand command,
        CancellationToken ct = default
    );
}

using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Core.Dtos.Common;
using See.Idp.Core.Services.Auth;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class PasswordCommandService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<PasswordCommandService> logger
) : IPasswordCommandService
{
    public async Task<GeneratePasswordResetTokenResult> GeneratePasswordResetTokenAsync(
        GeneratePasswordResetTokenCommand command,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByEmailAsync(command.Email);
        if (user is null || !await userManager.IsEmailConfirmedAsync(user))
            return GeneratePasswordResetTokenResult.Failed();

        var code = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        LogPasswordResetTokenGenerated(command.Email);
        return GeneratePasswordResetTokenResult.Success(encodedCode);
    }

    private const string InvalidResetLinkError =
        "The password reset link is invalid or has expired. Please request a new one.";

    public async Task<CommandResult> ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByEmailAsync(command.Email);
        if (user is null)
        {
            LogPasswordResetFailed(command.Email);
            return CommandResult.Failure(InvalidResetLinkError);
        }

        var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(command.Code));
        var result = await userManager.ResetPasswordAsync(user, code, command.NewPassword);

        if (result.Succeeded)
        {
            LogPasswordResetSucceeded(command.Email);
            return CommandResult.Success();
        }

        LogPasswordResetFailed(command.Email);

        if (result.Errors.Any(e => e.Code == "InvalidToken"))
            return CommandResult.Failure(InvalidResetLinkError);

        return CommandResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<CommandResult> ChangePasswordAsync(
        ChangePasswordCommand command,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return CommandResult.Failure($"Unable to load user with ID '{command.UserId}'.");

        var result = await userManager.ChangePasswordAsync(
            user,
            command.OldPassword,
            command.NewPassword
        );

        if (!result.Succeeded)
        {
            LogPasswordChangeFailed(command.UserId);
            return CommandResult.Failure(
                string.Join(", ", result.Errors.Select(e => e.Description))
            );
        }

        await signInManager.RefreshSignInAsync(user);
        LogPasswordChanged(command.UserId);
        return CommandResult.Success();
    }

    [LoggerMessage(
        EventId = EventIds.AuthenticationPasswordResetTokenGenerated,
        Level = LogLevel.Information,
        Message = "Password reset token generated for {Email}"
    )]
    private partial void LogPasswordResetTokenGenerated(string email);

    [LoggerMessage(
        EventId = EventIds.AuthenticationPasswordResetSucceeded,
        Level = LogLevel.Information,
        Message = "Password reset succeeded for {Email}"
    )]
    private partial void LogPasswordResetSucceeded(string email);

    [LoggerMessage(
        EventId = EventIds.AuthenticationPasswordResetFailed,
        Level = LogLevel.Warning,
        Message = "Password reset failed for {Email}"
    )]
    private partial void LogPasswordResetFailed(string email);

    [LoggerMessage(
        EventId = EventIds.AuthenticationPasswordChanged,
        Level = LogLevel.Information,
        Message = "Password changed for user {UserId}"
    )]
    private partial void LogPasswordChanged(string userId);

    [LoggerMessage(
        EventId = EventIds.AuthenticationPasswordChangeFailed,
        Level = LogLevel.Warning,
        Message = "Password change failed for user {UserId}"
    )]
    private partial void LogPasswordChangeFailed(string userId);
}

using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Core.Dtos.Common;
using See.Idp.Core.Dtos.Users;
using See.Idp.Core.Services.Auth;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class UserAccountService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<UserAccountService> logger
) : IUserAuthenticationCommandService, IUserPasswordCommandService
{
    public async Task<PasswordSignInResult> PasswordSignInAsync(
        PasswordSignInCommand command,
        CancellationToken ct = default
    )
    {
        LogAuthenticationSignInAttempt(command.Email);

        var result = await signInManager.PasswordSignInAsync(
            command.Email,
            command.Password,
            command.RememberMe,
            lockoutOnFailure: true
        );

        if (result.Succeeded)
        {
            LogAuthenticationSignInSucceeded(command.Email);
            return PasswordSignInResult.Success();
        }

        if (result.IsLockedOut)
        {
            LogAuthenticationSignInLockedOut(command.Email);
            return PasswordSignInResult.LockedOut();
        }

        if (result.RequiresTwoFactor)
            return PasswordSignInResult.TwoFactorRequired();

        LogAuthenticationSignInFailed(command.Email);
        return PasswordSignInResult.Failure(
            "Login failed. Please check your credentials and try again."
        );
    }

    public async Task<TwoFactorSignInResult> TwoFactorSignInAsync(
        TwoFactorSignInCommand command,
        CancellationToken ct = default
    )
    {
        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(
            command.Code,
            command.RememberMe,
            command.RememberClient
        );

        if (result.Succeeded)
        {
            LogTwoFactorSignInSucceeded();
            return TwoFactorSignInResult.Success();
        }

        if (result.IsLockedOut)
        {
            LogTwoFactorSignInLockedOut();
            return TwoFactorSignInResult.LockedOut();
        }

        if (result.IsNotAllowed)
            return TwoFactorSignInResult.NotAllowed();

        LogTwoFactorSignInFailed();
        return TwoFactorSignInResult.Failure("Invalid authenticator code.");
    }

    public async Task<TwoFactorSignInResult> RecoveryCodeSignInAsync(
        RecoveryCodeSignInCommand command,
        CancellationToken ct = default
    )
    {
        var result = await signInManager.TwoFactorRecoveryCodeSignInAsync(command.Code);

        if (result.Succeeded)
        {
            LogRecoveryCodeSignInSucceeded();
            return TwoFactorSignInResult.Success();
        }

        if (result.IsLockedOut)
            return TwoFactorSignInResult.LockedOut();

        LogRecoveryCodeSignInFailed();
        return TwoFactorSignInResult.Failure("Invalid recovery code.");
    }

    public async Task SignOutAsync(CancellationToken ct = default)
    {
        await signInManager.SignOutAsync();
        LogAuthenticationSignOut();
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
            LogAuthenticationPasswordResetFailed(command.Email);
            return CommandResult.Failure(InvalidResetLinkError);
        }

        var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(command.Code));
        var result = await userManager.ResetPasswordAsync(user, code, command.NewPassword);

        if (result.Succeeded)
        {
            LogAuthenticationPasswordResetSucceeded(command.Email);
            return CommandResult.Success();
        }

        LogAuthenticationPasswordResetFailed(command.Email);

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
            LogAuthenticationPasswordChangeFailed(command.UserId);
            return CommandResult.Failure(
                string.Join(", ", result.Errors.Select(e => e.Description))
            );
        }

        await RefreshSignInCoreAsync(user);
        LogAuthenticationPasswordChanged(command.UserId);
        return CommandResult.Success();
    }

    public async Task RefreshSignInAsync(CancellationToken ct = default)
    {
        var ctx = signInManager.Context;
        if (ctx is null)
            return;

        var user = await userManager.GetUserAsync(ctx.User);
        if (user is not null)
            await RefreshSignInCoreAsync(user);
    }

    private async Task RefreshSignInCoreAsync(ApplicationUser user)
    {
        await signInManager.RefreshSignInAsync(user);
        LogAuthenticationSignInRefreshed();
    }

    [LoggerMessage(
        EventId = EventIds.AuthenticationSignInAttempt,
        Level = LogLevel.Information,
        Message = "Authentication sign-in attempt for {Email}"
    )]
    private partial void LogAuthenticationSignInAttempt(string email);

    [LoggerMessage(
        EventId = EventIds.AuthenticationSignInSucceeded,
        Level = LogLevel.Information,
        Message = "Authentication sign-in succeeded for {Email}"
    )]
    private partial void LogAuthenticationSignInSucceeded(string email);

    [LoggerMessage(
        EventId = EventIds.AuthenticationSignInFailed,
        Level = LogLevel.Warning,
        Message = "Authentication sign-in failed for {Email}"
    )]
    private partial void LogAuthenticationSignInFailed(string email);

    [LoggerMessage(
        EventId = EventIds.AuthenticationSignInLockedOut,
        Level = LogLevel.Warning,
        Message = "Authentication sign-in locked out for {Email}"
    )]
    private partial void LogAuthenticationSignInLockedOut(string email);

    [LoggerMessage(
        EventId = EventIds.AuthenticationSignOut,
        Level = LogLevel.Information,
        Message = "Authentication sign-out completed"
    )]
    private partial void LogAuthenticationSignOut();

    [LoggerMessage(
        EventId = EventIds.AuthenticationPasswordResetSucceeded,
        Level = LogLevel.Information,
        Message = "Password reset succeeded for {Email}"
    )]
    private partial void LogAuthenticationPasswordResetSucceeded(string email);

    [LoggerMessage(
        EventId = EventIds.AuthenticationPasswordResetFailed,
        Level = LogLevel.Warning,
        Message = "Password reset failed for {Email}"
    )]
    private partial void LogAuthenticationPasswordResetFailed(string email);

    [LoggerMessage(
        EventId = EventIds.AuthenticationPasswordChanged,
        Level = LogLevel.Information,
        Message = "Password changed for user {UserId}"
    )]
    private partial void LogAuthenticationPasswordChanged(string userId);

    [LoggerMessage(
        EventId = EventIds.AuthenticationPasswordChangeFailed,
        Level = LogLevel.Warning,
        Message = "Password change failed for user {UserId}"
    )]
    private partial void LogAuthenticationPasswordChangeFailed(string userId);

    [LoggerMessage(
        EventId = EventIds.TwoFactorSignInSucceeded,
        Level = LogLevel.Information,
        Message = "Two-factor sign-in succeeded"
    )]
    private partial void LogTwoFactorSignInSucceeded();

    [LoggerMessage(
        EventId = EventIds.TwoFactorSignInFailed,
        Level = LogLevel.Warning,
        Message = "Two-factor sign-in failed"
    )]
    private partial void LogTwoFactorSignInFailed();

    [LoggerMessage(
        EventId = EventIds.TwoFactorSignInLockedOut,
        Level = LogLevel.Warning,
        Message = "Two-factor sign-in locked out"
    )]
    private partial void LogTwoFactorSignInLockedOut();

    [LoggerMessage(
        EventId = EventIds.RecoveryCodeSignInSucceeded,
        Level = LogLevel.Information,
        Message = "Recovery code sign-in succeeded"
    )]
    private partial void LogRecoveryCodeSignInSucceeded();

    [LoggerMessage(
        EventId = EventIds.RecoveryCodeSignInFailed,
        Level = LogLevel.Warning,
        Message = "Recovery code sign-in failed"
    )]
    private partial void LogRecoveryCodeSignInFailed();

    [LoggerMessage(
        EventId = EventIds.AuthenticationSignInRefreshed,
        Level = LogLevel.Information,
        Message = "Sign-in refreshed for current user"
    )]
    private partial void LogAuthenticationSignInRefreshed();
}

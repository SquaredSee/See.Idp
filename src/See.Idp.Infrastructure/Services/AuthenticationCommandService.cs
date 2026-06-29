using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Core.Services.Auth;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class AuthenticationCommandService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<AuthenticationCommandService> logger
) : IAuthenticationCommandService
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

    public async Task<RecoveryCodeSignInResult> RecoveryCodeSignInAsync(
        RecoveryCodeSignInCommand command,
        CancellationToken ct = default
    )
    {
        var result = await signInManager.TwoFactorRecoveryCodeSignInAsync(command.Code);

        if (result.Succeeded)
        {
            LogRecoveryCodeSignInSucceeded();
            return RecoveryCodeSignInResult.Success();
        }

        if (result.IsLockedOut)
            return RecoveryCodeSignInResult.LockedOut();

        LogRecoveryCodeSignInFailed();
        return RecoveryCodeSignInResult.Failure("Invalid recovery code.");
    }

    public async Task SignOutAsync(CancellationToken ct = default)
    {
        await signInManager.SignOutAsync();
        LogAuthenticationSignOut();
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
}

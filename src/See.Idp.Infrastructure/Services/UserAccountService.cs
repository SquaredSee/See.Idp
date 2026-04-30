using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Core.Services.Auth;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

// TODO: implement cancellation token support in SignInManager
public sealed partial class UserAccountService(
    SignInManager<ApplicationUser> signInManager,
    ILogger<UserAccountService> logger
) : IUserAuthenticationCommandService
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

        LogAuthenticationSignInFailed(command.Email);
        return PasswordSignInResult.Failure(
            "Login failed. Please check your credentials and try again."
        );
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
}

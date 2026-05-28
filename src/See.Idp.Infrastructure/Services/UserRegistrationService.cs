using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Dtos.Common;
using See.Idp.Core.Dtos.Users;
using See.Idp.Core.Services.Users;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class UserRegistrationService(
    UserManager<ApplicationUser> userManager,
    ILogger<UserRegistrationService> logger
) : IUserRegistrationCommandService
{
    public async Task<RegisterUserResult> RegisterAsync(
        RegisterUserCommand command,
        CancellationToken ct = default
    )
    {
        var user = new ApplicationUser { UserName = command.Email, Email = command.Email };
        var result = await userManager.CreateAsync(user, command.Password);

        if (!result.Succeeded)
        {
            LogUserRegistrationFailed(command.Email);
            return RegisterUserResult.Failure(result.Errors.Select(e => e.Description));
        }

        var userId = await userManager.GetUserIdAsync(user);
        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        LogUserRegistered(command.Email);
        return RegisterUserResult.Success(userId, encodedCode);
    }

    public async Task<CommandResult> ConfirmEmailAsync(
        ConfirmEmailCommand command,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return CommandResult.Failure($"Unable to load user with ID '{command.UserId}'.");

        var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(command.EncodedToken));
        var result = await userManager.ConfirmEmailAsync(user, code);

        if (result.Succeeded)
        {
            LogUserEmailConfirmed(command.UserId);
            return CommandResult.Success();
        }

        LogUserEmailConfirmationFailed(command.UserId);
        return CommandResult.Failure(
            "Error confirming email. The link may have expired or already been used."
        );
    }

    [LoggerMessage(
        EventId = EventIds.UserRegistered,
        Level = LogLevel.Information,
        Message = "User registered successfully for {Email}"
    )]
    private partial void LogUserRegistered(string email);

    [LoggerMessage(
        EventId = EventIds.UserRegistrationFailed,
        Level = LogLevel.Warning,
        Message = "User registration failed for {Email}"
    )]
    private partial void LogUserRegistrationFailed(string email);

    [LoggerMessage(
        EventId = EventIds.UserEmailConfirmed,
        Level = LogLevel.Information,
        Message = "Email confirmed for user {UserId}"
    )]
    private partial void LogUserEmailConfirmed(string userId);

    [LoggerMessage(
        EventId = EventIds.UserEmailConfirmationFailed,
        Level = LogLevel.Warning,
        Message = "Email confirmation failed for user {UserId}"
    )]
    private partial void LogUserEmailConfirmationFailed(string userId);
}

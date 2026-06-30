using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Core.Dtos.Common;
using See.Idp.Core.Services.Auth;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class TwoFactorCommandService(
    UserManager<ApplicationUser> userManager,
    ILogger<TwoFactorCommandService> logger
) : ITwoFactorCommandService
{
    public async Task<CommandResult> ProvisionAuthenticatorKeyAsync(
        ProvisionAuthenticatorKeyCommand command,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return CommandResult.Failure($"User '{command.UserId}' not found.");

        var existing = await userManager.GetAuthenticatorKeyAsync(user);
        if (!string.IsNullOrEmpty(existing))
            return CommandResult.Success();

        var result = await userManager.ResetAuthenticatorKeyAsync(user);
        if (!result.Succeeded)
            return CommandResult.Failure(
                string.Join(", ", result.Errors.Select(e => e.Description))
            );

        LogAuthenticatorKeyProvisioned(command.UserId);
        return CommandResult.Success();
    }

    public async Task<EnableTwoFactorResult> EnableTwoFactorAsync(
        EnableTwoFactorCommand command,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return EnableTwoFactorResult.Failure($"User '{command.UserId}' not found.");

        var code = command.VerificationCode.Replace(" ", string.Empty).Replace("-", string.Empty);

        var isValid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            code
        );

        if (!isValid)
            return EnableTwoFactorResult.Failure("Verification code is invalid.");

        var enableResult = await userManager.SetTwoFactorEnabledAsync(user, true);
        if (!enableResult.Succeeded)
            return EnableTwoFactorResult.Failure(
                string.Join(", ", enableResult.Errors.Select(e => e.Description))
            );

        var codes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        LogTwoFactorEnabled(command.UserId);
        return EnableTwoFactorResult.Success(codes ?? Array.Empty<string>());
    }

    public async Task<CommandResult> DisableTwoFactorAsync(
        DisableTwoFactorCommand command,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return CommandResult.Failure($"User '{command.UserId}' not found.");

        var result = await userManager.SetTwoFactorEnabledAsync(user, false);
        if (!result.Succeeded)
            return CommandResult.Failure(
                string.Join(", ", result.Errors.Select(e => e.Description))
            );

        LogTwoFactorDisabled(command.UserId);
        return CommandResult.Success();
    }

    public async Task<CommandResult> ResetAuthenticatorKeyAsync(
        ResetAuthenticatorKeyCommand command,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return CommandResult.Failure($"User '{command.UserId}' not found.");

        var disableResult = await userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disableResult.Succeeded)
            return CommandResult.Failure(
                string.Join(", ", disableResult.Errors.Select(e => e.Description))
            );

        var resetResult = await userManager.ResetAuthenticatorKeyAsync(user);
        if (!resetResult.Succeeded)
            return CommandResult.Failure(
                string.Join(", ", resetResult.Errors.Select(e => e.Description))
            );

        LogAuthenticatorKeyReset(command.UserId);
        return CommandResult.Success();
    }

    public async Task<GenerateRecoveryCodesResult> GenerateRecoveryCodesAsync(
        GenerateRecoveryCodesCommand command,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return GenerateRecoveryCodesResult.Failure($"User '{command.UserId}' not found.");

        var codes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        LogRecoveryCodesGenerated(command.UserId);
        return GenerateRecoveryCodesResult.Success(codes ?? Array.Empty<string>());
    }

    [LoggerMessage(
        EventId = EventIds.AuthenticatorKeyProvisioned,
        Level = LogLevel.Information,
        Message = "Authenticator key provisioned for user {UserId}"
    )]
    private partial void LogAuthenticatorKeyProvisioned(string userId);

    [LoggerMessage(
        EventId = EventIds.TwoFactorEnabled,
        Level = LogLevel.Information,
        Message = "Two-factor authentication enabled for user {UserId}"
    )]
    private partial void LogTwoFactorEnabled(string userId);

    [LoggerMessage(
        EventId = EventIds.TwoFactorDisabled,
        Level = LogLevel.Information,
        Message = "Two-factor authentication disabled for user {UserId}"
    )]
    private partial void LogTwoFactorDisabled(string userId);

    [LoggerMessage(
        EventId = EventIds.AuthenticatorKeyReset,
        Level = LogLevel.Information,
        Message = "Authenticator key reset for user {UserId}"
    )]
    private partial void LogAuthenticatorKeyReset(string userId);

    [LoggerMessage(
        EventId = EventIds.RecoveryCodesGenerated,
        Level = LogLevel.Information,
        Message = "Recovery codes generated for user {UserId}"
    )]
    private partial void LogRecoveryCodesGenerated(string userId);
}

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

public sealed partial class TwoFactorService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<TwoFactorService> logger
) : ITwoFactorCommandService, ITwoFactorQueryService
{
    private const string Issuer = "See.Idp";

    public async Task<TwoFactorInfo?> GetTwoFactorInfoAsync(
        GetTwoFactorInfoQuery query,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(query.UserId);
        if (user is null)
            return null;

        return new TwoFactorInfo(
            IsTwoFactorEnabled: await userManager.GetTwoFactorEnabledAsync(user),
            HasAuthenticator: !string.IsNullOrEmpty(
                await userManager.GetAuthenticatorKeyAsync(user)
            ),
            RecoveryCodesLeft: await userManager.CountRecoveryCodesAsync(user),
            IsMachineRemembered: await signInManager.IsTwoFactorClientRememberedAsync(user)
        );
    }

    public async Task<AuthenticatorSetupInfo?> GetAuthenticatorSetupAsync(
        GetAuthenticatorSetupQuery query,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(query.UserId);
        if (user is null)
            return null;

        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        var email = await userManager.GetEmailAsync(user) ?? string.Empty;
        return new AuthenticatorSetupInfo(
            SharedKey: FormatKey(key!),
            AuthenticatorUri: GenerateQrCodeUri(email, key!)
        );
    }

    public async Task<GenerateRecoveryCodesResult> EnableTwoFactorAsync(
        EnableTwoFactorCommand command,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return GenerateRecoveryCodesResult.Failure($"User '{command.UserId}' not found.");

        var code = command.VerificationCode.Replace(" ", string.Empty).Replace("-", string.Empty);

        var isValid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            code
        );

        if (!isValid)
            return GenerateRecoveryCodesResult.Failure("Verification code is invalid.");

        await userManager.SetTwoFactorEnabledAsync(user, true);
        var codes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        LogTwoFactorEnabled(command.UserId);
        return GenerateRecoveryCodesResult.Success(codes ?? Array.Empty<string>());
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

        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);
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

    private static string FormatKey(string key)
    {
        var result = new StringBuilder();
        var pos = 0;
        while (pos + 4 < key.Length)
        {
            result.Append(key, pos, 4).Append(' ');
            pos += 4;
        }
        if (pos < key.Length)
            result.Append(key, pos, key.Length - pos);
        return result.ToString().ToUpperInvariant();
    }

    private static string GenerateQrCodeUri(string email, string key) =>
        $"otpauth://totp/{Uri.EscapeDataString(Issuer)}:{Uri.EscapeDataString(email)}"
        + $"?secret={Uri.EscapeDataString(key)}&issuer={Uri.EscapeDataString(Issuer)}&digits=6";

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

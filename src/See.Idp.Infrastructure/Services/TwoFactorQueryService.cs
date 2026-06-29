using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Core.Services.Auth;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class TwoFactorQueryService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<TwoFactorQueryService> logger
) : ITwoFactorQueryService
{
    private const string Issuer = "See.Idp";

    public async Task<TwoFactorInfo?> GetTwoFactorInfoAsync(
        GetTwoFactorInfoQuery query,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(query.UserId);
        if (user is null)
        {
            LogTwoFactorUserNotFound(query.UserId);
            return null;
        }

        var result = new TwoFactorInfo(
            IsTwoFactorEnabled: await userManager.GetTwoFactorEnabledAsync(user),
            HasAuthenticator: !string.IsNullOrEmpty(
                await userManager.GetAuthenticatorKeyAsync(user)
            ),
            RecoveryCodesLeft: await userManager.CountRecoveryCodesAsync(user),
            IsMachineRemembered: await signInManager.IsTwoFactorClientRememberedAsync(user)
        );
        LogTwoFactorInfoRetrieved(query.UserId);
        return result;
    }

    public async Task<AuthenticatorSetupInfo?> GetAuthenticatorSetupAsync(
        GetAuthenticatorSetupQuery query,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(query.UserId);
        if (user is null)
        {
            LogTwoFactorUserNotFound(query.UserId);
            return null;
        }

        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            LogTwoFactorKeyNotProvisioned(query.UserId);
            return null;
        }

        var email = await userManager.GetEmailAsync(user) ?? string.Empty;
        var setup = new AuthenticatorSetupInfo(
            SharedKey: FormatKey(key),
            AuthenticatorUri: GenerateQrCodeUri(email, key)
        );
        LogTwoFactorSetupRetrieved(query.UserId);
        return setup;
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
        EventId = EventIds.TwoFactorUserNotFound,
        Level = LogLevel.Warning,
        Message = "Two-factor query failed: user {UserId} not found"
    )]
    private partial void LogTwoFactorUserNotFound(string userId);

    [LoggerMessage(
        EventId = EventIds.TwoFactorInfoRetrieved,
        Level = LogLevel.Debug,
        Message = "Two-factor info retrieved for user {UserId}"
    )]
    private partial void LogTwoFactorInfoRetrieved(string userId);

    [LoggerMessage(
        EventId = EventIds.TwoFactorKeyNotProvisioned,
        Level = LogLevel.Debug,
        Message = "Authenticator setup requested but no key provisioned for user {UserId}"
    )]
    private partial void LogTwoFactorKeyNotProvisioned(string userId);

    [LoggerMessage(
        EventId = EventIds.TwoFactorSetupRetrieved,
        Level = LogLevel.Debug,
        Message = "Authenticator setup retrieved for user {UserId}"
    )]
    private partial void LogTwoFactorSetupRetrieved(string userId);
}

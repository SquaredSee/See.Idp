using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos.Auth;
using See.Idp.Core.Dtos.Common;

namespace See.Idp.Core.Services.Auth;

/// <summary>Provides commands for managing a user's two-factor authentication settings.</summary>
public interface ITwoFactorCommandService
{
    /// <summary>
    ///     Provisions a TOTP authenticator key for the user if one does not already exist.
    ///     Idempotent — no-op if a key has already been provisioned.
    /// </summary>
    Task<CommandResult> ProvisionAuthenticatorKeyAsync(
        ProvisionAuthenticatorKeyCommand command,
        CancellationToken ct = default
    );

    /// <summary>Verifies the TOTP code and enables 2FA, returning new recovery codes.</summary>
    Task<EnableTwoFactorResult> EnableTwoFactorAsync(
        EnableTwoFactorCommand command,
        CancellationToken ct = default
    );

    /// <summary>Disables 2FA for the user.</summary>
    Task<CommandResult> DisableTwoFactorAsync(
        DisableTwoFactorCommand command,
        CancellationToken ct = default
    );

    /// <summary>Resets the authenticator key and disables 2FA.</summary>
    Task<CommandResult> ResetAuthenticatorKeyAsync(
        ResetAuthenticatorKeyCommand command,
        CancellationToken ct = default
    );

    /// <summary>Generates a new set of recovery codes, invalidating the previous set.</summary>
    Task<GenerateRecoveryCodesResult> GenerateRecoveryCodesAsync(
        GenerateRecoveryCodesCommand command,
        CancellationToken ct = default
    );
}

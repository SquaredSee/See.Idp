using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos.Common;
using See.Idp.Core.Dtos.Users;

namespace See.Idp.Core.Services.Users;

/// <summary>Provides commands for registering users and confirming their email addresses.</summary>
public interface IRegistrationCommandService
{
    /// <summary>Registers a new user account.</summary>
    Task<RegisterUserResult> RegisterAsync(
        RegisterUserCommand command,
        CancellationToken ct = default
    );

    /// <summary>Confirms a user's email address using the provided token.</summary>
    Task<CommandResult> ConfirmEmailAsync(
        ConfirmEmailCommand command,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Generates an email confirmation token for the given user.
    ///     Returns a not-found result if no user with the given ID exists.
    /// </summary>
    Task<GenerateEmailConfirmationTokenResult> GenerateEmailConfirmationTokenAsync(
        GenerateEmailConfirmationTokenCommand command,
        CancellationToken ct = default
    );
}

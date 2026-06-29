using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos.Common;
using See.Idp.Core.Dtos.Users;

namespace See.Idp.Core.Services.Users;

/// <summary>Provides commands for managing user accounts.</summary>
public interface IUserCommandService
{
    /// <summary>Enables or disables administrator privileges for a user.</summary>
    Task<CommandResult> ToggleAdminAsync(
        ToggleUserAdminCommand command,
        CancellationToken ct = default
    );

    /// <summary>Locks or unlocks a user account.</summary>
    Task<CommandResult> ToggleLockAsync(
        ToggleUserLockCommand command,
        CancellationToken ct = default
    );

    /// <summary>Deletes a user account.</summary>
    Task<CommandResult> DeleteUserAsync(DeleteUserCommand command, CancellationToken ct = default);

    /// <summary>Updates the phone number for a user.</summary>
    Task<CommandResult> UpdatePhoneNumberAsync(
        UpdatePhoneNumberCommand command,
        CancellationToken ct = default
    );
}

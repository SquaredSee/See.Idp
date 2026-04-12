using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos.Common;
using See.Idp.Core.Dtos.Users;

namespace See.Idp.Core.Services.Users;

/// <summary>
///     Provides commands for creating and managing user accounts and roles.
/// </summary>
public interface IUserCommandService
{
    /// <summary>
    ///     Creates a role when it does not already exist.
    /// </summary>
    /// <param name="command">The command describing the role to create.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result indicating whether the role was created.</returns>
    Task<CreateIfMissingResult> CreateRoleIfMissingAsync(
        CreateRoleIfMissingCommand command,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Creates a user when an account with the same identity does not already exist.
    /// </summary>
    /// <param name="command">The command describing the user to create.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result indicating whether the user was created.</returns>
    Task<CreateUserIfMissingResult> CreateUserIfMissingAsync(
        CreateUserIfMissingCommand command,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Adds a user to a role when that membership does not already exist.
    /// </summary>
    /// <param name="command">The command describing the role assignment.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result indicating whether the membership was created.</returns>
    Task<CreateIfMissingResult> AddUserToRoleIfMissingAsync(
        AddUserToRoleIfMissingCommand command,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Enables or disables administrator privileges for a user.
    /// </summary>
    /// <param name="command">The command describing the target user and admin state.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result of the command.</returns>
    Task<CommandResult> ToggleAdminAsync(
        ToggleUserAdminCommand command,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Locks or unlocks a user account.
    /// </summary>
    /// <param name="command">The command describing the target user and lock state.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result of the command.</returns>
    Task<CommandResult> ToggleLockAsync(
        ToggleUserLockCommand command,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Deletes a user account.
    /// </summary>
    /// <param name="command">The command describing the user to delete.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result of the command.</returns>
    Task<CommandResult> DeleteUserAsync(DeleteUserCommand command, CancellationToken ct = default);
}

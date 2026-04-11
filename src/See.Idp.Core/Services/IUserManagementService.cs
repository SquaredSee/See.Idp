using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos;

namespace See.Idp.Core.Services;

public interface IUserManagementService
{
    Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(
        string? currentUserId,
        CancellationToken ct = default
    );

    Task<UserManagementActionResult> ToggleAdminAsync(
        string targetUserId,
        string? currentUserId,
        CancellationToken ct = default
    );

    Task<UserManagementActionResult> ToggleLockAsync(
        string targetUserId,
        string? currentUserId,
        CancellationToken ct = default
    );

    Task<UserManagementActionResult> DeleteUserAsync(
        string targetUserId,
        string? currentUserId,
        CancellationToken ct = default
    );
}

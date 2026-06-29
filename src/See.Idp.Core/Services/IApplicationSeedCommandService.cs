using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos.Clients;
using See.Idp.Core.Dtos.Common;
using See.Idp.Core.Dtos.Users;

namespace See.Idp.Core.Services;

/// <summary>Provides idempotent initialisation commands for seeding roles, users, and clients.</summary>
public interface IApplicationSeedCommandService
{
    /// <summary>Creates a role when it does not already exist.</summary>
    Task<CreateIfMissingResult> CreateRoleIfMissingAsync(
        CreateRoleIfMissingCommand command,
        CancellationToken ct = default
    );

    /// <summary>Creates a user when an account with the same email does not already exist.</summary>
    Task<CreateUserIfMissingResult> CreateUserIfMissingAsync(
        CreateUserIfMissingCommand command,
        CancellationToken ct = default
    );

    /// <summary>Adds a user to a role when that membership does not already exist.</summary>
    Task<CreateIfMissingResult> AddUserToRoleIfMissingAsync(
        AddUserToRoleIfMissingCommand command,
        CancellationToken ct = default
    );

    /// <summary>Creates a client when it does not already exist.</summary>
    Task<CreateIfMissingResult> CreateClientIfMissingAsync(
        CreateClientIfMissingCommand command,
        CancellationToken ct = default
    );
}

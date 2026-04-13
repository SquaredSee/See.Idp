using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos.Clients;
using See.Idp.Core.Dtos.Common;

namespace See.Idp.Core.Services.Clients;

/// <summary>
///     Provides methods for managing clients, such as creating, updating, and deleting clients.
/// </summary>
public interface IClientCommandService
{
    /// <summary>
    ///     Creates a new client.
    /// </summary>
    /// <param name="command">The command containing the client details.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result of the command, including an initial secret when generated.</returns>
    Task<CreateClientResult> CreateClientAsync(
        CreateClientCommand command,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Updates an existing client.
    /// </summary>
    /// <param name="command">The command containing the client updates.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result of the command.</returns>
    Task<CommandResult> UpdateClientAsync(
        UpdateClientCommand command,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Deletes a client.
    /// </summary>
    /// <param name="command">The command identifying the client to delete.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result of the command.</returns>
    Task<CommandResult> DeleteClientAsync(
        DeleteClientCommand command,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Rotates a client's secret and returns the generated value.
    /// </summary>
    /// <param name="command">The command identifying which client secret to rotate.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result containing the generated secret when successful.</returns>
    Task<RotateClientSecretResult> RotateClientSecretAsync(
        RotateClientSecretCommand command,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Creates a client when it does not already exist.
    /// </summary>
    /// <param name="command">The command containing the client details.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result indicating whether the client was created.</returns>
    Task<CreateIfMissingResult> CreateClientIfMissingAsync(
        CreateClientIfMissingCommand command,
        CancellationToken ct = default
    );
}

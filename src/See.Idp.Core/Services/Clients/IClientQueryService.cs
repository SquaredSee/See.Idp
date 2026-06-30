using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos.Clients;

namespace See.Idp.Core.Services.Clients;

/// <summary>
///     Provides read operations for client application data.
/// </summary>
public interface IClientQueryService
{
    /// <summary>
    ///     Lists clients that match the supplied query criteria.
    /// </summary>
    /// <param name="query">The query containing filter and paging options.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A read-only list of matching clients.</returns>
    Task<IReadOnlyList<ClientSummaryDto>> ListClientsAsync(
        ListClientsQuery query,
        CancellationToken ct = default
    );

    /// <summary>
    ///     Gets a single client by identifier.
    /// </summary>
    /// <param name="query">The query containing the client identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result indicating success, not-found, or failure.</returns>
    Task<GetClientResult> GetClientByIdAsync(
        GetClientByIdQuery query,
        CancellationToken ct = default
    );
}

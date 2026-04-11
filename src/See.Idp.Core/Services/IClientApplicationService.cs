using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos;

namespace See.Idp.Core.Services;

public interface IClientApplicationService
{
    Task<IReadOnlyList<ClientApplicationDto>> ListClientsAsync(CancellationToken ct = default);

    Task<ClientApplicationDto?> GetClientAsync(string clientId, CancellationToken ct = default);

    Task<ClientApplicationActionResult> CreateClientAsync(
        CreateClientRequest request,
        CancellationToken ct = default
    );

    Task<ClientApplicationActionResult> UpdateClientAsync(
        UpdateClientRequest request,
        CancellationToken ct = default
    );

    Task<ClientApplicationActionResult> DeleteClientAsync(
        string clientId,
        CancellationToken ct = default
    );
}

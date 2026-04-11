using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using See.Idp.Core.Dtos;

namespace See.Idp.Core.Services;

public interface IClientApplicationService
{
    Task<IReadOnlyList<ClientApplicationDto>> ListClientsAsync(CancellationToken ct = default);

    Task<OperationResult> CreateClientAsync(
        CreateClientRequest request,
        CancellationToken ct = default
    );

    Task<OperationResult> DeleteClientAsync(string clientId, CancellationToken ct = default);
}

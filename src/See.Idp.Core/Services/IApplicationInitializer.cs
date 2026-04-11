using System.Threading;
using System.Threading.Tasks;

namespace See.Idp.Core.Services;

public interface IApplicationInitializer
{
    Task InitializeAsync(CancellationToken ct = default);
}

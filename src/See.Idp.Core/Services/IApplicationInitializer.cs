using System.Threading;
using System.Threading.Tasks;

namespace See.Idp.Core.Services;

/// <summary>
///     Provides a method for initializing the application, such as seeding the database with initial data.
/// </summary>
public interface IApplicationInitializer
{
    /// <summary>
    ///     Initializes the application.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync(CancellationToken ct = default);
}

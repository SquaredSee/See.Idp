using System.Collections.Immutable;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace See.Idp.Core.Services.Auth;

/// <summary>Provides read-only queries for authentication state and token claims.</summary>
public interface IAuthenticationQueryService
{
    /// <summary>
    ///     Builds a <see cref="ClaimsIdentity"/> for the specified user and scopes, or
    ///     <see langword="null"/> if the user does not exist or is not permitted to sign in.
    /// </summary>
    Task<ClaimsIdentity?> BuildUserIdentityAsync(
        string userId,
        ImmutableArray<string> scopes,
        CancellationToken ct = default
    );
}

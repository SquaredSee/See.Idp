using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using See.Idp.Core.Services.Auth;
using See.Idp.Infrastructure.Logging;

namespace See.Idp.Infrastructure.Services;

public sealed partial class AuthenticationQueryService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<AuthenticationQueryService> logger
) : IAuthenticationQueryService
{
    public async Task<ClaimsIdentity?> BuildUserIdentityAsync(
        string userId,
        ImmutableArray<string> scopes,
        CancellationToken ct = default
    )
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            LogAuthenticationQueryUserNotFound(userId);
            return null;
        }

        if (!await signInManager.CanSignInAsync(user))
        {
            LogAuthenticationQuerySignInNotAllowed(userId);
            return null;
        }

        var identity = new ClaimsIdentity(
            authenticationType: "Bearer",
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role
        );

        identity
            .SetClaim(OpenIddictConstants.Claims.Subject, await userManager.GetUserIdAsync(user))
            .SetClaim(OpenIddictConstants.Claims.Email, await userManager.GetEmailAsync(user))
            .SetClaim(
                OpenIddictConstants.Claims.EmailVerified,
                await userManager.IsEmailConfirmedAsync(user)
            )
            .SetClaim(OpenIddictConstants.Claims.Name, await userManager.GetUserNameAsync(user))
            .SetClaims(
                OpenIddictConstants.Claims.Role,
                (await userManager.GetRolesAsync(user)).ToImmutableArray()
            );

        identity.SetScopes(scopes);
        identity.SetDestinations(GetDestinations);

        LogAuthenticationQueryIdentityBuilt(userId);
        return identity;
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Name:
            case OpenIddictConstants.Claims.PreferredUsername:
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (claim.Subject!.HasScope(OpenIddictConstants.Scopes.Profile))
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            case OpenIddictConstants.Claims.Email:
            case OpenIddictConstants.Claims.EmailVerified:
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (claim.Subject!.HasScope(OpenIddictConstants.Scopes.Email))
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            case OpenIddictConstants.Claims.Role:
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (claim.Subject!.HasScope(OpenIddictConstants.Scopes.Roles))
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            case "AspNet.Identity.SecurityStamp":
                yield break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }

    [LoggerMessage(
        EventId = EventIds.AuthenticationQueryIdentityBuilt,
        Level = LogLevel.Debug,
        Message = "Token claims identity built for user {UserId}"
    )]
    private partial void LogAuthenticationQueryIdentityBuilt(string userId);

    [LoggerMessage(
        EventId = EventIds.AuthenticationQueryUserNotFound,
        Level = LogLevel.Warning,
        Message = "Token claims build failed: user {UserId} not found"
    )]
    private partial void LogAuthenticationQueryUserNotFound(string userId);

    [LoggerMessage(
        EventId = EventIds.AuthenticationQuerySignInNotAllowed,
        Level = LogLevel.Warning,
        Message = "Token claims build failed: user {UserId} is not permitted to sign in"
    )]
    private partial void LogAuthenticationQuerySignInNotAllowed(string userId);
}

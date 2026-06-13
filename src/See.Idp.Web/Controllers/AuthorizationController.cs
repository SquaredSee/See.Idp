using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using See.Idp.Infrastructure;

namespace See.Idp.Web.Controllers;

public sealed class AuthorizationController(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictAuthorizationManager authorizationManager,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager
) : Controller
{
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request =
            HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException(
                "The OpenIddict server request cannot be retrieved."
            );

        // Redirect to login if the user is not authenticated.
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!result.Succeeded)
        {
            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri =
                        Request.PathBase
                        + Request.Path
                        + QueryString.Create(
                            Request.HasFormContentType
                                ? Request.Form.ToList()
                                : Request.Query.ToList()
                        ),
                }
            );
        }

        var user =
            await userManager.GetUserAsync(result.Principal)
            ?? throw new InvalidOperationException("The user details cannot be retrieved.");

        var application =
            await applicationManager.FindByClientIdAsync(request.ClientId!)
            ?? throw new InvalidOperationException(
                $"The client application '{request.ClientId}' cannot be found."
            );

        var applicationId = (await applicationManager.GetIdAsync(application))!;
        var userId = await userManager.GetUserIdAsync(user);

        // Look for an existing permanent authorization for this user/client/scopes combo.
        object? authorization = null;
        await foreach (
            var auth in authorizationManager.FindAsync(
                subject: userId,
                client: applicationId,
                status: OpenIddictConstants.Statuses.Valid,
                type: OpenIddictConstants.AuthorizationTypes.Permanent,
                scopes: request.GetScopes()
            )
        )
        {
            authorization = auth;
        }

        var identity = await BuildUserIdentityAsync(user, request.GetScopes());

        // Create an ad-hoc authorization if no permanent one exists.
        // Note: a consent page (issue 03) will replace this with an explicit approval step.
        authorization ??= await authorizationManager.CreateAsync(
            principal: new ClaimsPrincipal(identity),
            subject: userId,
            client: applicationId,
            type: OpenIddictConstants.AuthorizationTypes.AdHoc,
            scopes: identity.GetScopes()
        );

        identity.SetAuthorizationId(await authorizationManager.GetIdAsync(authorization));
        identity.SetDestinations(GetDestinations);

        return SignIn(
            new ClaimsPrincipal(identity),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
        );
    }

    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    [EnableRateLimiting("token")]
    public async Task<IActionResult> Exchange()
    {
        var request =
            HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException(
                "The OpenIddict server request cannot be retrieved."
            );

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
            );

            var userId = result.Principal!.GetClaim(OpenIddictConstants.Claims.Subject)!;
            var user = await userManager.FindByIdAsync(userId);

            if (user is null)
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(
                        new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                                OpenIddictConstants.Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                "The token is no longer valid.",
                        }
                    )
                );

            if (!await signInManager.CanSignInAsync(user))
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(
                        new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                                OpenIddictConstants.Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                "The user is no longer allowed to sign in.",
                        }
                    )
                );

            var identity = await BuildUserIdentityAsync(user, result.Principal!.GetScopes());
            identity.SetDestinations(GetDestinations);

            return SignIn(
                new ClaimsPrincipal(identity),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
            );
        }

        if (request.IsClientCredentialsGrantType())
        {
            var identity = new ClaimsIdentity(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
            );
            identity.SetClaim(OpenIddictConstants.Claims.Subject, request.ClientId!);
            identity.SetScopes(request.GetScopes());
            identity.SetDestinations(_ => [OpenIddictConstants.Destinations.AccessToken]);

            return SignIn(
                new ClaimsPrincipal(identity),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
            );
        }

        throw new InvalidOperationException("The specified grant type is not supported.");
    }

    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> UserInfo()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Challenge(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(
                    new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants
                            .Errors
                            .InvalidToken,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The access token is bound to an account that no longer exists.",
                    }
                )
            );

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [OpenIddictConstants.Claims.Subject] = await userManager.GetUserIdAsync(user),
        };

        if (User.HasScope(OpenIddictConstants.Scopes.Email))
        {
            claims[OpenIddictConstants.Claims.Email] =
                await userManager.GetEmailAsync(user) ?? string.Empty;
            claims[OpenIddictConstants.Claims.EmailVerified] =
                await userManager.IsEmailConfirmedAsync(user);
        }

        if (User.HasScope(OpenIddictConstants.Scopes.Profile))
        {
            claims[OpenIddictConstants.Claims.Name] =
                await userManager.GetUserNameAsync(user) ?? string.Empty;
            claims[OpenIddictConstants.Claims.PreferredUsername] =
                await userManager.GetUserNameAsync(user) ?? string.Empty;
        }

        if (User.HasScope(OpenIddictConstants.Scopes.Roles))
            claims[OpenIddictConstants.Claims.Role] = await userManager.GetRolesAsync(user);

        return Ok(claims);
    }

    [HttpGet("~/connect/logout")]
    [HttpPost("~/connect/logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();

        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties { RedirectUri = "/" }
        );
    }

    private async Task<ClaimsIdentity> BuildUserIdentityAsync(
        ApplicationUser user,
        ImmutableArray<string> scopes
    )
    {
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

            // Never include the ASP.NET Identity security stamp in tokens.
            case "AspNet.Identity.SecurityStamp":
                yield break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }
}

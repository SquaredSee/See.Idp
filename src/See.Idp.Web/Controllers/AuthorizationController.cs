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
using See.Idp.Core.Services.Auth;

namespace See.Idp.Web.Controllers;

public sealed class AuthorizationController(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IAuthenticationQueryService authenticationQueryService,
    IAuthenticationCommandService authenticationCommandService
) : Controller
{
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        if (HttpContext.GetOpenIddictServerRequest() is not { } request)
            return BadRequest();

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

        var userId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(
                    new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants
                            .Errors
                            .InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The user account no longer exists.",
                    }
                )
            );

        var application = await applicationManager.FindByClientIdAsync(request.ClientId!);
        if (application is null)
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(
                    new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants
                            .Errors
                            .InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The client application cannot be found.",
                    }
                )
            );

        var applicationId = (await applicationManager.GetIdAsync(application))!;

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

        var identity = await authenticationQueryService.BuildUserIdentityAsync(
            userId,
            request.GetScopes(),
            HttpContext.RequestAborted
        );
        if (identity is null)
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(
                    new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants
                            .Errors
                            .InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The user account no longer exists.",
                    }
                )
            );

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
        if (HttpContext.GetOpenIddictServerRequest() is not { } request)
            return BadRequest();

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
            );

            var userId = result.Principal!.GetClaim(OpenIddictConstants.Claims.Subject)!;

            var identity = await authenticationQueryService.BuildUserIdentityAsync(
                userId,
                result.Principal!.GetScopes(),
                HttpContext.RequestAborted
            );

            if (identity is null)
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

        return Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties(
                new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants
                        .Errors
                        .UnsupportedGrantType,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "The specified grant type is not supported.",
                }
            )
        );
    }

    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> UserInfo()
    {
        var userId = User.GetClaim(OpenIddictConstants.Claims.Subject);
        if (userId is null)
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

        var identity = await authenticationQueryService.BuildUserIdentityAsync(
            userId,
            User.GetScopes(),
            HttpContext.RequestAborted
        );

        if (identity is null)
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
            [OpenIddictConstants.Claims.Subject] = userId,
        };

        if (User.HasScope(OpenIddictConstants.Scopes.Email))
        {
            claims[OpenIddictConstants.Claims.Email] =
                identity.GetClaim(OpenIddictConstants.Claims.Email) ?? string.Empty;
            bool.TryParse(
                identity.FindFirst(OpenIddictConstants.Claims.EmailVerified)?.Value,
                out var emailVerified
            );
            claims[OpenIddictConstants.Claims.EmailVerified] = emailVerified;
        }

        if (User.HasScope(OpenIddictConstants.Scopes.Profile))
        {
            var name = identity.GetClaim(OpenIddictConstants.Claims.Name) ?? string.Empty;
            claims[OpenIddictConstants.Claims.Name] = name;
            claims[OpenIddictConstants.Claims.PreferredUsername] = name;
        }

        if (User.HasScope(OpenIddictConstants.Scopes.Roles))
            claims[OpenIddictConstants.Claims.Role] = identity
                .GetClaims(OpenIddictConstants.Claims.Role)
                .ToArray();

        return Ok(claims);
    }

    [HttpGet("~/connect/logout")]
    [HttpPost("~/connect/logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await authenticationCommandService.SignOutAsync(HttpContext.RequestAborted);

        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties { RedirectUri = "/Identity/Account/Login" }
        );
    }
}

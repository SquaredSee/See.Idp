# 35 — AuthorizationController throws InvalidOperationException for unexpected states

## Context

`AuthorizationController` uses `throw new InvalidOperationException(...)` for several
conditions that should produce structured HTTP error responses:

```csharp
// src/See.Idp.Web/Controllers/AuthorizationController.cs

var request =
    HttpContext.GetOpenIddictServerRequest()
    ?? throw new InvalidOperationException(
        "The OpenIddict server request cannot be retrieved."
    );

var user =
    await userManager.GetUserAsync(result.Principal)
    ?? throw new InvalidOperationException("The user details cannot be retrieved.");

var application =
    await applicationManager.FindByClientIdAsync(request.ClientId!)
    ?? throw new InvalidOperationException(
        $"The client application '{request.ClientId}' cannot be found."
    );
```

Because there is no `ProblemDetails` middleware and no `IExceptionHandler` configured
in `Program.cs`, these exceptions produce either the developer exception page (development)
or the plain `/Error` Razor Page response (production). In production the user sees an
opaque error page with no actionable information and the operator gets an unstructured
500 log entry.

For an OIDC authorization endpoint, these failures should return structured OAuth 2.0
error responses (`error` + `error_description` JSON) so that the client application can
respond appropriately rather than receiving a raw 500.

## Fix

Replace the null-propagating throws with explicit `Forbid` or `Challenge` results that
communicate through the OpenIddict error pipeline:

```csharp
// Null OpenIddict request — this should not reach user-visible flows;
// return 400 Bad Request via OpenIddict
if (HttpContext.GetOpenIddictServerRequest() is not { } request)
    return BadRequest();

// User not found (deleted between authentication and token issuance)
if (await userManager.GetUserAsync(result.Principal) is not { } user)
    return Forbid(
        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
        properties: new AuthenticationProperties(
            new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                    OpenIddictConstants.Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                    "The user account no longer exists.",
            }
        )
    );

// Client not found — should have been validated by OpenIddict before reaching this handler
if (await applicationManager.FindByClientIdAsync(request.ClientId!) is not { } application)
    return Forbid(
        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
        properties: new AuthenticationProperties(
            new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                    OpenIddictConstants.Errors.InvalidClient,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                    "The client application cannot be found.",
            }
        )
    );
```

Note: Issue #25 (AuthorizationController layer violation) restructures this controller
significantly. This fix should either be applied to the post-#25 implementation or
be folded into the #25 work item.

## Acceptance Criteria

- `AuthorizationController` contains no `throw new InvalidOperationException(...)` for
  null-check guard conditions
- A missing OpenIddict request returns a 400 or OpenIddict error response
- A missing user after authentication returns an OpenIddict `invalid_grant` error
- A missing client application returns an OpenIddict `invalid_client` error
- All error responses use the OpenIddict server authentication scheme so the OAuth 2.0
  error JSON is correctly serialised to the client
- `dotnet build` passes clean

## Dependencies

Issue #25 (AuthorizationController layer violation) — implement this fix as part of or
after the #25 restructure to avoid duplicate churn on the same file.

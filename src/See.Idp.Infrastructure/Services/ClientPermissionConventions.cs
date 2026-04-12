using System;
using System.Collections.Generic;
using System.Linq;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace See.Idp.Infrastructure.Services;

internal static class ClientPermissionConventions
{
    private static readonly HashSet<string> FlowControlledPermissions =
    [
        Permissions.Endpoints.Authorization,
        Permissions.Endpoints.Token,
        Permissions.GrantTypes.AuthorizationCode,
        Permissions.GrantTypes.ClientCredentials,
        Permissions.GrantTypes.RefreshToken,
        Permissions.ResponseTypes.Code,
    ];

    public static bool IsFlowControlledPermission(string permission) =>
        FlowControlledPermissions.Contains(permission);

    public static IReadOnlyList<string> BuildPermissions(
        bool allowAuthorizationCodeFlow,
        bool allowClientCredentialsFlow,
        bool allowRefreshTokenFlow,
        IReadOnlyList<string> additionalPermissions
    )
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal);

        if (allowAuthorizationCodeFlow)
        {
            permissions.Add(Permissions.Endpoints.Authorization);
            permissions.Add(Permissions.Endpoints.Token);
            permissions.Add(Permissions.GrantTypes.AuthorizationCode);
            permissions.Add(Permissions.ResponseTypes.Code);
        }

        if (allowClientCredentialsFlow)
        {
            permissions.Add(Permissions.Endpoints.Token);
            permissions.Add(Permissions.GrantTypes.ClientCredentials);
        }

        if (allowRefreshTokenFlow)
        {
            permissions.Add(Permissions.Endpoints.Token);
            permissions.Add(Permissions.GrantTypes.RefreshToken);
        }

        foreach (var permission in additionalPermissions)
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                continue;
            }

            var trimmedPermission = permission.Trim();
            if (FlowControlledPermissions.Contains(trimmedPermission))
            {
                continue;
            }

            permissions.Add(trimmedPermission);
        }

        return permissions.OrderBy(p => p, StringComparer.Ordinal).ToList();
    }

    public static bool HasAuthorizationCodeFlow(IReadOnlyList<string> permissions) =>
        permissions.Any(p =>
            string.Equals(p, Permissions.GrantTypes.AuthorizationCode, StringComparison.Ordinal)
        );

    public static bool HasClientCredentialsFlow(IReadOnlyList<string> permissions) =>
        permissions.Any(p =>
            string.Equals(p, Permissions.GrantTypes.ClientCredentials, StringComparison.Ordinal)
        );

    public static bool HasRefreshTokenFlow(IReadOnlyList<string> permissions) =>
        permissions.Any(p =>
            string.Equals(p, Permissions.GrantTypes.RefreshToken, StringComparison.Ordinal)
        );
}

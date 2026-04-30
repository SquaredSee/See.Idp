using Microsoft.AspNetCore.Authorization;
using See.Idp.Core.Auth;

namespace See.Idp.Web.Auth;

public static class Policies
{
    public const string AdminPortal = "AdminPortal";
}

public static class AuthorizationBuilderExtensions
{
    /// <summary>
    ///     Adds the admin portal policy to the authorization builder.
    ///     This policy requires the authenticated user to have the <see cref="Roles.Admin"/> role.
    /// </summary>
    public static AuthorizationBuilder AddAdminPortalPolicy(this AuthorizationBuilder builder)
    {
        return builder.AddPolicy(
            Policies.AdminPortal,
            policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(Roles.Admin);
            }
        );
    }
}

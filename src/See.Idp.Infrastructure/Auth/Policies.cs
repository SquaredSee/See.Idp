using Microsoft.AspNetCore.Authorization;

namespace See.Idp.Infrastructure.Auth;

public static class Policies
{
    public const string AdminPortal = "AdminPortal";
}

public static class Roles
{
    public const string Admin = "admin";
}

public static class AuthorizationBuilderExtensions
{
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

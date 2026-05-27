using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using See.Idp.Core.Models;
using See.Idp.Infrastructure;

namespace See.Idp.Tests.Support;

internal static class IdentityTestFactory
{
    public static UserManager<ApplicationUser> CreateUserManager()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        var options = Substitute.For<IOptions<IdentityOptions>>();
        options.Value.Returns(new IdentityOptions());

        var passwordHasher = Substitute.For<IPasswordHasher<ApplicationUser>>();
        var userValidators = Array.Empty<IUserValidator<ApplicationUser>>();
        var passwordValidators = Array.Empty<IPasswordValidator<ApplicationUser>>();
        var keyNormalizer = Substitute.For<ILookupNormalizer>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<UserManager<ApplicationUser>>>();

        return Substitute.For<UserManager<ApplicationUser>>(
            store,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            new IdentityErrorDescriber(),
            serviceProvider,
            logger
        );
    }

    public static RoleManager<ApplicationRole> CreateRoleManager()
    {
        var store = Substitute.For<IRoleStore<ApplicationRole>>();
        var roleValidators = Array.Empty<IRoleValidator<ApplicationRole>>();
        var keyNormalizer = Substitute.For<ILookupNormalizer>();
        var logger = Substitute.For<ILogger<RoleManager<ApplicationRole>>>();

        return Substitute.For<RoleManager<ApplicationRole>>(
            store,
            roleValidators,
            keyNormalizer,
            new IdentityErrorDescriber(),
            logger
        );
    }

    public static IdentityResult FailedResult(params string[] descriptions)
    {
        var errors = descriptions
            .Select(
                (description, index) =>
                    new IdentityError { Code = $"ERR{index + 1}", Description = description }
            )
            .ToArray();

        return IdentityResult.Failed(errors);
    }

    public static IList<ApplicationUser> Users(params ApplicationUser[] users)
    {
        return users.ToList();
    }
}

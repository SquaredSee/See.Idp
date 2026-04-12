using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace See.Idp.Tests.Support;

internal static class IdentityTestFactory
{
    public static UserManager<IdentityUser> CreateUserManager()
    {
        var store = Substitute.For<IUserStore<IdentityUser>>();
        var options = Substitute.For<IOptions<IdentityOptions>>();
        options.Value.Returns(new IdentityOptions());

        var passwordHasher = Substitute.For<IPasswordHasher<IdentityUser>>();
        var userValidators = Array.Empty<IUserValidator<IdentityUser>>();
        var passwordValidators = Array.Empty<IPasswordValidator<IdentityUser>>();
        var keyNormalizer = Substitute.For<ILookupNormalizer>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<UserManager<IdentityUser>>>();

        return Substitute.For<UserManager<IdentityUser>>(
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

    public static RoleManager<IdentityRole> CreateRoleManager()
    {
        var store = Substitute.For<IRoleStore<IdentityRole>>();
        var roleValidators = Array.Empty<IRoleValidator<IdentityRole>>();
        var keyNormalizer = Substitute.For<ILookupNormalizer>();
        var logger = Substitute.For<ILogger<RoleManager<IdentityRole>>>();

        return Substitute.For<RoleManager<IdentityRole>>(
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

    public static IList<IdentityUser> Users(params IdentityUser[] users)
    {
        return users.ToList();
    }
}

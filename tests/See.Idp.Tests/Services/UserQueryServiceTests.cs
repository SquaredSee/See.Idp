using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using See.Idp.Core.Dtos.Users;
using See.Idp.Infrastructure;
using See.Idp.Infrastructure.Auth;
using See.Idp.Infrastructure.Services;
using See.Idp.Tests.Support;

namespace See.Idp.Tests.Services;

[TestClass]
public sealed class UserQueryServiceTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task ListUsersAsync_MapsUsers_WithOrderingAdminAndLockState()
    {
        var alpha = new ApplicationUser
        {
            Id = "user-alpha",
            UserName = "alpha",
            Email = "alpha@example.com",
            EmailConfirmed = true,
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(10),
        };

        var beta = new ApplicationUser
        {
            Id = "user-beta",
            UserName = "beta",
            Email = "beta@example.com",
            EmailConfirmed = false,
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(-10),
        };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.Users.Returns(AsyncQueryableTestFactory.Create([beta, alpha]));
        userManager
            .GetUsersInRoleAsync(Roles.Admin)
            .Returns(Task.FromResult(IdentityTestFactory.Users(alpha)));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.ListUsersAsync(new ListUsersQuery(), Ct);

        Assert.HasCount(2, result);

        Assert.AreEqual("user-alpha", result[0].UserId);
        Assert.AreEqual("alpha", result[0].UserName);
        Assert.AreEqual("alpha@example.com", result[0].Email);
        Assert.IsTrue(result[0].EmailConfirmed);
        Assert.IsTrue(result[0].IsAdmin);
        Assert.IsTrue(result[0].IsLockedOut);

        Assert.AreEqual("user-beta", result[1].UserId);
        Assert.AreEqual("beta", result[1].UserName);
        Assert.AreEqual("beta@example.com", result[1].Email);
        Assert.IsFalse(result[1].EmailConfirmed);
        Assert.IsFalse(result[1].IsAdmin);
        Assert.IsFalse(result[1].IsLockedOut);

        await userManager.Received(1).GetUsersInRoleAsync(Roles.Admin);
        await userManager
            .DidNotReceive()
            .IsInRoleAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
    }

    [TestMethod]
    public async Task ListUsersAsync_SetsIsLockedOutFalse_WhenLockoutDisabled()
    {
        var user = new ApplicationUser
        {
            Id = "user-lock-disabled",
            UserName = "nolock",
            Email = "nolock@example.com",
            LockoutEnabled = false,
            LockoutEnd = DateTimeOffset.UtcNow.AddYears(1),
        };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.Users.Returns(AsyncQueryableTestFactory.Create([user]));
        userManager
            .GetUsersInRoleAsync(Roles.Admin)
            .Returns(Task.FromResult(IdentityTestFactory.Users()));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.ListUsersAsync(new ListUsersQuery(), Ct);

        Assert.HasCount(1, result);
        Assert.IsFalse(result[0].IsLockedOut);
    }

    [TestMethod]
    public async Task ListUsersAsync_FiltersBySearchTerm()
    {
        var alpha = new ApplicationUser
        {
            Id = "user-alpha",
            UserName = "alpha",
            Email = "alpha@example.com",
        };

        var beta = new ApplicationUser
        {
            Id = "user-beta",
            UserName = "beta",
            Email = "beta@example.com",
        };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.Users.Returns(AsyncQueryableTestFactory.Create([alpha, beta]));
        userManager
            .GetUsersInRoleAsync(Roles.Admin)
            .Returns(Task.FromResult(IdentityTestFactory.Users()));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.ListUsersAsync(new ListUsersQuery(SearchTerm: "alpha"), Ct);

        Assert.HasCount(1, result);
        Assert.AreEqual("user-alpha", result[0].UserId);
    }

    [TestMethod]
    public async Task ListUsersAsync_AppliesSkipAndTake()
    {
        var userA = new ApplicationUser
        {
            Id = "user-a",
            UserName = "a",
            Email = "a@example.com",
        };

        var userB = new ApplicationUser
        {
            Id = "user-b",
            UserName = "b",
            Email = "b@example.com",
        };

        var userC = new ApplicationUser
        {
            Id = "user-c",
            UserName = "c",
            Email = "c@example.com",
        };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.Users.Returns(AsyncQueryableTestFactory.Create([userC, userA, userB]));
        userManager
            .GetUsersInRoleAsync(Roles.Admin)
            .Returns(Task.FromResult(IdentityTestFactory.Users()));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.ListUsersAsync(new ListUsersQuery(Skip: 1, Take: 1), Ct);

        Assert.HasCount(1, result);
        Assert.AreEqual("user-b", result[0].UserId);
    }

    private static UserQueryService CreateSut(UserManager<ApplicationUser>? userManager = null)
    {
        var effectiveUserManager = userManager ?? IdentityTestFactory.CreateUserManager();
        var logger = Substitute.For<ILogger<UserQueryService>>();

        return new UserQueryService(effectiveUserManager, logger);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using OpenIddict.EntityFrameworkCore;
using See.Idp.Core.Dtos.Users;
using See.Idp.Infrastructure;
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
        await using var db = CreateDbContext();

        var adminRole = new ApplicationRole
        {
            Id = "role-admin",
            Name = "Admin",
            NormalizedName = "ADMIN",
        };
        var alpha = new ApplicationUser
        {
            Id = "user-alpha",
            UserName = "alpha",
            NormalizedUserName = "ALPHA",
            Email = "alpha@example.com",
            NormalizedEmail = "ALPHA@EXAMPLE.COM",
            EmailConfirmed = true,
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(10),
        };
        var beta = new ApplicationUser
        {
            Id = "user-beta",
            UserName = "beta",
            NormalizedUserName = "BETA",
            Email = "beta@example.com",
            NormalizedEmail = "BETA@EXAMPLE.COM",
            EmailConfirmed = false,
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(-10),
        };

        db.Roles.Add(adminRole);
        db.Users.AddRange(alpha, beta);
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = alpha.Id, RoleId = adminRole.Id });
        await db.SaveChangesAsync(Ct);

        var result = await CreateSut(db).ListUsersAsync(new ListUsersQuery(), Ct);

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
    }

    [TestMethod]
    public async Task ListUsersAsync_SetsIsLockedOutFalse_WhenLockoutDisabled()
    {
        await using var db = CreateDbContext();

        db.Users.Add(
            new ApplicationUser
            {
                Id = "user-lock-disabled",
                UserName = "nolock",
                NormalizedUserName = "NOLOCK",
                Email = "nolock@example.com",
                NormalizedEmail = "NOLOCK@EXAMPLE.COM",
                LockoutEnabled = false,
                LockoutEnd = DateTimeOffset.UtcNow.AddYears(1),
            }
        );
        await db.SaveChangesAsync(Ct);

        var result = await CreateSut(db).ListUsersAsync(new ListUsersQuery(), Ct);

        Assert.HasCount(1, result);
        Assert.IsFalse(result[0].IsLockedOut);
    }

    [TestMethod]
    public async Task ListUsersAsync_FiltersBySearchTerm()
    {
        await using var db = CreateDbContext();

        db.Users.AddRange(
            new ApplicationUser
            {
                Id = "user-alpha",
                UserName = "alpha",
                NormalizedUserName = "ALPHA",
                Email = "alpha@example.com",
                NormalizedEmail = "ALPHA@EXAMPLE.COM",
            },
            new ApplicationUser
            {
                Id = "user-beta",
                UserName = "beta",
                NormalizedUserName = "BETA",
                Email = "beta@example.com",
                NormalizedEmail = "BETA@EXAMPLE.COM",
            }
        );
        await db.SaveChangesAsync(Ct);

        var result = await CreateSut(db)
            .ListUsersAsync(new ListUsersQuery(SearchTerm: "alpha"), Ct);

        Assert.HasCount(1, result);
        Assert.AreEqual("user-alpha", result[0].UserId);
    }

    [TestMethod]
    public async Task ListUsersAsync_AppliesSkipAndTake()
    {
        await using var db = CreateDbContext();

        db.Users.AddRange(
            new ApplicationUser
            {
                Id = "user-a",
                UserName = "a",
                NormalizedUserName = "A",
                Email = "a@example.com",
                NormalizedEmail = "A@EXAMPLE.COM",
            },
            new ApplicationUser
            {
                Id = "user-b",
                UserName = "b",
                NormalizedUserName = "B",
                Email = "b@example.com",
                NormalizedEmail = "B@EXAMPLE.COM",
            },
            new ApplicationUser
            {
                Id = "user-c",
                UserName = "c",
                NormalizedUserName = "C",
                Email = "c@example.com",
                NormalizedEmail = "C@EXAMPLE.COM",
            }
        );
        await db.SaveChangesAsync(Ct);

        var result = await CreateSut(db).ListUsersAsync(new ListUsersQuery(Skip: 1, Take: 1), Ct);

        Assert.HasCount(1, result);
        Assert.AreEqual("user-b", result[0].UserId);
    }

    [TestMethod]
    public async Task GetUserProfileAsync_ReturnsProfile_WhenUserExists()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            Email = "user@example.com",
            PhoneNumber = "555-1234",
        };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("user-1").Returns(Task.FromResult<ApplicationUser?>(user));

        var result = await CreateSut(userManager: userManager)
            .GetUserProfileAsync(new GetUserProfileQuery("user-1"), Ct);

        Assert.IsNotNull(result);
        Assert.AreEqual("user@example.com", result.Email);
        Assert.AreEqual("555-1234", result.PhoneNumber);
    }

    [TestMethod]
    public async Task GetUserProfileAsync_ReturnsNull_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("missing").Returns(Task.FromResult<ApplicationUser?>(null));

        var result = await CreateSut(userManager: userManager)
            .GetUserProfileAsync(new GetUserProfileQuery("missing"), Ct);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task FindUserIdByEmailAsync_ReturnsUserId_WhenUserExists()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "user@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager
            .FindByEmailAsync("user@example.com")
            .Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.GetUserIdAsync(user).Returns(Task.FromResult("user-1"));

        var result = await CreateSut(userManager: userManager)
            .FindUserIdByEmailAsync(new FindUserByEmailQuery("user@example.com"), Ct);

        Assert.AreEqual("user-1", result.UserId);
    }

    [TestMethod]
    public async Task FindUserIdByEmailAsync_ReturnsNullUserId_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager
            .FindByEmailAsync("missing@example.com")
            .Returns(Task.FromResult<ApplicationUser?>(null));

        var result = await CreateSut(userManager: userManager)
            .FindUserIdByEmailAsync(new FindUserByEmailQuery("missing@example.com"), Ct);

        Assert.IsNull(result.UserId);
    }

    private static ApplicationDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .UseOpenIddict()
                .Options
        );

    private static UserQueryService CreateSut(
        ApplicationDbContext? dbContext = null,
        UserManager<ApplicationUser>? userManager = null
    ) =>
        new(
            dbContext ?? CreateDbContext(),
            userManager ?? IdentityTestFactory.CreateUserManager(),
            Substitute.For<ILogger<UserQueryService>>()
        );
}

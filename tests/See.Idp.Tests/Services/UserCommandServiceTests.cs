using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using See.Idp.Core.Auth;
using See.Idp.Core.Dtos.Users;
using See.Idp.Infrastructure;
using See.Idp.Infrastructure.Services;
using See.Idp.Tests.Support;

namespace See.Idp.Tests.Services;

[TestClass]
public sealed class UserCommandServiceTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task ToggleAdminAsync_ReturnsFailure_WhenTargetUserIdMissing()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        var sut = CreateSut(userManager: userManager);

        var result = await sut.ToggleAdminAsync(new ToggleUserAdminCommand("", "current-user"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("User id is required.", result.Error);
        await userManager.DidNotReceive().FindByIdAsync(Arg.Any<string>());
    }

    [TestMethod]
    public async Task ToggleAdminAsync_ReturnsFailure_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("missing").Returns(Task.FromResult<ApplicationUser?>(null));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.ToggleAdminAsync(
            new ToggleUserAdminCommand("missing", "current-user"),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("User not found.", result.Error);
    }

    [TestMethod]
    public async Task ToggleAdminAsync_ReturnsFailure_WhenRemovingOwnAdminRole()
    {
        var user = new ApplicationUser { Id = "admin-1", Email = "admin@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.IsInRoleAsync(user, Roles.Admin).Returns(Task.FromResult(true));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.ToggleAdminAsync(new ToggleUserAdminCommand(user.Id, user.Id), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("You cannot remove your own admin role.", result.Error);
        await userManager.DidNotReceive().RemoveFromRoleAsync(user, Roles.Admin);
    }

    [TestMethod]
    public async Task ToggleAdminAsync_RemovesAdminRole_WhenUserIsAdmin()
    {
        var user = new ApplicationUser { Id = "admin-2", Email = "admin2@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.IsInRoleAsync(user, Roles.Admin).Returns(Task.FromResult(true));
        userManager
            .RemoveFromRoleAsync(user, Roles.Admin)
            .Returns(Task.FromResult(IdentityResult.Success));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.ToggleAdminAsync(
            new ToggleUserAdminCommand(user.Id, "other-user"),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("Removed admin role from admin2@example.com.", result.Message);
        await userManager.Received(1).RemoveFromRoleAsync(user, Roles.Admin);
    }

    [TestMethod]
    public async Task ToggleAdminAsync_ReturnsFailure_WhenGrantAdminFails()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "user@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.IsInRoleAsync(user, Roles.Admin).Returns(Task.FromResult(false));
        userManager
            .AddToRoleAsync(user, Roles.Admin)
            .Returns(Task.FromResult(IdentityTestFactory.FailedResult("Unable to add role.")));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.ToggleAdminAsync(
            new ToggleUserAdminCommand(user.Id, "current-user"),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Unable to add role.", result.Error);
    }

    [TestMethod]
    public async Task ToggleLockAsync_ReturnsFailure_WhenCurrentUserTargetsSelf()
    {
        var user = new ApplicationUser { Id = "self-user", Email = "self@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<ApplicationUser?>(user));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.ToggleLockAsync(new ToggleUserLockCommand(user.Id, user.Id), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("You cannot lock your own account.", result.Error);
        await userManager
            .DidNotReceive()
            .SetLockoutEndDateAsync(Arg.Any<ApplicationUser>(), Arg.Any<DateTimeOffset?>());
    }

    [TestMethod]
    public async Task ToggleLockAsync_EnablesAndLocksUser_WhenLockoutDisabled()
    {
        var user = new ApplicationUser
        {
            Id = "lock-user",
            Email = "lock@example.com",
            LockoutEnabled = false,
        };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.UpdateAsync(user).Returns(Task.FromResult(IdentityResult.Success));
        userManager
            .SetLockoutEndDateAsync(user, Arg.Any<DateTimeOffset?>())
            .Returns(Task.FromResult(IdentityResult.Success));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.ToggleLockAsync(
            new ToggleUserLockCommand(user.Id, "admin-user"),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("Locked lock@example.com.", result.Message);
        Assert.IsTrue(user.LockoutEnabled);
        await userManager.Received(1).UpdateAsync(user);
        await userManager
            .Received(1)
            .SetLockoutEndDateAsync(user, Arg.Is<DateTimeOffset?>(d => d.HasValue));
    }

    [TestMethod]
    public async Task ToggleLockAsync_ReturnsFailure_WhenEnablingLockoutFails()
    {
        var user = new ApplicationUser
        {
            Id = "lock-user-fail",
            Email = "lockfail@example.com",
            LockoutEnabled = false,
        };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .UpdateAsync(user)
            .Returns(Task.FromResult(IdentityTestFactory.FailedResult("Update failed.")));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.ToggleLockAsync(
            new ToggleUserLockCommand(user.Id, "admin-user"),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Update failed.", result.Error);
        await userManager
            .DidNotReceive()
            .SetLockoutEndDateAsync(Arg.Any<ApplicationUser>(), Arg.Any<DateTimeOffset?>());
    }

    [TestMethod]
    public async Task ToggleLockAsync_UnlocksUser_WhenAlreadyLocked()
    {
        var user = new ApplicationUser
        {
            Id = "unlock-user",
            Email = "unlock@example.com",
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddDays(1),
        };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .SetLockoutEndDateAsync(user, null)
            .Returns(Task.FromResult(IdentityResult.Success));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.ToggleLockAsync(
            new ToggleUserLockCommand(user.Id, "admin-user"),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("Unlocked unlock@example.com.", result.Message);
        await userManager.Received(1).SetLockoutEndDateAsync(user, null);
    }

    [TestMethod]
    public async Task DeleteUserAsync_ReturnsFailure_WhenCurrentUserTargetsSelf()
    {
        var user = new ApplicationUser { Id = "delete-self", Email = "selfdelete@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<ApplicationUser?>(user));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.DeleteUserAsync(new DeleteUserCommand(user.Id, user.Id), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("You cannot delete your own account.", result.Error);
        await userManager.DidNotReceive().DeleteAsync(Arg.Any<ApplicationUser>());
    }

    [TestMethod]
    public async Task DeleteUserAsync_ReturnsFailure_WhenDeletingLastAdmin()
    {
        var user = new ApplicationUser { Id = "admin-last", Email = "adminlast@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.IsInRoleAsync(user, Roles.Admin).Returns(Task.FromResult(true));
        userManager
            .GetUsersInRoleAsync(Roles.Admin)
            .Returns(Task.FromResult(IdentityTestFactory.Users(user)));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.DeleteUserAsync(new DeleteUserCommand(user.Id, "admin-other"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Cannot delete the last admin user.", result.Error);
        await userManager.DidNotReceive().DeleteAsync(Arg.Any<ApplicationUser>());
    }

    [TestMethod]
    public async Task DeleteUserAsync_DeletesUser_WhenAllowed()
    {
        var user = new ApplicationUser { Id = "delete-ok", Email = "deleteok@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.IsInRoleAsync(user, Roles.Admin).Returns(Task.FromResult(false));
        userManager.DeleteAsync(user).Returns(Task.FromResult(IdentityResult.Success));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.DeleteUserAsync(new DeleteUserCommand(user.Id, "admin-other"), Ct);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("Deleted deleteok@example.com.", result.Message);
        await userManager.Received(1).DeleteAsync(user);
    }

    [TestMethod]
    public async Task ToggleAdminAsync_GrantsAdminRole_WhenUserIsNotAdmin()
    {
        var user = new ApplicationUser { Id = "user-2", Email = "user2@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.IsInRoleAsync(user, Roles.Admin).Returns(Task.FromResult(false));
        userManager
            .AddToRoleAsync(user, Roles.Admin)
            .Returns(Task.FromResult(IdentityResult.Success));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.ToggleAdminAsync(
            new ToggleUserAdminCommand(user.Id, "current-user"),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("Granted admin role to user2@example.com.", result.Message);
        await userManager.Received(1).AddToRoleAsync(user, Roles.Admin);
    }

    [TestMethod]
    public async Task UpdatePhoneNumberAsync_ReturnsFailure_WhenUserIdIsEmpty()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("").Returns(Task.FromResult<ApplicationUser?>(null));
        var sut = CreateSut(userManager: userManager);

        var result = await sut.UpdatePhoneNumberAsync(new UpdatePhoneNumberCommand("", null), Ct);

        Assert.IsFalse(result.Succeeded);
    }

    [TestMethod]
    public async Task UpdatePhoneNumberAsync_ReturnsFailure_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("missing").Returns(Task.FromResult<ApplicationUser?>(null));
        var sut = CreateSut(userManager: userManager);

        var result = await sut.UpdatePhoneNumberAsync(
            new UpdatePhoneNumberCommand("missing", null),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task UpdatePhoneNumberAsync_UpdatesPhoneNumber_WhenUserExists()
    {
        var user = new ApplicationUser { Id = "u1", Email = "u@example.com" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .SetPhoneNumberAsync(user, "+15550001234")
            .Returns(Task.FromResult(IdentityResult.Success));
        var sut = CreateSut(userManager: userManager);

        var result = await sut.UpdatePhoneNumberAsync(
            new UpdatePhoneNumberCommand(user.Id, "+15550001234"),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        await userManager.Received(1).SetPhoneNumberAsync(user, "+15550001234");
    }

    [TestMethod]
    public async Task UpdatePhoneNumberAsync_ReturnsFailure_WhenSetPhoneNumberFails()
    {
        var user = new ApplicationUser { Id = "u1", Email = "u@example.com" };
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .SetPhoneNumberAsync(user, Arg.Any<string?>())
            .Returns(Task.FromResult(IdentityTestFactory.FailedResult("Invalid phone number.")));
        var sut = CreateSut(userManager: userManager);

        var result = await sut.UpdatePhoneNumberAsync(
            new UpdatePhoneNumberCommand(user.Id, "not-a-phone"),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        Assert.IsNotNull(result.Error);
    }

    private static UserCommandService CreateSut(UserManager<ApplicationUser>? userManager = null)
    {
        var effectiveUserManager = userManager ?? IdentityTestFactory.CreateUserManager();
        var logger = Substitute.For<ILogger<UserCommandService>>();

        return new UserCommandService(effectiveUserManager, logger);
    }
}

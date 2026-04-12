using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using See.Idp.Core.Dtos.Users;
using See.Idp.Infrastructure.Auth;
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
        userManager.FindByIdAsync("missing").Returns(Task.FromResult<IdentityUser?>(null));

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
        var user = new IdentityUser { Id = "admin-1", Email = "admin@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<IdentityUser?>(user));
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
        var user = new IdentityUser { Id = "admin-2", Email = "admin2@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<IdentityUser?>(user));
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
        var user = new IdentityUser { Id = "user-1", Email = "user@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<IdentityUser?>(user));
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
        var user = new IdentityUser { Id = "self-user", Email = "self@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<IdentityUser?>(user));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.ToggleLockAsync(new ToggleUserLockCommand(user.Id, user.Id), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("You cannot lock your own account.", result.Error);
        await userManager
            .DidNotReceive()
            .SetLockoutEndDateAsync(Arg.Any<IdentityUser>(), Arg.Any<DateTimeOffset?>());
    }

    [TestMethod]
    public async Task ToggleLockAsync_EnablesAndLocksUser_WhenLockoutDisabled()
    {
        var user = new IdentityUser
        {
            Id = "lock-user",
            Email = "lock@example.com",
            LockoutEnabled = false,
        };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<IdentityUser?>(user));
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
        var user = new IdentityUser
        {
            Id = "lock-user-fail",
            Email = "lockfail@example.com",
            LockoutEnabled = false,
        };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<IdentityUser?>(user));
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
            .SetLockoutEndDateAsync(Arg.Any<IdentityUser>(), Arg.Any<DateTimeOffset?>());
    }

    [TestMethod]
    public async Task ToggleLockAsync_UnlocksUser_WhenAlreadyLocked()
    {
        var user = new IdentityUser
        {
            Id = "unlock-user",
            Email = "unlock@example.com",
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddDays(1),
        };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<IdentityUser?>(user));
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
        var user = new IdentityUser { Id = "delete-self", Email = "selfdelete@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<IdentityUser?>(user));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.DeleteUserAsync(new DeleteUserCommand(user.Id, user.Id), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("You cannot delete your own account.", result.Error);
        await userManager.DidNotReceive().DeleteAsync(Arg.Any<IdentityUser>());
    }

    [TestMethod]
    public async Task DeleteUserAsync_ReturnsFailure_WhenDeletingLastAdmin()
    {
        var user = new IdentityUser { Id = "admin-last", Email = "adminlast@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<IdentityUser?>(user));
        userManager.IsInRoleAsync(user, Roles.Admin).Returns(Task.FromResult(true));
        userManager
            .GetUsersInRoleAsync(Roles.Admin)
            .Returns(Task.FromResult(IdentityTestFactory.Users(user)));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.DeleteUserAsync(new DeleteUserCommand(user.Id, "admin-other"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Cannot delete the last admin user.", result.Error);
        await userManager.DidNotReceive().DeleteAsync(Arg.Any<IdentityUser>());
    }

    [TestMethod]
    public async Task DeleteUserAsync_DeletesUser_WhenAllowed()
    {
        var user = new IdentityUser { Id = "delete-ok", Email = "deleteok@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<IdentityUser?>(user));
        userManager.IsInRoleAsync(user, Roles.Admin).Returns(Task.FromResult(false));
        userManager.DeleteAsync(user).Returns(Task.FromResult(IdentityResult.Success));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.DeleteUserAsync(new DeleteUserCommand(user.Id, "admin-other"), Ct);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("Deleted deleteok@example.com.", result.Message);
        await userManager.Received(1).DeleteAsync(user);
    }

    [TestMethod]
    public async Task CreateRoleIfMissingAsync_ReturnsFailure_WhenRoleNameMissing()
    {
        var roleManager = IdentityTestFactory.CreateRoleManager();
        var sut = CreateSut(roleManager: roleManager);

        var result = await sut.CreateRoleIfMissingAsync(new CreateRoleIfMissingCommand(""), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Role is required.", result.Error);
        await roleManager.DidNotReceive().CreateAsync(Arg.Any<IdentityRole>());
    }

    [TestMethod]
    public async Task CreateRoleIfMissingAsync_ReturnsAlreadyExists_WhenRoleAlreadyExists()
    {
        var roleManager = IdentityTestFactory.CreateRoleManager();
        roleManager.RoleExistsAsync(Roles.Admin).Returns(Task.FromResult(true));

        var sut = CreateSut(roleManager: roleManager);

        var result = await sut.CreateRoleIfMissingAsync(
            new CreateRoleIfMissingCommand(Roles.Admin),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(result.Created);
    }

    [TestMethod]
    public async Task CreateRoleIfMissingAsync_CreatesRole_WhenMissing()
    {
        var roleName = "auditor";

        var roleManager = IdentityTestFactory.CreateRoleManager();
        roleManager.RoleExistsAsync(roleName).Returns(Task.FromResult(false));
        roleManager
            .CreateAsync(Arg.Any<IdentityRole>())
            .Returns(Task.FromResult(IdentityResult.Success));

        var sut = CreateSut(roleManager: roleManager);

        var result = await sut.CreateRoleIfMissingAsync(
            new CreateRoleIfMissingCommand(roleName),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Created);
        await roleManager.Received(1).CreateAsync(Arg.Is<IdentityRole>(r => r.Name == roleName));
    }

    [TestMethod]
    public async Task CreateRoleIfMissingAsync_ReturnsFailure_WhenCreateFails()
    {
        var roleName = "auditor";

        var roleManager = IdentityTestFactory.CreateRoleManager();
        roleManager.RoleExistsAsync(roleName).Returns(Task.FromResult(false));
        roleManager
            .CreateAsync(Arg.Any<IdentityRole>())
            .Returns(Task.FromResult(IdentityTestFactory.FailedResult("Role create failed.")));

        var sut = CreateSut(roleManager: roleManager);

        var result = await sut.CreateRoleIfMissingAsync(
            new CreateRoleIfMissingCommand(roleName),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Role create failed.", result.Error);
    }

    [TestMethod]
    public async Task CreateUserIfMissingAsync_ReturnsFailure_WhenEmailMissing()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        var sut = CreateSut(userManager: userManager);

        var result = await sut.CreateUserIfMissingAsync(
            new CreateUserIfMissingCommand("", "pass"),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Email is required.", result.Error);
    }

    [TestMethod]
    public async Task CreateUserIfMissingAsync_ReturnsAlreadyExists_WhenUserExists()
    {
        var existingUser = new IdentityUser
        {
            Id = "existing-user",
            Email = "existing@example.com",
        };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager
            .FindByEmailAsync(existingUser.Email)
            .Returns(Task.FromResult<IdentityUser?>(existingUser));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.CreateUserIfMissingAsync(
            new CreateUserIfMissingCommand(existingUser.Email, "pass"),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(result.Created);
        Assert.AreEqual(existingUser.Id, result.UserId);
        await userManager.DidNotReceive().CreateAsync(Arg.Any<IdentityUser>(), Arg.Any<string>());
    }

    [TestMethod]
    public async Task CreateUserIfMissingAsync_CreatesUserWithoutPassword_WhenPasswordBlank()
    {
        const string email = "new@example.com";

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByEmailAsync(email).Returns(Task.FromResult<IdentityUser?>(null));
        userManager
            .CreateAsync(Arg.Any<IdentityUser>())
            .Returns(Task.FromResult(IdentityResult.Success));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.CreateUserIfMissingAsync(
            new CreateUserIfMissingCommand(email, " "),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Created);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.UserId));
        await userManager
            .Received(1)
            .CreateAsync(Arg.Is<IdentityUser>(u => u.Email == email && u.UserName == email));
        await userManager.DidNotReceive().CreateAsync(Arg.Any<IdentityUser>(), Arg.Any<string>());
    }

    [TestMethod]
    public async Task CreateUserIfMissingAsync_CreatesUserWithPassword_WhenPasswordProvided()
    {
        const string email = "new-with-password@example.com";
        const string password = "Passw0rd!";

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByEmailAsync(email).Returns(Task.FromResult<IdentityUser?>(null));
        userManager
            .CreateAsync(Arg.Any<IdentityUser>(), password)
            .Returns(Task.FromResult(IdentityResult.Success));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.CreateUserIfMissingAsync(
            new CreateUserIfMissingCommand(email, password),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Created);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.UserId));
        await userManager
            .Received(1)
            .CreateAsync(
                Arg.Is<IdentityUser>(u => u.Email == email && u.UserName == email),
                password
            );
    }

    [TestMethod]
    public async Task CreateUserIfMissingAsync_ReturnsFailure_WhenCreateFails()
    {
        const string email = "failed@example.com";

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByEmailAsync(email).Returns(Task.FromResult<IdentityUser?>(null));
        userManager
            .CreateAsync(Arg.Any<IdentityUser>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityTestFactory.FailedResult("Create user failed.")));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.CreateUserIfMissingAsync(
            new CreateUserIfMissingCommand(email, "Passw0rd!"),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Create user failed.", result.Error);
    }

    [TestMethod]
    public async Task AddUserToRoleIfMissingAsync_ReturnsFailure_WhenUserIdMissing()
    {
        var sut = CreateSut();

        var result = await sut.AddUserToRoleIfMissingAsync(
            new AddUserToRoleIfMissingCommand("", Roles.Admin),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("User id is required.", result.Error);
    }

    [TestMethod]
    public async Task AddUserToRoleIfMissingAsync_ReturnsFailure_WhenRoleNameMissing()
    {
        var sut = CreateSut();

        var result = await sut.AddUserToRoleIfMissingAsync(
            new AddUserToRoleIfMissingCommand("u-1", ""),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Role is required.", result.Error);
    }

    [TestMethod]
    public async Task AddUserToRoleIfMissingAsync_ReturnsFailure_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("missing").Returns(Task.FromResult<IdentityUser?>(null));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.AddUserToRoleIfMissingAsync(
            new AddUserToRoleIfMissingCommand("missing", Roles.Admin),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("User not found.", result.Error);
    }

    [TestMethod]
    public async Task AddUserToRoleIfMissingAsync_ReturnsAlreadyExists_WhenUserAlreadyInRole()
    {
        var user = new IdentityUser { Id = "in-role", Email = "inrole@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<IdentityUser?>(user));
        userManager.IsInRoleAsync(user, Roles.Admin).Returns(Task.FromResult(true));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.AddUserToRoleIfMissingAsync(
            new AddUserToRoleIfMissingCommand(user.Id, Roles.Admin),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(result.Created);
    }

    [TestMethod]
    public async Task AddUserToRoleIfMissingAsync_CreatesMembership_WhenMissing()
    {
        var user = new IdentityUser { Id = "new-member", Email = "newmember@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<IdentityUser?>(user));
        userManager.IsInRoleAsync(user, Roles.Admin).Returns(Task.FromResult(false));
        userManager
            .AddToRoleAsync(user, Roles.Admin)
            .Returns(Task.FromResult(IdentityResult.Success));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.AddUserToRoleIfMissingAsync(
            new AddUserToRoleIfMissingCommand(user.Id, Roles.Admin),
            Ct
        );

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Created);
        await userManager.Received(1).AddToRoleAsync(user, Roles.Admin);
    }

    [TestMethod]
    public async Task AddUserToRoleIfMissingAsync_ReturnsFailure_WhenAddToRoleFails()
    {
        var user = new IdentityUser { Id = "member-fail", Email = "memberfail@example.com" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync(user.Id).Returns(Task.FromResult<IdentityUser?>(user));
        userManager.IsInRoleAsync(user, Roles.Admin).Returns(Task.FromResult(false));
        userManager
            .AddToRoleAsync(user, Roles.Admin)
            .Returns(Task.FromResult(IdentityTestFactory.FailedResult("Add role failed.")));

        var sut = CreateSut(userManager: userManager);

        var result = await sut.AddUserToRoleIfMissingAsync(
            new AddUserToRoleIfMissingCommand(user.Id, Roles.Admin),
            Ct
        );

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Add role failed.", result.Error);
    }

    private static UserCommandService CreateSut(
        UserManager<IdentityUser>? userManager = null,
        RoleManager<IdentityRole>? roleManager = null
    )
    {
        var effectiveUserManager = userManager ?? IdentityTestFactory.CreateUserManager();
        var effectiveRoleManager = roleManager ?? IdentityTestFactory.CreateRoleManager();
        var logger = Substitute.For<ILogger<UserCommandService>>();

        return new UserCommandService(effectiveUserManager, effectiveRoleManager, logger);
    }
}

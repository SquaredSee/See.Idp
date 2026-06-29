using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using See.Idp.Core.Dtos.Users;
using See.Idp.Infrastructure;
using See.Idp.Infrastructure.Services;
using See.Idp.Tests.Support;

namespace See.Idp.Tests.Services;

[TestClass]
public sealed class RegistrationCommandServiceTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task RegisterAsync_ReturnsSuccess_WithUserIdAndEncodedToken()
    {
        const string rawToken = "raw-confirmation-token";

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager
            .CreateAsync(Arg.Any<ApplicationUser>(), "Password1!")
            .Returns(Task.FromResult(IdentityResult.Success));
        userManager.GetUserIdAsync(Arg.Any<ApplicationUser>()).Returns(Task.FromResult("new-id"));
        userManager
            .GenerateEmailConfirmationTokenAsync(Arg.Any<ApplicationUser>())
            .Returns(Task.FromResult(rawToken));

        var result = await CreateSut(userManager)
            .RegisterAsync(new RegisterUserCommand("new@example.com", "Password1!"), Ct);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("new-id", result.UserId);
        Assert.IsNotNull(result.EmailConfirmationToken);
        Assert.AreEqual(0, result.Errors.Count);

        var decoded = Encoding.UTF8.GetString(
            WebEncoders.Base64UrlDecode(result.EmailConfirmationToken)
        );
        Assert.AreEqual(rawToken, decoded);
    }

    [TestMethod]
    public async Task RegisterAsync_ReturnsFailure_WhenIdentityFails()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager
            .CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(
                Task.FromResult(
                    IdentityTestFactory.FailedResult("Password too weak.", "Email already taken.")
                )
            );

        var result = await CreateSut(userManager)
            .RegisterAsync(new RegisterUserCommand("taken@example.com", "weak"), Ct);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNull(result.UserId);
        Assert.IsNull(result.EmailConfirmationToken);
        Assert.HasCount(2, result.Errors);
        CollectionAssert.Contains(
            (System.Collections.ICollection)result.Errors,
            "Password too weak."
        );
        CollectionAssert.Contains(
            (System.Collections.ICollection)result.Errors,
            "Email already taken."
        );
    }

    [TestMethod]
    public async Task ConfirmEmailAsync_ReturnsSuccess_WhenTokenIsValid()
    {
        const string rawToken = "raw-token";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
        var user = new ApplicationUser { Id = "user-1" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("user-1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .ConfirmEmailAsync(user, rawToken)
            .Returns(Task.FromResult(IdentityResult.Success));

        var result = await CreateSut(userManager)
            .ConfirmEmailAsync(new ConfirmEmailCommand("user-1", encodedToken), Ct);

        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task ConfirmEmailAsync_ReturnsFailure_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("missing").Returns(Task.FromResult<ApplicationUser?>(null));

        var result = await CreateSut(userManager)
            .ConfirmEmailAsync(new ConfirmEmailCommand("missing", "any-token"), Ct);

        Assert.IsFalse(result.Succeeded);
    }

    [TestMethod]
    public async Task ConfirmEmailAsync_ReturnsFailure_WhenTokenIsInvalid()
    {
        const string rawToken = "bad-token";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
        var user = new ApplicationUser { Id = "user-1" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("user-1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager
            .ConfirmEmailAsync(user, rawToken)
            .Returns(Task.FromResult(IdentityTestFactory.FailedResult("Invalid token.")));

        var result = await CreateSut(userManager)
            .ConfirmEmailAsync(new ConfirmEmailCommand("user-1", encodedToken), Ct);

        Assert.IsFalse(result.Succeeded);
    }

    [TestMethod]
    public async Task GenerateEmailConfirmationTokenAsync_ReturnsSuccess_WhenUserExists()
    {
        const string rawToken = "confirmation-token";
        var user = new ApplicationUser { Id = "user-1" };

        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("user-1").Returns(Task.FromResult<ApplicationUser?>(user));
        userManager.GenerateEmailConfirmationTokenAsync(user).Returns(Task.FromResult(rawToken));

        var result = await CreateSut(userManager)
            .GenerateEmailConfirmationTokenAsync(
                new GenerateEmailConfirmationTokenCommand("user-1"),
                Ct
            );

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.Token);
        var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(result.Token));
        Assert.AreEqual(rawToken, decoded);
    }

    [TestMethod]
    public async Task GenerateEmailConfirmationTokenAsync_ReturnsNotFound_WhenUserNotFound()
    {
        var userManager = IdentityTestFactory.CreateUserManager();
        userManager.FindByIdAsync("missing").Returns(Task.FromResult<ApplicationUser?>(null));

        var result = await CreateSut(userManager)
            .GenerateEmailConfirmationTokenAsync(
                new GenerateEmailConfirmationTokenCommand("missing"),
                Ct
            );

        Assert.IsFalse(result.Succeeded);
        Assert.IsNull(result.Token);
        Assert.IsNotNull(result.Error);
    }

    private static RegistrationCommandService CreateSut(
        UserManager<ApplicationUser>? userManager = null
    ) =>
        new(
            userManager ?? IdentityTestFactory.CreateUserManager(),
            Substitute.For<ILogger<RegistrationCommandService>>()
        );
}

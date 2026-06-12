using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Resend;
using See.Idp.Core.Configuration;
using See.Idp.Infrastructure;
using See.Idp.Infrastructure.Services;

namespace See.Idp.Tests.Services;

[TestClass]
public sealed class ResendEmailSenderTests
{
    [TestMethod]
    public async Task SendConfirmationLinkAsync_SendsEmailWithCorrectRecipientAndSubject()
    {
        var resend = Substitute.For<IResend>();
        var sut = CreateSut(resend);

        await sut.SendConfirmationLinkAsync(
            new ApplicationUser(),
            "user@example.com",
            "https://example.com/confirm"
        );

        await resend
            .Received(1)
            .EmailSendAsync(
                Arg.Is<EmailMessage>(m =>
                    m.To.Contains("user@example.com") && m.Subject == "Confirm your email"
                ),
                Arg.Any<System.Threading.CancellationToken>()
            );
    }

    [TestMethod]
    public async Task SendPasswordResetLinkAsync_SendsEmailWithCorrectRecipientAndSubject()
    {
        var resend = Substitute.For<IResend>();
        var sut = CreateSut(resend);

        await sut.SendPasswordResetLinkAsync(
            new ApplicationUser(),
            "user@example.com",
            "https://example.com/reset"
        );

        await resend
            .Received(1)
            .EmailSendAsync(
                Arg.Is<EmailMessage>(m =>
                    m.To.Contains("user@example.com") && m.Subject == "Reset your password"
                ),
                Arg.Any<System.Threading.CancellationToken>()
            );
    }

    [TestMethod]
    public async Task SendPasswordResetCodeAsync_SendsEmailWithCorrectRecipientAndSubject()
    {
        var resend = Substitute.For<IResend>();
        var sut = CreateSut(resend);

        await sut.SendPasswordResetCodeAsync(new ApplicationUser(), "user@example.com", "123456");

        await resend
            .Received(1)
            .EmailSendAsync(
                Arg.Is<EmailMessage>(m =>
                    m.To.Contains("user@example.com") && m.Subject == "Reset your password"
                ),
                Arg.Any<System.Threading.CancellationToken>()
            );
    }

    [TestMethod]
    public async Task SendConfirmationLinkAsync_DoesNotThrow_WhenResendFails()
    {
        var resend = Substitute.For<IResend>();
        resend
            .EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<System.Threading.CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Resend API error"));

        var sut = CreateSut(resend);

        await sut.SendConfirmationLinkAsync(
            new ApplicationUser(),
            "user@example.com",
            "https://example.com/confirm"
        );
    }

    [TestMethod]
    public async Task SendPasswordResetLinkAsync_DoesNotThrow_WhenResendFails()
    {
        var resend = Substitute.For<IResend>();
        resend
            .EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<System.Threading.CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Resend API error"));

        var sut = CreateSut(resend);

        await sut.SendPasswordResetLinkAsync(
            new ApplicationUser(),
            "user@example.com",
            "https://example.com/reset"
        );
    }

    private static ResendEmailSender CreateSut(
        IResend? resend = null,
        EmailOptions? emailOptions = null
    ) =>
        new(
            resend ?? Substitute.For<IResend>(),
            Options.Create(
                emailOptions
                    ?? new EmailOptions { ApiKey = "test-key", FromAddress = "from@test.com" }
            ),
            Substitute.For<ILogger<ResendEmailSender>>()
        );
}

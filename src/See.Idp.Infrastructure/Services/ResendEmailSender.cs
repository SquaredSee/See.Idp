using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Resend;
using See.Idp.Core.Configuration;

namespace See.Idp.Infrastructure.Services;

public sealed class ResendEmailSender(IResend resend, IOptions<EmailOptions> options)
    : IEmailSender<ApplicationUser>
{
    public async Task SendConfirmationLinkAsync(
        ApplicationUser user,
        string email,
        string confirmationLink
    )
    {
        var message = new EmailMessage();
        message.From = options.Value.FromAddress;
        message.To.Add(email);
        message.Subject = "Confirm your email";
        message.HtmlBody =
            $"<p>Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.</p>";

        await resend.EmailSendAsync(message);
    }

    public async Task SendPasswordResetLinkAsync(
        ApplicationUser user,
        string email,
        string resetLink
    )
    {
        var message = new EmailMessage();
        message.From = options.Value.FromAddress;
        message.To.Add(email);
        message.Subject = "Reset your password";
        message.HtmlBody =
            $"<p>Reset your password by <a href='{resetLink}'>clicking here</a>.</p>";

        await resend.EmailSendAsync(message);
    }

    public async Task SendPasswordResetCodeAsync(
        ApplicationUser user,
        string email,
        string resetCode
    )
    {
        var message = new EmailMessage();
        message.From = options.Value.FromAddress;
        message.To.Add(email);
        message.Subject = "Reset your password";
        message.HtmlBody = $"<p>Your password reset code is: <strong>{resetCode}</strong></p>";

        await resend.EmailSendAsync(message);
    }
}

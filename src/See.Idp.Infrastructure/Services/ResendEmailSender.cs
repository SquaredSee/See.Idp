using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;
using See.Idp.Core.Configuration;

namespace See.Idp.Infrastructure.Services;

public sealed class ResendEmailSender(
    IResend resend,
    IOptions<EmailOptions> options,
    ILogger<ResendEmailSender> logger
) : IEmailSender<ApplicationUser>
{
    public async Task SendConfirmationLinkAsync(
        ApplicationUser user,
        string email,
        string confirmationLink
    )
    {
        try
        {
            var message = new EmailMessage();
            message.From = options.Value.FromAddress;
            message.To.Add(email);
            message.Subject = "Confirm your email";
            message.HtmlBody =
                $"<p>Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.</p>";

            await resend.EmailSendAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send confirmation email to {Email}", email);
        }
    }

    public async Task SendPasswordResetLinkAsync(
        ApplicationUser user,
        string email,
        string resetLink
    )
    {
        try
        {
            var message = new EmailMessage();
            message.From = options.Value.FromAddress;
            message.To.Add(email);
            message.Subject = "Reset your password";
            message.HtmlBody =
                $"<p>Reset your password by <a href='{resetLink}'>clicking here</a>.</p>";

            await resend.EmailSendAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password reset email to {Email}", email);
        }
    }

    public async Task SendPasswordResetCodeAsync(
        ApplicationUser user,
        string email,
        string resetCode
    )
    {
        try
        {
            var message = new EmailMessage();
            message.From = options.Value.FromAddress;
            message.To.Add(email);
            message.Subject = "Reset your password";
            message.HtmlBody = $"<p>Your password reset code is: <strong>{resetCode}</strong></p>";

            await resend.EmailSendAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password reset code email to {Email}", email);
        }
    }
}

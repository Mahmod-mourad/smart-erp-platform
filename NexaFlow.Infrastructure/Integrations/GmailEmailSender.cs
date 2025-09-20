using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Enums;

namespace NexaFlow.Infrastructure.Integrations;

/// <summary>
/// Sends transactional email over Gmail SMTP using the tenant's app password. Skips (logs only)
/// when email isn't configured/enabled for the tenant.
/// </summary>
public class GmailEmailSender(
    IIntegrationConfigProvider configProvider,
    ILogger<GmailEmailSender> logger) : IEmailSender
{
    private const string SmtpHost = "smtp.gmail.com";
    private const int SmtpPort = 587;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var config = await configProvider.GetConfigAsync<GmailConfig>(IntegrationType.Gmail, ct);
        if (config is null || string.IsNullOrWhiteSpace(config.Email) || string.IsNullOrWhiteSpace(config.AppPassword))
        {
            logger.LogInformation("📧 [EMAIL SKIPPED — not configured] To: {To} | Subject: {Subject}", to, subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName ?? "NexaFlow", config.Email));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = WrapInTemplate(htmlBody) }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(config.Email, config.AppPassword, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        logger.LogInformation("Email sent to {To}", to);
    }

    private static string WrapInTemplate(string content) => $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8">
          <style>
            body { font-family: Arial, sans-serif; background: #f5f5f5; margin: 0; }
            .container { max-width: 600px; margin: 20px auto; background: #fff; border-radius: 8px; }
            .header { background: #1a1a2e; color: #fff; padding: 20px; border-radius: 8px 8px 0 0; text-align: center; }
            .content { padding: 24px; line-height: 1.6; color: #222; }
            .footer { text-align: center; color: #999; font-size: 12px; padding: 12px; }
          </style>
        </head>
        <body>
          <div class="container">
            <div class="header"><h2>NexaFlow</h2></div>
            <div class="content">{{content}}</div>
            <div class="footer">NexaFlow Business Management Platform</div>
          </div>
        </body>
        </html>
        """;
}

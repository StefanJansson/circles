using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Circles.Application.Services;

/// <summary>
/// Abstraction for sending a single e-mail message.
/// </summary>
public interface IEmailService
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody);
}

/// <summary>
/// SMTP-based e-mail sender using MailKit. All settings are read from
/// configuration (appsettings + environment variables / Azure App Settings)
/// under the "Email" section — no credentials are ever hard-coded.
///
/// If SMTP is not configured (e.g. in local dev without an SMTP server) the
/// service logs a warning and returns without throwing, so that notifications
/// never break the primary flow (creating an announcement, discussion, etc.).
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("E-post hoppades över: mottagaradress saknas (ämne: {Subject}).", subject);
            return;
        }

        var host = _config["Email:SmtpHost"];
        var portValue = _config["Email:SmtpPort"];
        var username = _config["Email:Username"];
        var password = _config["Email:Password"];
        var fromAddress = _config["Email:FromAddress"];
        var fromName = _config["Email:FromName"];

        // Graceful no-op when SMTP is not configured.
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogWarning(
                "SMTP är inte konfigurerat (Email:SmtpHost saknas). E-post till {ToEmail} med ämne \"{Subject}\" skickades INTE.",
                toEmail, subject);
            return;
        }

        var port = int.TryParse(portValue, out var p) ? p : 587;
        if (string.IsNullOrWhiteSpace(fromAddress)) fromAddress = "noreply@circles.app";
        if (string.IsNullOrWhiteSpace(fromName)) fromName = "Circles";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(toName ?? string.Empty, toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        try
        {
            using var client = new SmtpClient();

            // Port 465 = implicit TLS, otherwise STARTTLS when available.
            var secureOption = port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTlsWhenAvailable;

            await client.ConnectAsync(host, port, secureOption);

            if (!string.IsNullOrWhiteSpace(username))
                await client.AuthenticateAsync(username, password ?? string.Empty);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("E-post skickad till {ToEmail} (ämne: {Subject}).", toEmail, subject);
        }
        catch (Exception ex)
        {
            // Never rethrow — a failed notification must not break the caller.
            _logger.LogError(ex,
                "Misslyckades att skicka e-post till {ToEmail} (ämne: {Subject}).", toEmail, subject);
        }
    }
}

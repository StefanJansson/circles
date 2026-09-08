using Circles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Circles.Application.Services;

/// <summary>
/// Builds and sends e-mail notifications for key events in a circle
/// (new announcements, new discussions, task assignments).
///
/// Recipients are resolved from the domain model: only active members of the
/// circle that have a linked <c>UserAccount</c> with an e-mail address receive
/// mail. Delivery is delegated to <see cref="IEmailService"/>, which is a no-op
/// when SMTP is not configured.
/// </summary>
public class NotificationService
{
    private readonly CirclesDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(CirclesDbContext db, IEmailService email, ILogger<NotificationService> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Notify all active members of the circle (except the author) that a new
    /// announcement has been published.
    /// </summary>
    public async Task NotifyNewAnnouncementAsync(
        Guid circleId, string announcementTitle, string authorName, Guid? authorPersonId = null)
    {
        var circleName = await GetCircleNameAsync(circleId);
        var recipients = await GetActiveMemberContactsAsync(circleId, authorPersonId);

        if (recipients.Count == 0)
        {
            _logger.LogInformation(
                "Inga e-postmottagare för nytt meddelande i cirkel {CircleId}.", circleId);
            return;
        }

        var subject = $"Nytt meddelande i {circleName}: {announcementTitle}";

        foreach (var r in recipients)
        {
            var body = BuildHtml(
                heading: "Nytt meddelande",
                greeting: $"Hej {r.FirstName}!",
                bodyText:
                    $"<strong>{Escape(authorName)}</strong> har publicerat ett nytt meddelande i " +
                    $"<strong>{Escape(circleName)}</strong>:",
                highlightTitle: announcementTitle);

            await _email.SendAsync(r.Email, r.FullName, subject, body);
        }
    }

    /// <summary>
    /// Notify all active members of the circle (except the author) that a new
    /// discussion has been started.
    /// </summary>
    public async Task NotifyNewDiscussionAsync(
        Guid circleId, string discussionTitle, string authorName, Guid? authorPersonId = null)
    {
        var circleName = await GetCircleNameAsync(circleId);
        var recipients = await GetActiveMemberContactsAsync(circleId, authorPersonId);

        if (recipients.Count == 0)
        {
            _logger.LogInformation(
                "Inga e-postmottagare för ny diskussion i cirkel {CircleId}.", circleId);
            return;
        }

        var subject = $"Ny diskussion i {circleName}: {discussionTitle}";

        foreach (var r in recipients)
        {
            var body = BuildHtml(
                heading: "Ny diskussion",
                greeting: $"Hej {r.FirstName}!",
                bodyText:
                    $"<strong>{Escape(authorName)}</strong> har startat en ny diskussion i " +
                    $"<strong>{Escape(circleName)}</strong>:",
                highlightTitle: discussionTitle);

            await _email.SendAsync(r.Email, r.FullName, subject, body);
        }
    }

    /// <summary>
    /// Notify the person a task was assigned to.
    /// </summary>
    public async Task NotifyTaskAssignedAsync(
        Guid assignedToPersonId, string taskTitle, string circleName, string assignedByName)
    {
        var contact = await _db.Persons
            .Where(p => p.Id == assignedToPersonId && p.UserAccount != null)
            .Select(p => new
            {
                p.FirstName,
                FullName = p.FirstName + " " + p.LastName,
                Email = p.UserAccount!.Email
            })
            .FirstOrDefaultAsync();

        if (contact is null || string.IsNullOrWhiteSpace(contact.Email))
        {
            _logger.LogInformation(
                "Uppgiftstilldelning: person {PersonId} saknar konto/e-post, ingen notis skickad.",
                assignedToPersonId);
            return;
        }

        var subject = $"Du har fått en ny uppgift: {taskTitle}";

        var body = BuildHtml(
            heading: "Ny uppgift tilldelad",
            greeting: $"Hej {contact.FirstName}!",
            bodyText:
                $"<strong>{Escape(assignedByName)}</strong> har tilldelat dig uppgiften " +
                $"<strong>{Escape(taskTitle)}</strong> i <strong>{Escape(circleName)}</strong>.",
            highlightTitle: taskTitle);

        await _email.SendAsync(contact.Email, contact.FullName, subject, body);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private record Contact(string FirstName, string FullName, string Email);

    private async Task<List<Contact>> GetActiveMemberContactsAsync(Guid circleId, Guid? excludePersonId)
    {
        var now = DateTime.UtcNow;

        return await _db.Memberships
            .Where(m => m.CircleId == circleId
                        && m.ValidFrom <= now
                        && (m.ValidUntil == null || m.ValidUntil > now)
                        && m.Person!.UserAccount != null
                        && (excludePersonId == null || m.PersonId != excludePersonId))
            .Select(m => new Contact(
                m.Person!.FirstName,
                m.Person.FirstName + " " + m.Person.LastName,
                m.Person.UserAccount!.Email))
            // A person may hold several roles in one circle — dedupe by e-mail.
            .Distinct()
            .ToListAsync();
    }

    private async Task<string> GetCircleNameAsync(Guid circleId)
    {
        var name = await _db.Circles
            .Where(c => c.Id == circleId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync();
        return name ?? "din cirkel";
    }

    private static string Escape(string? value) =>
        System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

    /// <summary>
    /// Simple, inline-styled HTML template following the Circles design
    /// (cream background, navy text, forest accent). Inline styles are required
    /// because most e-mail clients strip &lt;style&gt; blocks.
    /// </summary>
    private static string BuildHtml(string heading, string greeting, string bodyText, string highlightTitle)
    {
        return $@"<!DOCTYPE html>
<html lang=""sv"">
<head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""></head>
<body style=""margin:0;padding:0;background-color:#FAFAF8;font-family:'Segoe UI',Helvetica,Arial,sans-serif;color:#1C2B3A;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#FAFAF8;padding:24px 0;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""430"" cellpadding=""0"" cellspacing=""0"" style=""max-width:430px;width:100%;background-color:#ffffff;border:1px solid #E8E6E1;border-radius:12px;overflow:hidden;"">
          <tr>
            <td style=""background-color:#1C2B3A;padding:20px 28px;"">
              <span style=""color:#FAFAF8;font-size:20px;font-weight:700;letter-spacing:0.5px;"">Circles</span>
            </td>
          </tr>
          <tr>
            <td style=""padding:28px;"">
              <p style=""margin:0 0 4px 0;font-size:13px;text-transform:uppercase;letter-spacing:1px;color:#4A7C59;font-weight:600;"">{Escape(heading)}</p>
              <p style=""margin:0 0 16px 0;font-size:16px;color:#1C2B3A;"">{Escape(greeting)}</p>
              <p style=""margin:0 0 16px 0;font-size:15px;line-height:1.6;color:#1C2B3A;"">{bodyText}</p>
              <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                <tr>
                  <td style=""background-color:#FAFAF8;border-left:4px solid #4A7C59;border-radius:6px;padding:14px 18px;"">
                    <span style=""font-size:16px;font-weight:600;color:#1C2B3A;"">{Escape(highlightTitle)}</span>
                  </td>
                </tr>
              </table>
              <p style=""margin:24px 0 0 0;font-size:14px;line-height:1.6;color:#6B7280;"">Logga in i Circles för att läsa mer.</p>
            </td>
          </tr>
          <tr>
            <td style=""padding:18px 28px;border-top:1px solid #E8E6E1;"">
              <p style=""margin:0;font-size:12px;line-height:1.5;color:#6B7280;"">Du får detta mejl eftersom du är medlem i Danmarks IF via Circles.</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}

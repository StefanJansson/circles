using Circles.Domain.Entities;
using Circles.Domain.Enums;
using Circles.Infrastructure.Persistence;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Circles.Infrastructure.Sync;

/// <summary>
/// Imports a circle's events from a laget.se ICS calendar feed. laget.se is one
/// of the tools this platform replaces, so during the transition a team can keep
/// maintaining its schedule there and simply subscribe to it here.
/// </summary>
public class LagetSeCalendarSyncService : ICalendarSyncService
{
    private const string ExternalSourceName = "laget.se";

    private readonly CirclesDbContext _db;
    private readonly HttpClient _http;
    private readonly ILogger<LagetSeCalendarSyncService> _logger;

    public LagetSeCalendarSyncService(
        CirclesDbContext db,
        HttpClient http,
        ILogger<LagetSeCalendarSyncService> logger)
    {
        _db = db;
        _http = http;
        _logger = logger;
    }

    public async Task<int> SyncCircleAsync(Guid circleId, CancellationToken ct = default)
    {
        var circle = await _db.Circles
            .FirstOrDefaultAsync(c => c.Id == circleId, ct);

        if (circle is null)
            throw new InvalidOperationException($"Cirkeln {circleId} hittades inte.");

        if (string.IsNullOrWhiteSpace(circle.LagetSeCalendarUrl))
        {
            _logger.LogInformation(
                "Circle {CircleId} has no calendar URL configured; skipping sync.", circleId);
            return 0;
        }

        var icsText = await FetchIcsAsync(circle.LagetSeCalendarUrl!, ct);
        var calendar = Calendar.Load(icsText);
        if (calendar?.Events is null)
            return 0;

        // Existing events for this circle that came from an external feed,
        // keyed by their feed UID for O(1) upsert lookups.
        var existing = await _db.Events
            .Where(e => e.CircleId == circleId && e.ExternalId != null)
            .ToDictionaryAsync(e => e.ExternalId!, ct);

        var now = DateTime.UtcNow;
        var imported = 0;

        foreach (var vevent in calendar.Events)
        {
            var uid = vevent.Uid;
            var start = vevent.Start;
            if (string.IsNullOrWhiteSpace(uid) || start is null)
                continue; // Skip malformed entries without an id or start time.

            var title = string.IsNullOrWhiteSpace(vevent.Summary)
                ? "(namnlös händelse)"
                : vevent.Summary.Trim();

            var startsAt = start.Value;
            DateTime? endsAt = vevent.End?.Value;
            var location = string.IsNullOrWhiteSpace(vevent.Location)
                ? null
                : vevent.Location.Trim();
            var description = string.IsNullOrWhiteSpace(vevent.Description)
                ? null
                : vevent.Description.Trim();
            var type = MapCategory(vevent);

            if (existing.TryGetValue(uid, out var existingEvent))
            {
                existingEvent.Title = title;
                existingEvent.Description = description;
                existingEvent.Type = type;
                existingEvent.StartsAt = startsAt;
                existingEvent.EndsAt = endsAt;
                existingEvent.Location = location;
                existingEvent.ExternalSource = ExternalSourceName;
                existingEvent.UpdatedAt = now;
            }
            else
            {
                _db.Events.Add(new Event
                {
                    Id = Guid.NewGuid(),
                    CircleId = circleId,
                    Title = title,
                    Description = description,
                    Type = type,
                    StartsAt = startsAt,
                    EndsAt = endsAt,
                    Location = location,
                    ExternalId = uid,
                    ExternalSource = ExternalSourceName,
                    CreatedAt = now
                });
            }

            imported++;
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Synced {Count} events for circle {CircleId} from laget.se.", imported, circleId);

        return imported;
    }

    private async Task<string> FetchIcsAsync(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        // Some calendar hosts reject requests without a browser-like User-Agent.
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Circles calendar sync)");

        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    /// <summary>
    /// Maps a laget.se calendar category to our <see cref="EventType"/>.
    /// laget.se uses Swedish category names: "Match", "Träning" and other
    /// activity labels ("Övrig aktivitet", …).
    /// </summary>
    private static EventType MapCategory(CalendarEvent vevent)
    {
        foreach (var category in vevent.Categories)
        {
            if (string.IsNullOrWhiteSpace(category))
                continue;

            var normalized = category.Trim().ToLowerInvariant();
            if (normalized.Contains("match"))
                return EventType.Match;
            if (normalized.Contains("träning") || normalized.Contains("traning") || normalized.Contains("training"))
                return EventType.Training;
        }

        return EventType.Other;
    }
}

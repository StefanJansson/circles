namespace Circles.Infrastructure.Sync;

/// <summary>
/// Synchronises a circle's events from its external calendar subscription
/// (currently a laget.se ICS feed). The sync is idempotent: events are matched
/// on (CircleId, ExternalId) so re-running never creates duplicates.
/// </summary>
public interface ICalendarSyncService
{
    /// <summary>
    /// Fetches and imports events for the given circle from its configured
    /// calendar URL. Returns the number of events created or updated.
    /// Returns 0 if the circle has no calendar URL configured.
    /// </summary>
    Task<int> SyncCircleAsync(Guid circleId, CancellationToken ct = default);
}

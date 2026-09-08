using Circles.Application.Services;
using Circles.Domain.Enums;
using Circles.Domain.Interfaces;
using Circles.Web.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Circles.Web.Components.Pages;

public class HandelserBase : ComponentBase
{
    [Parameter] public Guid CircleId { get; set; }

    [Inject] private EventService EventService { get; set; } = default!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] private CirclesQueryService CirclesQueryService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    protected string CircleName { get; private set; } = string.Empty;
    protected List<EventDto> Events { get; private set; } = [];
    protected bool IsLoading { get; private set; } = true;
    protected bool CanManage { get; private set; }
    protected bool IsSyncing { get; private set; }
    protected string? SyncMessage { get; private set; }
    protected string? ErrorMessage { get; private set; }

    private Guid _personId;

    protected override async Task OnParametersSetAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _personId = authState.User.GetPersonId() ?? Guid.Empty;

        var perms = await AuthorizationService.GetPersonPermissionsInCircleAsync(_personId, CircleId);
        CanManage = perms.Contains(PermissionType.PublishAnnouncements)
                 || perms.Contains(PermissionType.AdministerMembers);

        try
        {
            var accessible = await CirclesQueryService.GetAccessibleCirclesAsync(_personId);
            CircleName = accessible.FirstOrDefault(c => c.CircleId == CircleId)?.Name ?? "Cirkel";
        }
        catch
        {
            CircleName = "Cirkel";
        }

        await LoadEventsAsync();
    }

    private async Task LoadEventsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Events = await EventService.GetEventsAsync(_personId, CircleId);
        }
        catch (UnauthorizedAccessException)
        {
            Events = [];
            ErrorMessage = "Du saknar behörighet att se händelser i den här cirkeln.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task SyncAsync()
    {
        SyncMessage = null;
        ErrorMessage = null;
        IsSyncing = true;
        try
        {
            var imported = await EventService.SyncEventsAsync(_personId, CircleId);
            SyncMessage = imported > 0
                ? $"Synkroniserade {imported} händelser från laget.se."
                : "Inga händelser att synkronisera (ingen kalender kopplad).";
            await LoadEventsAsync();
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "Du saknar behörighet att synkronisera kalendern.";
        }
        catch
        {
            ErrorMessage = "Kunde inte hämta kalendern från laget.se. Försök igen senare.";
        }
        finally
        {
            IsSyncing = false;
        }
    }

    // ---- View helpers -----------------------------------------------------

    protected static string TypeLabel(EventType type) => type switch
    {
        EventType.Match    => "Match",
        EventType.Training => "Träning",
        _                  => "Övrigt"
    };

    protected static string TypeCssClass(EventType type) => type switch
    {
        EventType.Match    => "event-badge-match",
        EventType.Training => "event-badge-training",
        _                  => "event-badge-other"
    };

    protected static bool IsPast(EventDto ev) =>
        (ev.EndsAt ?? ev.StartsAt) < DateTime.Now;

    /// <summary>Formats an event's date/time range in Swedish, e.g. "lör 18 jan · 11:00–12:30".</summary>
    protected static string FormatWhen(EventDto ev)
    {
        var culture = System.Globalization.CultureInfo.GetCultureInfo("sv-SE");
        var start = ev.StartsAt;
        var date = start.ToString("ddd d MMM", culture);
        var startTime = start.ToString("HH:mm", culture);

        if (ev.EndsAt is { } end)
        {
            if (end.Date == start.Date)
                return $"{date} · {startTime}–{end.ToString("HH:mm", culture)}";
            return $"{date} {startTime} – {end.ToString("ddd d MMM HH:mm", culture)}";
        }

        return $"{date} · {startTime}";
    }
}

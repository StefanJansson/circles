using Circles.Application.Authorization;
using Circles.Domain.Interfaces;
using Circles.Application.Services;
using Circles.Domain.Enums;
using Circles.Web.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Circles.Web.Components.Pages;

public class MeddelandenBase : ComponentBase
{
    [Parameter] public Guid CircleId { get; set; }

    [Inject] private AnnouncementService AnnouncementService { get; set; } = default!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    protected string CircleName { get; private set; } = string.Empty;
    protected List<AnnouncementDto> Announcements { get; private set; } = [];
    protected bool IsLoading { get; private set; } = true;
    protected bool CanManage { get; private set; }
    protected string NewTitle { get; set; } = string.Empty;
    protected string NewBody { get; set; } = string.Empty;
    protected bool IsSaving { get; private set; }
    protected string? ErrorMessage { get; private set; }

    private Guid _personId;

    protected override async Task OnParametersSetAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _personId = authState.User.GetPersonId() ?? Guid.Empty;

        var perms = await AuthorizationService.GetPersonPermissionsInCircleAsync(_personId, CircleId);
        CanManage = perms.Contains(PermissionType.PublishAnnouncements);

        await LoadAnnouncementsAsync();
    }

    private async Task LoadAnnouncementsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Announcements = await AnnouncementService.GetAnnouncementsAsync(_personId, CircleId);
            if (Announcements.Count > 0)
                CircleName = Announcements[0].CircleName;
        }
        catch (UnauthorizedAccessException)
        {
            Announcements = [];
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task PublishAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(NewTitle))
        {
            ErrorMessage = "Rubrik får inte vara tom.";
            return;
        }
        if (string.IsNullOrWhiteSpace(NewBody))
        {
            ErrorMessage = "Innehåll får inte vara tomt.";
            return;
        }

        IsSaving = true;
        try
        {
            var ann = await AnnouncementService.CreateAnnouncementAsync(
                _personId, CircleId, NewTitle, NewBody);

            if (string.IsNullOrEmpty(CircleName))
                CircleName = ann.CircleName;

            Announcements.Insert(0, ann);
            NewTitle = string.Empty;
            NewBody = string.Empty;
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "Du saknar behörighet att publicera meddelanden.";
        }
        finally
        {
            IsSaving = false;
        }
    }
}

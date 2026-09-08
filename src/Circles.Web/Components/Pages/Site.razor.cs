using Circles.Application.DTOs;
using Circles.Application.Services;
using Circles.Web.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Circles.Web.Components.Pages;

/// <summary>
/// Platform overview reserved for the site administrator — the operator who sits
/// above every organization. Lists all organizations on the platform.
/// </summary>
public class SiteBase : ComponentBase
{
    [Inject] private SiteAdminService SiteAdminService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    protected bool IsLoading { get; private set; } = true;
    protected bool IsSiteAdmin { get; private set; }

    protected List<SiteOrganizationDto> Organizations { get; private set; } = [];

    protected int TotalCircles => Organizations.Sum(o => o.CircleCount);
    protected int TotalMembers => Organizations.Sum(o => o.MemberCount);

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        var accountId = user.GetUserAccountId();
        if (accountId is { } id && user.IsSiteAdmin())
            IsSiteAdmin = await SiteAdminService.IsSiteAdminAsync(id);

        if (IsSiteAdmin)
            Organizations = await SiteAdminService.GetAllOrganizationsAsync();

        IsLoading = false;
    }
}

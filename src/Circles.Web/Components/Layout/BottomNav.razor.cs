using Circles.Application.Services;
using Circles.Web.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Circles.Web.Components.Layout;

public class BottomNavBase : ComponentBase
{
    [Inject] private AdminService AdminService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    protected bool CanAccessAdmin { get; private set; }
    protected bool IsSiteAdmin { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        IsSiteAdmin = user.IsSiteAdmin();

        var personId = user.GetPersonId();
        if (personId is { } pid && pid != Guid.Empty)
            CanAccessAdmin = await AdminService.CanAccessAdminAsync(pid);
    }
}

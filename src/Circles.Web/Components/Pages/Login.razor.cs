using Circles.Web.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Circles.Web.Components.Pages;

public class LoginBase : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] protected ExternalAuthOptions ExternalAuth { get; set; } = default!;

    [SupplyParameterFromQuery] public string? Error { get; set; }
    [SupplyParameterFromQuery] public string? Email { get; set; }
    [SupplyParameterFromQuery] public string? Sent { get; set; }
    [SupplyParameterFromQuery] public string? DevToken { get; set; }
    [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Already signed in? Skip the form.
        var state = await AuthState.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated == true)
            Nav.NavigateTo(string.IsNullOrWhiteSpace(ReturnUrl) ? "/hem" : ReturnUrl, forceLoad: false);
    }
}

using Circles.Web.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Circles.Web.Components.Pages;

public class ProfilBase : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;

    protected string? _fullName;
    protected string? _email;
    protected bool _linked;

    protected override async Task OnInitializedAsync()
    {
        var user = (await AuthState.GetAuthenticationStateAsync()).User;
        _fullName = user.GetFullName();
        _email = user.GetEmail();
        _linked = user.GetPersonId() is not null;
    }
}

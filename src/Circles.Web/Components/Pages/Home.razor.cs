using Microsoft.AspNetCore.Components;

namespace Circles.Web.Components.Pages;

public class HomeBase : ComponentBase
{
    [Inject] private NavigationManager Nav { get; set; } = default!;

    // The app entry point simply forwards to /hem, which is protected and will
    // bounce anonymous visitors to /login.
    protected override void OnInitialized() => Nav.NavigateTo("/hem", forceLoad: false);
}

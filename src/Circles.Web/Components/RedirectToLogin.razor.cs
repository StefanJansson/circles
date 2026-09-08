using Microsoft.AspNetCore.Components;

namespace Circles.Web.Components;

public class RedirectToLoginBase : ComponentBase
{
    [Inject] private NavigationManager Nav { get; set; } = default!;

    // Sends unauthenticated visitors to the login page, preserving where they
    // were headed so they land back there after signing in. forceLoad ensures a
    // full navigation so the cookie challenge is handled by the server.
    protected override void OnInitialized()
    {
        var returnUrl = Nav.ToBaseRelativePath(Nav.Uri);
        var target = string.IsNullOrEmpty(returnUrl) || returnUrl.StartsWith("login")
            ? "/login"
            : $"/login?returnUrl=/{Uri.EscapeDataString(returnUrl)}";
        Nav.NavigateTo(target, forceLoad: true);
    }
}

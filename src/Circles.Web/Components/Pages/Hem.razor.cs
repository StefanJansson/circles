using Circles.Application.DTOs;
using Circles.Application.Services;
using Circles.Web.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Circles.Web.Components.Pages;

public class HemBase : ComponentBase
{
    [Inject] private CirclesQueryService Circles { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;

    protected List<CircleAccessDto>? _circles;
    protected string? _error;
    protected bool _hasPerson;
    protected string Greeting = "Hej!";

    protected override async Task OnInitializedAsync()
    {
        var user = (await AuthState.GetAuthenticationStateAsync()).User;
        var first = user.GetFirstName();
        Greeting = string.IsNullOrWhiteSpace(first) ? "Hej!" : $"Hej {first}!";

        var personId = user.GetPersonId();
        _hasPerson = personId is not null;

        if (personId is not { } pid)
        {
            _circles = new();
            return;
        }

        try
        {
            _circles = await Circles.GetAccessibleCirclesAsync(pid);
        }
        catch
        {
            _error = "Kunde inte hämta dina cirklar.";
        }
    }
}

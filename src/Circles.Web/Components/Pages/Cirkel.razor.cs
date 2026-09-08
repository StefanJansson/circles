using Circles.Application.DTOs;
using Circles.Application.Services;
using Circles.Domain.Enums;
using Circles.Web.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Circles.Web.Components.Pages;

public class CirkelBase : ComponentBase
{
    [Inject] private CirclesQueryService Circles { get; set; } = default!;
    [Inject] private ModuleService Modules { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;

    [Parameter] public Guid Id { get; set; }

    protected CircleAccessDto? _circle;
    protected List<MemberDto>? _members;
    protected List<ModuleType> _enabledModules = new();
    protected bool _loading = true;
    protected string? _error;

    protected override async Task OnInitializedAsync()
    {
        var user = (await AuthState.GetAuthenticationStateAsync()).User;
        var personId = user.GetPersonId();
        if (personId is not { } pid)
        {
            _loading = false;
            return;
        }

        try
        {
            var accessible = await Circles.GetAccessibleCirclesAsync(pid);
            _circle = accessible.FirstOrDefault(c => c.CircleId == Id);

            if (_circle is not null)
            {
                _members = await Circles.GetActiveMembersAsync(Id);
                _enabledModules = await Modules.GetEnabledModulesForCircleAsync(Id);
            }
        }
        catch
        {
            _error = "Kunde inte hämta cirkeln.";
        }
        finally
        {
            _loading = false;
        }
    }
}

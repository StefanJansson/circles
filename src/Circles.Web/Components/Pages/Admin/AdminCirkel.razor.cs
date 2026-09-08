using Circles.Application.DTOs;
using Circles.Application.Services;
using Circles.Domain.Enums;
using Circles.Web.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Circles.Web.Components.Pages.Admin;

public class AdminCirkelBase : ComponentBase
{
    [Inject] private AdminService AdminService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Parameter] public Guid Id { get; set; }

    protected enum Tab { Members, Modules }
    protected Tab ActiveTab { get; set; } = Tab.Members;

    protected bool IsLoading { get; private set; } = true;
    protected bool CanAccess { get; private set; }

    protected AdminCircleDto? Circle { get; private set; }
    protected List<MemberDto> Members { get; private set; } = [];
    protected List<ModuleStatusDto> Modules { get; private set; } = [];

    // Invite-form state.
    protected string InviteEmail { get; set; } = string.Empty;
    protected MembershipRole InviteRole { get; set; } = MembershipRole.Player;
    protected bool IsInviting { get; private set; }
    protected string? InviteMessage { get; private set; }
    protected bool InviteSuccess { get; private set; }

    protected static readonly MembershipRole[] Roles = Enum.GetValues<MembershipRole>();

    private Guid _personId;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _personId = authState.User.GetPersonId() ?? Guid.Empty;

        if (_personId == Guid.Empty)
        {
            IsLoading = false;
            return;
        }

        CanAccess = await AdminService.CanAdministerCircleAsync(_personId, Id);
        if (CanAccess)
        {
            Circle = await AdminService.GetCircleAsync(Id);
            await ReloadMembersAsync();
            Modules = await AdminService.GetModuleStatusesForCircleAsync(Id);
        }

        IsLoading = false;
    }

    private async Task ReloadMembersAsync()
    {
        Members = await AdminService.GetCircleMembersAsync(Id);
        Circle = await AdminService.GetCircleAsync(Id);
    }

    protected async Task InviteMember()
    {
        InviteMessage = null;
        IsInviting = true;
        try
        {
            var result = await AdminService.InviteMemberAsync(Id, InviteEmail, InviteRole);
            InviteSuccess = result.Success;
            InviteMessage = result.Message;
            if (result.Success)
            {
                InviteEmail = string.Empty;
                InviteRole = MembershipRole.Player;
                await ReloadMembersAsync();
            }
        }
        finally
        {
            IsInviting = false;
        }
    }

    protected async Task ChangeRole(Guid membershipId, ChangeEventArgs e)
    {
        if (!Enum.TryParse<MembershipRole>(e.Value?.ToString(), out var role))
            return;

        try
        {
            await AdminService.ChangeRoleAsync(membershipId, role);
            await ReloadMembersAsync();
        }
        catch (InvalidOperationException)
        {
            await ReloadMembersAsync();
        }
    }

    protected async Task EndMembership(Guid membershipId)
    {
        try
        {
            await AdminService.EndMembershipAsync(membershipId);
            await ReloadMembersAsync();
        }
        catch (InvalidOperationException)
        {
            await ReloadMembersAsync();
        }
    }

    protected async Task ToggleModule(ModuleType module)
    {
        await AdminService.ToggleModuleForCircleAsync(Id, module);
        Modules = await AdminService.GetModuleStatusesForCircleAsync(Id);
    }
}

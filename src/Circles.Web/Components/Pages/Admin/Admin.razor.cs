using Circles.Application.DTOs;
using Circles.Application.Services;
using Circles.Domain.Enums;
using Circles.Web.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Circles.Web.Components.Pages.Admin;

public class AdminBase : ComponentBase
{
    [Inject] private AdminService AdminService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    protected bool IsLoading { get; private set; } = true;
    protected bool CanAccess { get; private set; }

    protected List<AdminCircleDto> Circles { get; private set; } = [];
    protected List<OrganizationDto> Organizations { get; private set; } = [];

    protected List<AdminCircleDto> ActiveCircles => Circles.Where(c => !c.IsArchived).ToList();
    protected List<AdminCircleDto> ArchivedCircles => Circles.Where(c => c.IsArchived).ToList();

    // Create-form state.
    protected string NewOrgId { get; set; } = string.Empty;
    protected string NewName { get; set; } = string.Empty;
    protected CircleType NewType { get; set; } = CircleType.Team;
    protected string NewDescription { get; set; } = string.Empty;
    protected string NewParentId { get; set; } = string.Empty;
    protected bool IsCreating { get; private set; }
    protected string? CreateError { get; private set; }

    protected static readonly CircleType[] CircleTypes = Enum.GetValues<CircleType>();

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

        await ReloadAsync();
        Organizations = await AdminService.GetAdministrableOrganizationsAsync(_personId);
        if (Organizations.Count > 0)
            NewOrgId = Organizations[0].Id.ToString();

        CanAccess = Circles.Count > 0 || Organizations.Count > 0;
        IsLoading = false;
    }

    private async Task ReloadAsync()
    {
        Circles = await AdminService.GetAdministrableCirclesAsync(_personId);
    }

    /// <summary>Non-archived circles in the given organization, for the parent dropdown.</summary>
    protected List<AdminCircleDto> CirclesForOrg(string orgId)
    {
        return Guid.TryParse(orgId, out var oid)
            ? Circles.Where(c => c.OrganizationId == oid && !c.IsArchived).ToList()
            : Circles.Where(c => !c.IsArchived).ToList();
    }

    protected async Task CreateCircle()
    {
        CreateError = null;

        if (string.IsNullOrWhiteSpace(NewName))
        {
            CreateError = "Namn får inte vara tomt.";
            return;
        }

        Guid orgId;
        if (!Guid.TryParse(NewOrgId, out orgId))
        {
            if (Organizations.Count == 1)
                orgId = Organizations[0].Id;
            else
            {
                CreateError = "Välj en organisation.";
                return;
            }
        }

        IsCreating = true;
        try
        {
            Guid? parentId = Guid.TryParse(NewParentId, out var pid) ? pid : null;
            await AdminService.CreateCircleAsync(orgId, NewName, NewType, NewDescription, parentId);

            NewName = string.Empty;
            NewDescription = string.Empty;
            NewParentId = string.Empty;
            NewType = CircleType.Team;

            await ReloadAsync();
        }
        catch (InvalidOperationException ex)
        {
            CreateError = ex.Message;
        }
        finally
        {
            IsCreating = false;
        }
    }

    protected async Task ArchiveCircle(Guid circleId, bool archived)
    {
        try
        {
            await AdminService.SetCircleArchivedAsync(circleId, archived);
            await ReloadAsync();
        }
        catch (InvalidOperationException)
        {
            // Circle vanished; reload silently.
            await ReloadAsync();
        }
    }
}

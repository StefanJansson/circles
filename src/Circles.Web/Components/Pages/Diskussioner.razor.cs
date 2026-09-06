using Circles.Application.DTOs;
using Circles.Application.Services;
using Circles.Web.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Circles.Web.Components.Pages;

public partial class Diskussioner
{
    [Parameter] public Guid Id { get; set; }

    private CircleAccessDto? _circle;
    private List<DiscussionSummaryDto>? _discussions;
    private bool _loading = true;
    private string? _error;
    private bool _showForm;
    private bool _saving;
    private string _newTitle = "";
    private string _newBody = "";
    private string? _formError;
    private Guid _personId;

    protected override async Task OnInitializedAsync()
    {
        var user = (await AuthState.GetAuthenticationStateAsync()).User;
        var pid = user.GetPersonId();
        if (pid is null) { _loading = false; return; }
        _personId = pid.Value;

        try
        {
            var accessible = await Circles.GetAccessibleCirclesAsync(_personId);
            _circle = accessible.FirstOrDefault(c => c.CircleId == Id);
            if (_circle is not null)
                _discussions = await Content.GetDiscussionsAsync(Id, _personId);
        }
        catch (UnauthorizedAccessException)
        {
            _error = "Du saknar behörighet att visa diskussioner i den här cirkeln.";
        }
        catch
        {
            _error = "Kunde inte hämta diskussioner.";
        }
        finally { _loading = false; }
    }

    private void ToggleForm()
    {
        _showForm = !_showForm;
        _formError = null;
    }

    private async Task CreateDiscussion()
    {
        _formError = null;
        if (string.IsNullOrWhiteSpace(_newTitle)) { _formError = "Rubrik krävs."; return; }
        if (string.IsNullOrWhiteSpace(_newBody))  { _formError = "Meddelande krävs."; return; }

        _saving = true;
        try
        {
            var id = await Content.CreateDiscussionAsync(Id, _personId, _newTitle, _newBody);
            GoToDiscussion(id);
        }
        catch (UnauthorizedAccessException)
        {
            _formError = "Du saknar behörighet att skapa diskussioner.";
        }
        catch
        {
            _formError = "Kunde inte spara diskussionen.";
        }
        finally { _saving = false; }
    }

    private void GoToDiscussion(Guid id) => Nav.NavigateTo("/diskussioner/" + id);
}

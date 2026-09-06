using Circles.Application.DTOs;
using Circles.Application.Services;
using Circles.Web.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Circles.Web.Components.Pages;

public partial class Omrostningar
{
    [Parameter] public Guid Id { get; set; }

    private CircleAccessDto? _circle;
    private List<PollSummaryDto>? _polls;
    private bool _loading = true;
    private string? _error;
    private bool _showForm;
    private bool _saving;
    private string _newQuestion = "";
    private string _newOptionsRaw = "";
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
                _polls = await Content.GetPollsAsync(Id, _personId);
        }
        catch (UnauthorizedAccessException)
        {
            _error = "Du saknar behörighet att visa omröstningar i den här cirkeln.";
        }
        catch
        {
            _error = "Kunde inte hämta omröstningar.";
        }
        finally { _loading = false; }
    }

    private void ToggleForm()
    {
        _showForm = !_showForm;
        _formError = null;
    }

    private async Task CreatePoll()
    {
        _formError = null;
        if (string.IsNullOrWhiteSpace(_newQuestion)) { _formError = "Fråga krävs."; return; }

        var options = _newOptionsRaw
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .ToList();

        if (options.Count < 2) { _formError = "Minst två alternativ krävs."; return; }

        _saving = true;
        try
        {
            var id = await Content.CreatePollAsync(Id, _personId, _newQuestion, options);
            GoToPoll(id);
        }
        catch (UnauthorizedAccessException)
        {
            _formError = "Du saknar behörighet att skapa omröstningar.";
        }
        catch
        {
            _formError = "Kunde inte spara omröstningen.";
        }
        finally { _saving = false; }
    }

    private void GoToPoll(Guid id) => Nav.NavigateTo("/omrostningar/" + id);
}

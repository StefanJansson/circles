using Circles.Application.DTOs;
using Circles.Application.Services;
using Circles.Web.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Circles.Web.Components.Pages;

public partial class Uppgifter
{
    [Parameter] public Guid Id { get; set; }

    private CircleAccessDto? _circle;
    private List<CirclesTaskDto>? _tasks;
    private bool _loading = true;
    private string? _error;
    private bool _showForm;
    private bool _saving;
    private string _newTitle = "";
    private string _newDescription = "";
    private DateTime? _newDueDate;
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
                _tasks = await Content.GetTasksAsync(Id, _personId);
        }
        catch (UnauthorizedAccessException)
        {
            _error = "Du saknar behörighet att visa uppgifter i den här cirkeln.";
        }
        catch
        {
            _error = "Kunde inte hämta uppgifter.";
        }
        finally { _loading = false; }
    }

    private void ToggleForm()
    {
        _showForm = !_showForm;
        _formError = null;
    }

    private async Task CreateTask()
    {
        _formError = null;
        if (string.IsNullOrWhiteSpace(_newTitle)) { _formError = "Titel krävs."; return; }

        _saving = true;
        try
        {
            await Content.CreateTaskAsync(Id, _personId, _newTitle, _newDescription, _newDueDate);
            _newTitle = "";
            _newDescription = "";
            _newDueDate = null;
            _showForm = false;
            _tasks = await Content.GetTasksAsync(Id, _personId);
        }
        catch (UnauthorizedAccessException)
        {
            _formError = "Du saknar behörighet att skapa uppgifter.";
        }
        catch
        {
            _formError = "Kunde inte spara uppgiften.";
        }
        finally { _saving = false; }
    }

    private async Task ToggleTask(Guid taskId)
    {
        try
        {
            await Content.CompleteTaskAsync(taskId, _personId);
            _tasks = await Content.GetTasksAsync(Id, _personId);
        }
        catch { /* ignorera tyst */ }
    }
}

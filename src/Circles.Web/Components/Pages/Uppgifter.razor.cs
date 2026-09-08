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
    private List<MemberDto> _members = new();
    private bool _loading = true;
    private string? _error;

    // Top-level "new task" form
    private bool _showForm;
    private bool _saving;
    private string _newTitle = "";
    private string _newDescription = "";
    private DateTime? _newDueDate;
    private string _newAssignee = "";      // "" = unassigned, else person id
    private string? _formError;

    // Inline "new sub-task" form (one open at a time, keyed by parent id)
    private Guid? _subtaskParentId;
    private string _subtaskTitle = "";
    private string _subtaskAssignee = "";
    private bool _subtaskSaving;
    private string? _subtaskError;

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
            {
                _members = await Circles.GetActiveMembersAsync(Id);
                _tasks = await Content.GetTasksAsync(Id, _personId);
            }
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

    // ── Top-level task ────────────────────────────────────────────────────────

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
            Guid? assignee = Guid.TryParse(_newAssignee, out var a) ? a : null;
            await Content.CreateTaskAsync(
                Id, _personId, _newTitle, _newDescription, _newDueDate,
                parentTaskId: null, assignedToPersonId: assignee);
            _newTitle = "";
            _newDescription = "";
            _newDueDate = null;
            _newAssignee = "";
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

    // ── Sub-task ──────────────────────────────────────────────────────────────

    private void ToggleSubtaskForm(Guid parentId)
    {
        if (_subtaskParentId == parentId)
        {
            _subtaskParentId = null;
        }
        else
        {
            _subtaskParentId = parentId;
            _subtaskTitle = "";
            _subtaskAssignee = "";
        }
        _subtaskError = null;
    }

    private async Task CreateSubtask(Guid parentId)
    {
        _subtaskError = null;
        if (string.IsNullOrWhiteSpace(_subtaskTitle)) { _subtaskError = "Titel krävs."; return; }

        _subtaskSaving = true;
        try
        {
            Guid? assignee = Guid.TryParse(_subtaskAssignee, out var a) ? a : null;
            await Content.CreateTaskAsync(
                Id, _personId, _subtaskTitle, "", null,
                parentTaskId: parentId, assignedToPersonId: assignee);
            _subtaskParentId = null;
            _subtaskTitle = "";
            _subtaskAssignee = "";
            _tasks = await Content.GetTasksAsync(Id, _personId);
        }
        catch (UnauthorizedAccessException)
        {
            _subtaskError = "Du saknar behörighet att skapa deluppgifter.";
        }
        catch
        {
            _subtaskError = "Kunde inte spara deluppgiften.";
        }
        finally { _subtaskSaving = false; }
    }

    // ── Complete toggle ───────────────────────────────────────────────────────

    private async Task ToggleTask(Guid taskId)
    {
        try
        {
            await Content.CompleteTaskAsync(taskId, _personId);
            _tasks = await Content.GetTasksAsync(Id, _personId);
        }
        catch { /* ignorera tyst */ }
    }

    // ── View helpers ──────────────────────────────────────────────────────────

    private IEnumerable<CirclesTaskDto> SubTasksOf(Guid parentId) =>
        _tasks?.Where(t => t.ParentTaskId == parentId)
               .OrderBy(t => t.IsCompleted)
               .ThenBy(t => t.CreatedAt)
        ?? Enumerable.Empty<CirclesTaskDto>();
}

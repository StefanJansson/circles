using Circles.Application.DTOs;
using Circles.Application.Services;
using Circles.Web.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Circles.Web.Components.Pages;

public partial class Diskussion
{
    [Parameter] public Guid Id { get; set; }

    private DiscussionDetailDto? _discussion;
    private bool _loading = true;
    private string? _error;
    private bool _saving;
    private string _replyBody = "";
    private string? _replyError;
    private Guid _personId;

    protected override async Task OnInitializedAsync()
    {
        var user = (await AuthState.GetAuthenticationStateAsync()).User;
        var pid = user.GetPersonId();
        if (pid is null) { _loading = false; return; }
        _personId = pid.Value;

        try
        {
            _discussion = await Content.GetDiscussionAsync(Id, _personId);
        }
        catch (UnauthorizedAccessException)
        {
            _error = "Du saknar behörighet att visa den här diskussionen.";
        }
        catch
        {
            _error = "Kunde inte hämta diskussionen.";
        }
        finally { _loading = false; }
    }

    private async Task AddReply()
    {
        _replyError = null;
        if (string.IsNullOrWhiteSpace(_replyBody)) { _replyError = "Svaret får inte vara tomt."; return; }

        _saving = true;
        try
        {
            await Content.AddPostAsync(Id, _personId, _replyBody);
            _replyBody = "";
            _discussion = await Content.GetDiscussionAsync(Id, _personId);
        }
        catch (UnauthorizedAccessException)
        {
            _replyError = "Du saknar behörighet att svara i den här diskussionen.";
        }
        catch
        {
            _replyError = "Kunde inte spara svaret.";
        }
        finally { _saving = false; }
    }
}

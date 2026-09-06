using Circles.Application.DTOs;
using Circles.Application.Services;
using Circles.Web.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Circles.Web.Components.Pages;

public partial class Omrostning
{
    [Parameter] public Guid Id { get; set; }

    private PollDetailDto? _poll;
    private bool _loading = true;
    private string? _error;
    private string? _voteError;
    private string? _voteSuccess;
    private Guid _personId;

    protected override async Task OnInitializedAsync()
    {
        var user = (await AuthState.GetAuthenticationStateAsync()).User;
        var pid = user.GetPersonId();
        if (pid is null) { _loading = false; return; }
        _personId = pid.Value;
        await LoadPollAsync();
        _loading = false;
    }

    private async Task LoadPollAsync()
    {
        try
        {
            _poll = await Content.GetPollAsync(Id, _personId);
        }
        catch (UnauthorizedAccessException)
        {
            _error = "Du saknar behörighet att visa den här omröstningen.";
        }
        catch
        {
            _error = "Kunde inte hämta omröstningen.";
        }
    }

    private async Task CastVote(Guid optionId)
    {
        _voteError = null;
        _voteSuccess = null;

        if (_poll?.IsOpen == false)
        {
            _voteError = "Omröstningen är stängd.";
            return;
        }

        try
        {
            await Content.VoteAsync(Id, _personId, optionId);
            await LoadPollAsync();
            _voteSuccess = "Din röst har registrerats!";
        }
        catch (InvalidOperationException ex)
        {
            _voteError = ex.Message;
        }
        catch (UnauthorizedAccessException)
        {
            _voteError = "Du saknar behörighet att rösta.";
        }
        catch
        {
            _voteError = "Röstningen misslyckades.";
        }
    }
}

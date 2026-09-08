using Microsoft.AspNetCore.Components;

namespace Circles.Web.Components.Pages;

public class RegistreraBase : ComponentBase
{
    [SupplyParameterFromQuery] public string? Error { get; set; }

    // Wizard step: 1 = organization, 2 = admin, 3 = teams, 4 = review.
    protected int Step { get; set; } = 1;

    // ---- Step 1: organization ----
    protected string OrgName { get; set; } = string.Empty;

    // ---- Step 2: administrator ----
    protected string AdminFirstName { get; set; } = string.Empty;
    protected string AdminLastName { get; set; } = string.Empty;
    protected string Email { get; set; } = string.Empty;
    protected string Password { get; set; } = string.Empty;
    protected string PasswordConfirm { get; set; } = string.Empty;

    // ---- Step 3: teams ----
    protected List<string> Teams { get; set; } = new();
    protected string NewTeam { get; set; } = string.Empty;

    protected string? StepError { get; private set; }

    /// <summary>Teams serialized newline-separated for the hidden form field.</summary>
    protected string TeamsSerialized => string.Join("\n", Teams);

    protected void AddTeam()
    {
        var name = NewTeam.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;
        if (!Teams.Any(t => string.Equals(t, name, StringComparison.OrdinalIgnoreCase)))
            Teams.Add(name);
        NewTeam = string.Empty;
    }

    protected void RemoveTeam(string team) => Teams.Remove(team);

    protected void Next()
    {
        StepError = null;

        switch (Step)
        {
            case 1:
                if (string.IsNullOrWhiteSpace(OrgName))
                {
                    StepError = "Ange organisationens namn.";
                    return;
                }
                break;
            case 2:
                if (string.IsNullOrWhiteSpace(AdminFirstName) || string.IsNullOrWhiteSpace(AdminLastName))
                {
                    StepError = "Ange för- och efternamn.";
                    return;
                }
                if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
                {
                    StepError = "Ange en giltig e-postadress.";
                    return;
                }
                if (Password.Length < 8)
                {
                    StepError = "Lösenordet måste vara minst 8 tecken.";
                    return;
                }
                if (Password != PasswordConfirm)
                {
                    StepError = "Lösenorden matchar inte.";
                    return;
                }
                break;
        }

        if (Step < 4)
            Step++;
    }

    protected void Back()
    {
        StepError = null;
        if (Step > 1)
            Step--;
    }
}

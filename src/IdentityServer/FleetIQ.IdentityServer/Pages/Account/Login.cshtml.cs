using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FleetIQ.IdentityServer.Pages.Account;

public class LoginModel(TestUserStore users, IIdentityServerInteractionService interaction) : PageModel
{
    [BindProperty] public string Username { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var username = (Username ?? string.Empty).Trim();

        if (users.ValidateCredentials(username, Password))
        {
            var user = users.FindByUsername(username);
            var identityServerUser = new IdentityServerUser(user.SubjectId)
            {
                DisplayName = user.Username
            };

            await HttpContext.SignInAsync(identityServerUser);

            if (interaction.IsValidReturnUrl(ReturnUrl) || Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl!);
            }

            return Redirect("~/");
        }

        ErrorMessage = "Invalid username or password.";
        return Page();
    }
}

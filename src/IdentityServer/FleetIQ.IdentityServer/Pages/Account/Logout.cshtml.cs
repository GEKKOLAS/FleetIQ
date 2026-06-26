using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FleetIQ.IdentityServer.Pages.Account;

public class LogoutModel(IIdentityServerInteractionService interaction) : PageModel
{
    public async Task<IActionResult> OnGet(string? logoutId)
    {
        await HttpContext.SignOutAsync();

        var context = await interaction.GetLogoutContextAsync(logoutId, HttpContext.RequestAborted);
        if (!string.IsNullOrEmpty(context?.PostLogoutRedirectUri))
        {
            return Redirect(context.PostLogoutRedirectUri);
        }

        return Redirect("~/");
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FanzinePress.Web.Models;

namespace FanzinePress.Web.Pages.Admin.Users;

[Authorize(Roles = Roles.Admin)]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public class UserView
    {
        public string Id { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public List<UserView> Users { get; set; } = new();
    public string? CurrentUserId { get; set; }

    public async Task OnGetAsync()
    {
        CurrentUserId = _userManager.GetUserId(User);
        var users = _userManager.Users.OrderBy(u => u.Email).ToList();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            Users.Add(new UserView
            {
                Id = u.Id,
                Email = u.Email,
                DisplayName = u.DisplayName,
                CreatedAt = u.CreatedAt,
                Roles = roles.ToList()
            });
        }
    }

    public async Task<IActionResult> OnPostToggleRoleAsync(string userId)
    {
        var currentUserId = _userManager.GetUserId(User);
        if (userId == currentUserId)
        {
            TempData["Error"] = "Δεν μπορείτε να αλλάξετε τον δικό σας ρόλο.";
            return RedirectToPage();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return RedirectToPage();

        if (await _userManager.IsInRoleAsync(user, Roles.Admin))
        {
            await _userManager.RemoveFromRoleAsync(user, Roles.Admin);
            await _userManager.AddToRoleAsync(user, Roles.Editor);
            TempData["Success"] = $"Ο χρήστης {user.Email} είναι τώρα Editor.";
        }
        else
        {
            await _userManager.RemoveFromRoleAsync(user, Roles.Editor);
            await _userManager.AddToRoleAsync(user, Roles.Admin);
            TempData["Success"] = $"Ο χρήστης {user.Email} είναι τώρα Admin.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string userId)
    {
        var currentUserId = _userManager.GetUserId(User);
        if (userId == currentUserId)
        {
            TempData["Error"] = "Δεν μπορείτε να διαγράψετε τον εαυτό σας.";
            return RedirectToPage();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return RedirectToPage();

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            TempData["Success"] = $"Ο χρήστης {user.Email} διαγράφηκε.";
        }
        else
        {
            TempData["Error"] = "Σφάλμα: " + string.Join(", ", result.Errors.Select(e => e.Description));
        }

        return RedirectToPage();
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FanzinePress.Web.Data;
using FanzinePress.Web.Models;

namespace FanzinePress.Web.Pages.Issues;

public class PreviewModel : PageModel
{
    private readonly FanzinePressDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public PreviewModel(FanzinePressDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public Issue Issue { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var issue = await _db.Issues
            .Include(i => i.Articles).ThenInclude(a => a.Photos)
            .Include(i => i.Ads)
            .Include(i => i.Colophon)
            .Include(i => i.Settings)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (issue == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole(Roles.Admin);
        if (!isAdmin && issue.OwnerId != userId) return Forbid();

        Issue = issue;
        return Page();
    }
}

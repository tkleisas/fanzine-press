using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FanzinePress.Web.Data;
using FanzinePress.Web.Models;

namespace FanzinePress.Web.Pages;

public class IndexModel : PageModel
{
    private readonly FanzinePressDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(FanzinePressDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<Issue> Issues { get; set; } = [];

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole(Roles.Admin);

        var query = _db.Issues.Include(i => i.Articles).AsQueryable();
        if (!isAdmin)
        {
            query = query.Where(i => i.OwnerId == userId);
        }
        Issues = await query.OrderByDescending(i => i.Number).ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var userId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole(Roles.Admin);

        // Max number among the user's own issues (or all, for admin)
        var numberQuery = _db.Issues.AsQueryable();
        if (!isAdmin)
        {
            numberQuery = numberQuery.Where(i => i.OwnerId == userId);
        }
        var maxNumber = await numberQuery.MaxAsync(i => (int?)i.Number) ?? 0;

        var issue = new Issue
        {
            Title = $"Issue #{maxNumber + 1}",
            Number = maxNumber + 1,
            Date = DateTime.UtcNow,
            OwnerId = userId
        };
        _db.Issues.Add(issue);
        await _db.SaveChangesAsync();

        return RedirectToPage("/Issues/Edit", new { id = issue.Id });
    }
}

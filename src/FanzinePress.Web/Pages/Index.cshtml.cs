using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FanzinePress.Web.Data;
using FanzinePress.Web.Models;

namespace FanzinePress.Web.Pages;

public class IndexModel : PageModel
{
    private readonly FanzinePressDbContext _db;

    public IndexModel(FanzinePressDbContext db)
    {
        _db = db;
    }

    public List<Issue> Issues { get; set; } = [];

    public async Task OnGetAsync()
    {
        Issues = await _db.Issues
            .Include(i => i.Articles)
            .OrderByDescending(i => i.Number)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var maxNumber = await _db.Issues.MaxAsync(i => (int?)i.Number) ?? 0;
        var issue = new Issue
        {
            Title = $"Issue #{maxNumber + 1}",
            Number = maxNumber + 1,
            Date = DateTime.UtcNow
        };
        _db.Issues.Add(issue);
        await _db.SaveChangesAsync();

        return RedirectToPage("/Issues/Edit", new { id = issue.Id });
    }
}

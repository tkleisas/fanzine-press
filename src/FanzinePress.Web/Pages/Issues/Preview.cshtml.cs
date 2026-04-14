using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FanzinePress.Web.Data;
using FanzinePress.Web.Models;

namespace FanzinePress.Web.Pages.Issues;

public class PreviewModel : PageModel
{
    private readonly FanzinePressDbContext _db;

    public PreviewModel(FanzinePressDbContext db)
    {
        _db = db;
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
        Issue = issue;
        return Page();
    }
}

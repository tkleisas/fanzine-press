using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PuppeteerSharp;
using FanzinePress.Web.Data;
using FanzinePress.Web.Models;
using FanzinePress.Web.Services;

namespace FanzinePress.Web.Pages.Issues;

public class ExportPdfModel : PageModel
{
    private readonly FanzinePressDbContext _db;
    private readonly PdfService _pdfService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ExportPdfModel(FanzinePressDbContext db, PdfService pdfService, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _pdfService = pdfService;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var issue = await _db.Issues.FirstOrDefaultAsync(i => i.Id == id);
        if (issue == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole(Roles.Admin);
        if (!isAdmin && issue.OwnerId != userId) return Forbid();

        // Build the absolute URL to the Preview page.
        // PathBase is included so sub-path hosting (e.g. /fanzine-press) works,
        // and Scheme/Host come from X-Forwarded-* when behind a reverse proxy.
        var previewUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/Issues/Preview/{id}";

        // Forward the authentication cookies so PuppeteerSharp can fetch the
        // Preview page and image endpoints as the current user.
        var cookies = Request.Cookies
            .Select(c => new CookieParam
            {
                Name = c.Key,
                Value = c.Value,
                Domain = Request.Host.Host,
                Path = "/"
            })
            .ToList();

        var pdfBytes = await _pdfService.RenderPdfAsync(previewUrl, cookies);

        var fileName = $"{issue.Title.Replace(" ", "_")}_#{issue.Number}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }
}

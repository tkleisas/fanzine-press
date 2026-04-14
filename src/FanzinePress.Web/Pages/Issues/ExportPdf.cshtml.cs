using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FanzinePress.Web.Data;
using FanzinePress.Web.Services;

namespace FanzinePress.Web.Pages.Issues;

public class ExportPdfModel : PageModel
{
    private readonly FanzinePressDbContext _db;
    private readonly PdfService _pdfService;

    public ExportPdfModel(FanzinePressDbContext db, PdfService pdfService)
    {
        _db = db;
        _pdfService = pdfService;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var issue = await _db.Issues.FirstOrDefaultAsync(i => i.Id == id);
        if (issue == null) return NotFound();

        // Build the absolute URL to the Preview page
        var previewUrl = $"{Request.Scheme}://{Request.Host}/Issues/Preview/{id}";

        var pdfBytes = await _pdfService.RenderPdfAsync(previewUrl);

        var fileName = $"{issue.Title.Replace(" ", "_")}_#{issue.Number}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }
}

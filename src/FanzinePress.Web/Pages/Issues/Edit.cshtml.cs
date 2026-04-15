using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FanzinePress.Web.Data;
using FanzinePress.Web.Models;

namespace FanzinePress.Web.Pages.Issues;

public class EditModel : PageModel
{
    private readonly FanzinePressDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly UserManager<ApplicationUser> _userManager;

    public EditModel(FanzinePressDbContext db, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _env = env;
        _userManager = userManager;
    }

    public Issue Issue { get; set; } = null!;

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        // Authorize: only owner or admin may access this page's handlers
        if (context.HandlerArguments.TryGetValue("id", out var idObj) && idObj is int id)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole(Roles.Admin);
            var ownerId = await _db.Issues.Where(i => i.Id == id).Select(i => i.OwnerId).FirstOrDefaultAsync();
            if (ownerId == null)
            {
                context.Result = NotFound();
                return;
            }
            if (!isAdmin && ownerId != userId)
            {
                context.Result = Forbid();
                return;
            }
        }
        await next();
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var issue = await LoadIssue(id);
        if (issue == null) return NotFound();
        Issue = issue;
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateIssueAsync(int id, string title, int number, DateTime date, string template)
    {
        var issue = await _db.Issues.FindAsync(id);
        if (issue == null) return NotFound();

        issue.Title = title;
        issue.Number = number;
        issue.Date = date;
        issue.Template = template;
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateColophonAsync(int id, string publicationName, string? editors, string? contributors, string? contact, string? extraText)
    {
        var issue = await _db.Issues.Include(i => i.Colophon).FirstOrDefaultAsync(i => i.Id == id);
        if (issue == null) return NotFound();

        if (issue.Colophon == null)
        {
            issue.Colophon = new Colophon { IssueId = id };
            _db.Colophons.Add(issue.Colophon);
        }

        issue.Colophon.PublicationName = publicationName;
        issue.Colophon.Editors = editors;
        issue.Colophon.Contributors = contributors;
        issue.Colophon.Contact = contact;
        issue.Colophon.ExtraText = extraText;
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddArticleAsync(int id)
    {
        var maxOrder = await _db.Articles.Where(a => a.IssueId == id).MaxAsync(a => (int?)a.Order) ?? 0;
        _db.Articles.Add(new Article
        {
            IssueId = id,
            Title = "New Article",
            Order = maxOrder + 1
        });
        await _db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateArticleAsync(int id, int articleId, string articleTitle, string? subtitle, string? author, string body, int columnSpan, int order)
    {
        var article = await _db.Articles.FindAsync(articleId);
        if (article == null) return NotFound();

        article.Title = articleTitle;
        article.Subtitle = subtitle;
        article.Author = author;
        article.Body = body;
        article.ColumnSpan = columnSpan;
        article.Order = order;
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteArticleAsync(int id, int articleId)
    {
        var article = await _db.Articles.FindAsync(articleId);
        if (article != null)
        {
            _db.Articles.Remove(article);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUploadPhotoAsync(int id, int articleId, IFormFile photo)
    {
        if (photo == null || photo.Length == 0) return RedirectToPage(new { id });

        using var ms = new MemoryStream();
        await photo.CopyToAsync(ms);

        _db.Photos.Add(new Photo
        {
            ArticleId = articleId,
            FileName = photo.FileName,
            Caption = Path.GetFileNameWithoutExtension(photo.FileName),
            ImageData = ms.ToArray(),
            ImageContentType = photo.ContentType
        });
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostToggleVintageAsync(int id, int photoId)
    {
        var photo = await _db.Photos.FindAsync(photoId);
        if (photo != null)
        {
            photo.ApplyVintageEffect = !photo.ApplyVintageEffect;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeletePhotoAsync(int id, int photoId)
    {
        var photo = await _db.Photos.FindAsync(photoId);
        if (photo != null)
        {
            _db.Photos.Remove(photo);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUploadTitleImageAsync(int id, IFormFile titleImage)
    {
        if (titleImage == null || titleImage.Length == 0) return RedirectToPage(new { id });

        var issue = await _db.Issues.FindAsync(id);
        if (issue == null) return NotFound();

        using var ms = new MemoryStream();
        await titleImage.CopyToAsync(ms);

        issue.TitleImageFileName = titleImage.FileName;
        issue.TitleImageData = ms.ToArray();
        issue.TitleImageContentType = titleImage.ContentType;
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoveTitleImageAsync(int id)
    {
        var issue = await _db.Issues.FindAsync(id);
        if (issue == null) return NotFound();

        if (issue.TitleImageFileName != null)
        {
            issue.TitleImageFileName = null;
            issue.TitleImageData = null;
            issue.TitleImageContentType = null;
            await _db.SaveChangesAsync();
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddAdAsync(int id)
    {
        var maxOrder = await _db.Ads.Where(a => a.IssueId == id).MaxAsync(a => (int?)a.Order) ?? 0;
        _db.Ads.Add(new Ad
        {
            IssueId = id,
            Text = "New Ad",
            Order = maxOrder + 1
        });
        await _db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateAdAsync(int id, int adId, string? adText, string adSize, int adOrder)
    {
        var ad = await _db.Ads.FindAsync(adId);
        if (ad == null) return NotFound();

        ad.Text = adText;
        ad.Size = Enum.Parse<AdSize>(adSize);
        ad.Order = adOrder;
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAdAsync(int id, int adId)
    {
        var ad = await _db.Ads.FindAsync(adId);
        if (ad != null)
        {
            _db.Ads.Remove(ad);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUploadAdImageAsync(int id, int adId, IFormFile adImage)
    {
        if (adImage == null || adImage.Length == 0) return RedirectToPage(new { id });

        var ad = await _db.Ads.FindAsync(adId);
        if (ad == null) return NotFound();

        using var ms = new MemoryStream();
        await adImage.CopyToAsync(ms);

        ad.ImageFileName = adImage.FileName;
        ad.ImageData = ms.ToArray();
        ad.ImageContentType = adImage.ContentType;
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    private async Task<Issue?> LoadIssue(int id)
    {
        return await _db.Issues
            .Include(i => i.Articles).ThenInclude(a => a.Photos)
            .Include(i => i.Ads)
            .Include(i => i.Colophon)
            .Include(i => i.Settings)
            .FirstOrDefaultAsync(i => i.Id == id);
    }
}

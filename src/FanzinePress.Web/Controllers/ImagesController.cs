using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FanzinePress.Web.Data;
using FanzinePress.Web.Models;

namespace FanzinePress.Web.Controllers;

[Authorize]
[Route("api/images")]
[ApiController]
public class ImagesController : ControllerBase
{
    private readonly FanzinePressDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ImagesController(FanzinePressDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private bool IsAdmin => User.IsInRole(Roles.Admin);
    private string? CurrentUserId => _userManager.GetUserId(User);

    [HttpGet("photo/{id:int}")]
    public async Task<IActionResult> Photo(int id)
    {
        var photo = await _db.Photos
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new { p.ImageData, p.ImageContentType, OwnerId = p.Article.Issue.OwnerId })
            .FirstOrDefaultAsync();

        if (photo?.ImageData == null) return NotFound();
        if (!IsAdmin && photo.OwnerId != CurrentUserId) return Forbid();

        return File(photo.ImageData, photo.ImageContentType ?? "image/jpeg");
    }

    [HttpGet("ad/{id:int}")]
    public async Task<IActionResult> Ad(int id)
    {
        var ad = await _db.Ads
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new { a.ImageData, a.ImageContentType, OwnerId = a.Issue.OwnerId })
            .FirstOrDefaultAsync();

        if (ad?.ImageData == null) return NotFound();
        if (!IsAdmin && ad.OwnerId != CurrentUserId) return Forbid();

        return File(ad.ImageData, ad.ImageContentType ?? "image/jpeg");
    }

    [HttpGet("title/{id:int}")]
    public async Task<IActionResult> TitleImage(int id)
    {
        var issue = await _db.Issues
            .AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => new { i.TitleImageData, i.TitleImageContentType, i.OwnerId })
            .FirstOrDefaultAsync();

        if (issue?.TitleImageData == null) return NotFound();
        if (!IsAdmin && issue.OwnerId != CurrentUserId) return Forbid();

        return File(issue.TitleImageData, issue.TitleImageContentType ?? "image/jpeg");
    }
}

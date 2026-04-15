namespace FanzinePress.Web.Models;

public class Issue
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Number { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Template { get; set; } = "classic";
    public string? TitleImageFileName { get; set; }
    public IssueStatus Status { get; set; } = IssueStatus.Draft;

    // Title image stored in DB as binary
    public byte[]? TitleImageData { get; set; }
    public string? TitleImageContentType { get; set; }

    // Owner of this issue (user who created it)
    public string? OwnerId { get; set; }
    public ApplicationUser? Owner { get; set; }

    public List<Article> Articles { get; set; } = [];
    public List<Ad> Ads { get; set; } = [];
    public Colophon? Colophon { get; set; }
    public IssueSettings? Settings { get; set; }
}

public enum IssueStatus
{
    Draft,
    Published
}

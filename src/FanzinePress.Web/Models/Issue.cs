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

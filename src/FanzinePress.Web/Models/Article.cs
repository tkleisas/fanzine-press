namespace FanzinePress.Web.Models;

public class Article
{
    public int Id { get; set; }
    public int IssueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? Author { get; set; }
    public int ColumnSpan { get; set; } = 1;
    public int Order { get; set; }

    public Issue Issue { get; set; } = null!;
    public List<Photo> Photos { get; set; } = [];
}

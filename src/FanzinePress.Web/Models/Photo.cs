namespace FanzinePress.Web.Models;

public class Photo
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string? Credit { get; set; }
    public bool ApplyVintageEffect { get; set; }

    public Article Article { get; set; } = null!;
}

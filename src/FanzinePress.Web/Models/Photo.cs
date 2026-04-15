namespace FanzinePress.Web.Models;

public class Photo
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string? Credit { get; set; }
    public bool ApplyVintageEffect { get; set; }

    // Image stored in DB as binary
    public byte[]? ImageData { get; set; }
    public string? ImageContentType { get; set; }

    public Article Article { get; set; } = null!;
}

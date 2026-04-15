namespace FanzinePress.Web.Models;

public class Ad
{
    public int Id { get; set; }
    public int IssueId { get; set; }
    public string? ImageFileName { get; set; }
    public string? Text { get; set; }
    public AdSize Size { get; set; } = AdSize.Quarter;
    public int Order { get; set; }

    // Image stored in DB as binary
    public byte[]? ImageData { get; set; }
    public string? ImageContentType { get; set; }

    public Issue Issue { get; set; } = null!;
}

public enum AdSize
{
    Quarter,
    Half,
    Full
}

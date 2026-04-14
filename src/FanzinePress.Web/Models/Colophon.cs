namespace FanzinePress.Web.Models;

public class Colophon
{
    public int Id { get; set; }
    public int IssueId { get; set; }
    public string PublicationName { get; set; } = string.Empty;
    public string? Editors { get; set; }
    public string? Contributors { get; set; }
    public string? Contact { get; set; }
    public string? LicenseText { get; set; }
    public string? ExtraText { get; set; }

    public Issue Issue { get; set; } = null!;
}

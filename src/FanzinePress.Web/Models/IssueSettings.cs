namespace FanzinePress.Web.Models;

public class IssueSettings
{
    public int Id { get; set; }
    public int IssueId { get; set; }
    public string? HeaderText { get; set; }
    public string? FooterText { get; set; }
    public string? Motto { get; set; }

    public Issue Issue { get; set; } = null!;
}

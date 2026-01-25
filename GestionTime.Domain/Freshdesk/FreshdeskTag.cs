namespace GestionTime.Domain.Freshdesk;

public class FreshdeskTag
{
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = "freshdesk";
    public DateTime LastSeenAt { get; set; }
}

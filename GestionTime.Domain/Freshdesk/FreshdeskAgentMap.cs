namespace GestionTime.Domain.Freshdesk;

public class FreshdeskAgentMap
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public long AgentId { get; set; }
    public DateTime SyncedAt { get; set; }
}

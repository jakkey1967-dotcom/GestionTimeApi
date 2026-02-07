namespace GestionTime.Domain.Freshdesk;

/// <summary>DTO simplificado para listado de tickets desde /api/v2/tickets (con paginación).</summary>
public class FreshdeskTicketListDto
{
    public long Id { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string>? Tags { get; set; }
}

namespace GestionTime.Api.Contracts.Freshdesk;

/// <summary>
/// DTO mínimo para sugerencias de tickets en Desktop (desde view v_freshdesk_ticket_company_min)
/// </summary>
public class FreshdeskTicketSuggestDto
{
    /// <summary>
    /// ID del ticket en Freshdesk
    /// </summary>
    public long TicketId { get; set; }
    
    /// <summary>
    /// Nombre del cliente/compañía (puede ser null)
    /// </summary>
    public string? Customer { get; set; }
    
    /// <summary>
    /// Asunto del ticket
    /// </summary>
    public string Subject { get; set; } = string.Empty;
    
    /// <summary>
    /// Status del ticket (2=Open, 3=Pending, 4=Resolved, 5=Closed)
    /// </summary>
    public int Status { get; set; }
    
    /// <summary>
    /// ID del agente asignado (puede ser null si no está asignado)
    /// </summary>
    public long? AgentId { get; set; }
    
    /// <summary>
    /// Nombre del agente asignado (puede ser null)
    /// </summary>
    public string? AgentName { get; set; }
}

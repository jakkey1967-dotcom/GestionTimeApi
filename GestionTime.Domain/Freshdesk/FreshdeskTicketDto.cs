namespace GestionTime.Domain.Freshdesk;

public class FreshdeskTicketDto
{
    public long Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public int Status { get; set; }
    public int Priority { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string>? Tags { get; set; }
    
    // Campos de enriquecimiento (client info)
    public string? RequesterName { get; set; }
    public long? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    
    // Campos de enriquecimiento (técnico asignado)
    public long? ResponderId { get; set; }
    public string? TechnicianName { get; set; }
    public string? TechnicianEmail { get; set; }
    
    /// <summary>
    /// Nombre del cliente: CompanyName si existe, si no RequesterName
    /// </summary>
    public string ClientName => !string.IsNullOrEmpty(CompanyName) ? CompanyName : (RequesterName ?? string.Empty);
    
    public string StatusName => Status switch
    {
        2 => "Open",
        3 => "Pending",
        4 => "Resolved",
        5 => "Closed",
        _ => "Unknown"
    };
    
    public string PriorityName => Priority switch
    {
        1 => "Low",
        2 => "Medium",
        3 => "High",
        4 => "Urgent",
        _ => "Unknown"
    };
}

public class FreshdeskAgentDto
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class FreshdeskSearchResponse<T>
{
    public List<T> Results { get; set; } = new();
    public int Total { get; set; }
}

// DTOs para enriquecimiento de tickets
public class FreshdeskTicketDetailDto
{
    public long Id { get; set; }
    public long? CompanyId { get; set; }
    public long? ResponderId { get; set; }
    public FreshdeskRequesterDto? Requester { get; set; }
}

public class FreshdeskRequesterDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class FreshdeskCompanyDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class FreshdeskAgentDetailDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public FreshdeskContactDto? Contact { get; set; }
}

public class FreshdeskContactDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// DTOs para endpoint de detalles completos (tickets/{id}/details)
public class FreshdeskTicketDetailsDto
{
    public long Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public int Status { get; set; }
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string DescriptionText { get; set; } = string.Empty;
    public FreshdeskRequesterInfoDto? Requester { get; set; }
    public FreshdeskCompanyInfoDto? Company { get; set; }
    public List<FreshdeskConversationDto> Conversations { get; set; } = new();
}

public class FreshdeskRequesterInfoDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class FreshdeskCompanyInfoDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class FreshdeskConversationDto
{
    public long Id { get; set; }
    public bool Incoming { get; set; }
    public bool Private { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public List<string> ToEmails { get; set; } = new();
    public List<string> CcEmails { get; set; } = new();
    public string BodyText { get; set; } = string.Empty;
}




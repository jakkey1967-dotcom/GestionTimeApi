namespace GestionTime.Api.Contracts.Integrations;

/// <summary>Respuesta de sincronización de tags de Freshdesk.</summary>
public sealed record FreshdeskTagsSyncResponse
{
    /// <summary>Indica si la sincronización fue exitosa.</summary>
    public bool Success { get; init; }
    
    /// <summary>Mensaje descriptivo del resultado.</summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>Número de filas afectadas en el UPSERT.</summary>
    public int RowsAffected { get; init; }
    
    /// <summary>Total de tags en la tabla después del sync.</summary>
    public int TotalTags { get; init; }
    
    /// <summary>Timestamp UTC de la sincronización.</summary>
    public DateTime SyncedAt { get; init; }
}

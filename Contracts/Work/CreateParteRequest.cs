namespace GestionTime.Api.Contracts.Work;

public sealed record CreateParteRequest(
    DateTime fecha_trabajo,
    string hora_inicio,   // "HH:mm"
    string hora_fin,      // "HH:mm"
    int id_cliente,
    string? tienda,
    int? id_grupo,
    int? id_tipo,
    string accion,
    string? ticket,
    string[]? tags        // Opcional - array de tags
);



namespace GestionTime.Api.Contracts.Work;

public sealed record UpdateParteRequest(
    DateTime fecha_trabajo,
    string? hora_inicio,   // "HH:mm"
    string? hora_fin,      // "HH:mm"
    int id_cliente,
    string? tienda,
    int? id_grupo,
    int? id_tipo,
    string? accion,
    string? ticket,
    int? estado            // Opcional - 0=Abierto, 1=Pausado, 2=Cerrado, 3=Enviado, 9=Anulado
);

namespace GestionTime.Domain.Work;

public sealed class ParteDeTrabajo
{
    public long Id { get; set; }

    // Fecha del parte (lo que filtras en "día")
    public DateTime FechaTrabajo { get; set; }

    // Hora de inicio del trabajo
    public TimeOnly HoraInicio { get; set; }

    // Hora de fin del trabajo
    public TimeOnly HoraFin { get; set; }

    // Descripción de la acción/trabajo realizado
    public string Accion { get; set; } = "";

    // Número de ticket asociado (opcional)
    public string? Ticket { get; set; }

    // ID del cliente
    public int IdCliente { get; set; }

    // Nombre/código de la tienda (opcional)
    public string? Tienda { get; set; }

    // ID del grupo de trabajo (opcional)
    public int? IdGrupo { get; set; }

    // ID del tipo de trabajo (opcional)
    public int? IdTipo { get; set; }

    // ID del usuario propietario del parte
    public Guid IdUsuario { get; set; }

    // Estado del parte (int) - mapea a columna 'estado'
    // 0=Abierto, 1=Pausado, 2=Cerrado, 3=Enviado, 9=Anulado
    public int Estado { get; set; } = EstadoParte.Abierto;

    // Fecha de creación (UTC)
    public DateTime CreatedAt { get; set; }

    // Fecha de última actualización (UTC)
    public DateTime UpdatedAt { get; set; }

    // Métodos de conveniencia
    public bool EstaAbierto => Estado == EstadoParte.Abierto;
    public bool EstaPausado => Estado == EstadoParte.Pausado;
    public bool EstaCerrado => Estado == EstadoParte.Cerrado;
    public bool EstaEnviado => Estado == EstadoParte.Enviado;
    public bool EstaAnulado => Estado == EstadoParte.Anulado;
    public bool PuedeEditar => EstadoParte.PuedeEditar(Estado);
    public string EstadoNombre => EstadoParte.ObtenerNombre(Estado);
}

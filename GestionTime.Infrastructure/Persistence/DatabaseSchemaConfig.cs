namespace GestionTime.Infrastructure.Persistence;

/// <summary>
/// Configuración del schema de base de datos
/// </summary>
public class DatabaseSchemaConfig
{
    /// <summary>
    /// Nombre del schema a utilizar (ej: "gestiontime", "cliente1", etc.)
    /// </summary>
    public string Schema { get; set; } = "gestiontime";
}

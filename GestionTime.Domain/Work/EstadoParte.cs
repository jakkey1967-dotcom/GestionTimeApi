namespace GestionTime.Domain.Work;

/// <summary>
/// Estados posibles de un parte de trabajo.
/// Usar valores numéricos para consistencia y facilitar futuras integraciones.
/// </summary>
public static class EstadoParte
{
    /// <summary>
    /// Parte abierto/activo - En progreso
    /// </summary>
    public const int Abierto = 0;

    /// <summary>
    /// Parte pausado - Temporalmente detenido
    /// </summary>
    public const int Pausado = 1;

    /// <summary>
    /// Parte cerrado - Completado pero no enviado
    /// </summary>
    public const int Cerrado = 2;

    /// <summary>
    /// Parte enviado - Cerrado y enviado al sistema destino
    /// </summary>
    public const int Enviado = 3;

    /// <summary>
    /// Parte anulado - Cancelado/eliminado lógicamente
    /// </summary>
    public const int Anulado = 9;

    /// <summary>
    /// Todos los estados válidos
    /// </summary>
    public static readonly int[] TodosLosEstados = { Abierto, Pausado, Cerrado, Enviado, Anulado };

    /// <summary>
    /// Estados que permiten edición
    /// MODIFICADO: Ahora incluye Cerrado para permitir editar partes cerrados
    /// </summary>
    public static readonly int[] EstadosEditables = { Abierto, Pausado, Cerrado };

    /// <summary>
    /// Verifica si un estado es válido
    /// </summary>
    public static bool EsValido(int estado) => TodosLosEstados.Contains(estado);

    /// <summary>
    /// Verifica si el parte puede ser editado
    /// </summary>
    public static bool PuedeEditar(int estado) => EstadosEditables.Contains(estado);

    /// <summary>
    /// Verifica si el parte puede ser cerrado
    /// </summary>
    public static bool PuedeCerrar(int estado) => estado == Abierto || estado == Pausado;

    /// <summary>
    /// Verifica si el parte puede ser enviado
    /// </summary>
    public static bool PuedeEnviar(int estado) => estado == Cerrado;

    /// <summary>
    /// Verifica si el parte puede ser anulado
    /// </summary>
    public static bool PuedeAnular(int estado) => estado != Anulado && estado != Enviado;

    /// <summary>
    /// Obtiene el nombre del estado para mostrar
    /// </summary>
    public static string ObtenerNombre(int estado) => estado switch
    {
        Abierto => "Abierto",
        Pausado => "Pausado",
        Cerrado => "Cerrado",
        Enviado => "Enviado",
        Anulado => "Anulado",
        _ => "Desconocido"
    };

    /// <summary>
    /// Intenta parsear un nombre de estado a su valor numérico (para migración/compatibilidad)
    /// </summary>
    public static bool TryParse(string? nombre, out int estado)
    {
        estado = nombre?.Trim().ToLowerInvariant() switch
        {
            "abierto" or "activo" or "0" => Abierto,
            "pausado" or "1" => Pausado,
            "cerrado" or "2" => Cerrado,
            "enviado" or "3" => Enviado,
            "anulado" or "9" => Anulado,
            _ => -1
        };
        return estado >= 0;
    }
}

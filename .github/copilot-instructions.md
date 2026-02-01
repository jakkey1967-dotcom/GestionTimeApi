# Copilot Instructions

## General Guidelines
- Objetivo: reducir tamaño del archivo sin romper nada y sin reescribirlo completo.
- Límites: XAML <= 400 líneas, C# <= 700 líneas.
- Edición segura:
  - Devuelve SOLO el bloque cambiado + instrucciones exactas de reemplazo (BEGIN/END).
  - 1 archivo por respuesta.
  - Si requiere muchos cambios, divide en pasos (máx ~60–120 líneas modificadas por paso).

## Prioridades
### PRIORIDAD #1 (no romper comportamiento):
- NO cambies layout ni apariencia salvo lo imprescindible para extraer código.
- NO renombres bindings/propiedades/commands (ni x:Name si se usa).
- Cambios pequeños y compilables.

## Proceso de Reducción de Tamaño
### A) Para XAML
1) Añade marcadores GT-BEGIN/GT-END por secciones SIN cambiar layout.
2) Si hay ItemTemplate grande, muévelo a Templates/ y referencia por StaticResource.
3) Mueve estilos repetidos a Styles/ (ResourceDictionary) y reemplaza repetición por Style.
4) Si aún supera límites, extrae secciones a UserControls (Controls/) heredando DataContext.

### B) Para C# (.cs)
1) Añade marcadores GT-BEGIN/GT-END por bloques lógicos SIN cambiar comportamiento:
   INIT, LOAD, SAVE, FILTERS, VALIDATION, MAPPING, NAVIGATION, LOGGING, ERRORS.
2) Extrae funciones puras (formateo HH:mm, parsing, cálculos, utilidades) a Helpers/ o Extensions/.
3) Extrae IO/API/DB/HTTP a Services/ (el .cs queda como orquestador).
4) Si aún supera 700 líneas, divide en partial manteniendo el mismo nombre de clase:
   *.Commands.cs, *.Loading.cs, *.Validation.cs, *.Mapping.cs (si aplica).
   Nota: en code-behind (*.xaml.cs) dejar mínimo (InitializeComponent + wiring UI inevitable). Lo demás a VM/Services/Helpers.

## Estilo de Código
### Comentarios XML (C#):
**REGLA OBLIGATORIA:** Todos los comentarios XML deben estar en **una sola línea**.

✅ **FORMATO PERMITIDO:**/// <summary>Respuesta del endpoint /api/v1/users/me.</summary>
internal sealed class UserInfoResponse
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

/// <summary>Gestiona la carga de catálogos con caché compartido de 30 minutos.</summary>
public class CatalogManager
{
    /// <summary>Carga clientes desde la API y actualiza el caché local.</summary>
    public async Task LoadClientesAsync() { }
}
❌ **FORMATO PROHIBIDO:**// ❌ MAL - Multilínea innecesaria:
/// <summary>
/// Respuesta del API
/// </summary>

// ❌ MAL - Formato de 3 líneas para textos cortos:
/// <summary>
/// Carga clientes
/// </summary>

// ❌ MAL - Comentarios // para APIs públicas:
// Carga clientes desde el servidor
public async Task LoadClientesAsync() { }
**Reglas adicionales:**
- Si el texto es largo (>80 caracteres), resumir en UNA frase corta.
- Para detalles adicionales, usar `<remarks>` también en una línea:/// <summary>Valida credenciales de usuario.</summary>
/// <remarks>Usa bcrypt para hash y verifica expiración de contraseña.</remarks>- Usar punto final (`.`) en todas las descripciones.
- NO usar `//` para documentar clases, métodos o propiedades públicas.




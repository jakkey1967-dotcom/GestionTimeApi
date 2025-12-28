# Sistema de Logging - GestionTime API

## ?? Descripción General

El sistema de logging de GestionTime separa los logs en dos categorías principales:

| Archivo | Contenido | Activación |
|---------|-----------|------------|
| `gestiontime.log` | Logs informativos (Info, Warning) | Siempre activo |
| `gestiontimeerror.log` | Logs de debug y errores (Debug, Error, Critical) | Configurable por parámetros |

---

## ??? Arquitectura

```
???????????????????????????????????????????????????????????????
?                     Controllers                             ?
?  ILogger<T> inyectado automáticamente                       ?
???????????????????????????????????????????????????????????????
                              ?
                              ?
???????????????????????????????????????????????????????????????
?                   LoggingService                            ?
?  - LogInfo(message, params)                                 ?
?  - LogWarning(message, params)                              ?
?  - LogDebug(message, params)                                ?
?  - LogError(exception, message, params)                     ?
???????????????????????????????????????????????????????????????
                              ?
              ?????????????????????????????????
              ?                               ?
???????????????????????????     ???????????????????????????
?   gestiontime.log       ?     ?  gestiontimeerror.log   ?
???????????????????????????     ???????????????????????????
? • Information           ?     ? • Debug                 ?
? • Warning               ?     ? • Error                 ?
?                         ?     ? • Critical              ?
???????????????????????????     ???????????????????????????
```

---

## ?? Niveles de Log

### gestiontime.log (Informativos)

| Nivel | Uso | Ejemplo |
|-------|-----|---------|
| `Information` | Eventos normales del sistema | Login exitoso, CRUD completado |
| `Warning` | Situaciones anómalas no críticas | Intento de login fallido, token expirado |

### gestiontimeerror.log (Debug/Errores)

| Nivel | Uso | Ejemplo |
|-------|-----|---------|
| `Debug` | Información detallada para desarrollo | Parámetros de entrada, queries ejecutadas |
| `Error` | Errores recuperables | Validación fallida, recurso no encontrado |
| `Critical` | Errores críticos del sistema | Fallo de conexión BD, excepción no manejada |

---

## ?? Configuración

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    },
    "GestionTime": {
      "EnableDebugLogs": false,
      "LogDirectory": "logs",
      "MaxFileSizeMB": 10,
      "RetainedFileCountLimit": 5
    }
  }
}
```

### Parámetros de Configuración

| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `EnableDebugLogs` | bool | `false` | Activa logs de debug en `gestiontimeerror.log` |
| `LogDirectory` | string | `"logs"` | Directorio donde se guardan los archivos |
| `MaxFileSizeMB` | int | `10` | Tamaño máximo de archivo antes de rotar |
| `RetainedFileCountLimit` | int | `5` | Número de archivos históricos a mantener |

---

## ?? Activación de Debug Logs

### Opción 1: appsettings.json

```json
{
  "Logging": {
    "GestionTime": {
      "EnableDebugLogs": true
    }
  }
}
```

### Opción 2: Variable de entorno

```bash
# Windows
set GestionTime__Logging__EnableDebugLogs=true

# Linux/Mac
export GestionTime__Logging__EnableDebugLogs=true
```

### Opción 3: Línea de comandos

```bash
dotnet run --GestionTime:Logging:EnableDebugLogs=true
```

---

## ?? Estructura de Archivos de Log

```
logs/
??? gestiontime.log                    # Log actual informativo
??? gestiontime-20251215.log           # Log histórico por fecha
??? gestiontimeerror.log               # Log actual de errores
??? gestiontimeerror-20251215.log      # Log histórico de errores
```

---

## ?? Formato de Log

```
[2025-01-15 10:30:45.123 +00:00] [INF] [AuthController] Usuario admin@test.com autenticado correctamente
[2025-01-15 10:30:46.456 +00:00] [WRN] [AuthController] Intento de login fallido para email: unknown@test.com
[2025-01-15 10:30:47.789 +00:00] [ERR] [PartesController] Error al crear parte: {mensaje de error}
```

### Campos del formato

| Campo | Descripción |
|-------|-------------|
| Timestamp | Fecha/hora con timezone UTC |
| Level | INF, WRN, DBG, ERR, CRT |
| Source | Nombre del controller/servicio |
| Message | Mensaje descriptivo |

---

## ?? Uso en Código

### En Controllers

```csharp
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Login(LoginRequest req)
    {
        _logger.LogInformation("Intento de login para {Email}", req.Email);
        
        // ... lógica ...
        
        _logger.LogDebug("Token generado para usuario {UserId}", user.Id);
        
        return Ok();
    }
}
```

### Patrones Recomendados

```csharp
// ? Correcto - Usa plantillas estructuradas
_logger.LogInformation("Usuario {UserId} creó parte {ParteId}", userId, parteId);

// ? Incorrecto - Interpolación de strings
_logger.LogInformation($"Usuario {userId} creó parte {parteId}");

// ? Correcto - Incluye exception
_logger.LogError(ex, "Error al procesar parte {ParteId}", parteId);

// ? Incorrecto - Exception como string
_logger.LogError("Error: " + ex.Message);
```

---

## ?? Consulta de Logs

### Windows PowerShell

```powershell
# Ver últimas 50 líneas del log informativo
Get-Content .\logs\gestiontime.log -Tail 50

# Buscar errores específicos
Select-String -Path .\logs\gestiontimeerror.log -Pattern "ERROR"

# Monitorear en tiempo real
Get-Content .\logs\gestiontime.log -Wait
```

### Linux/Mac

```bash
# Ver últimas 50 líneas
tail -n 50 logs/gestiontime.log

# Buscar errores
grep "ERROR" logs/gestiontimeerror.log

# Monitorear en tiempo real
tail -f logs/gestiontime.log
```

---

## ?? Métricas de Log

El sistema registra automáticamente:

- **Autenticación**: Login/logout, refresh tokens, intentos fallidos
- **CRUD Partes**: Creación, actualización, anulación
- **Admin**: Gestión de usuarios, cambios de roles
- **Errores**: Excepciones, validaciones fallidas, recursos no encontrados
- **Performance**: Tiempo de respuesta de endpoints críticos

---

## ??? Seguridad

- **NO se registran**: Contraseñas, tokens completos, datos sensibles
- **SE registran hasheados**: IDs de sesión, referencias a tokens
- **Rotación automática**: Archivos se rotan por tamaño y fecha

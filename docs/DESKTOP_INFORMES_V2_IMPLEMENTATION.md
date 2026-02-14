# Implementación de Informes v2 en GestionTime Desktop

## 📋 Tabla de Contenidos

1. [Autenticación JWT](#autenticación-jwt)
2. [Endpoint: GET /api/v2/informes/partes](#endpoint-partes)
3. [Endpoint: GET /api/v2/informes/resumen](#endpoint-resumen)
4. [Implementación en C# Desktop](#implementación-en-c-desktop)
5. [Ejemplos de Uso](#ejemplos-de-uso)
6. [Manejo de Errores](#manejo-de-errores)

---

## 🔐 Autenticación JWT

### Login Desktop

```http
POST /api/v1/auth/login-desktop
Content-Type: application/json

{
  "email": "usuario@empresa.com",
  "password": "password123"
}
```

**Respuesta:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-02-15T03:10:57.000Z",
  "user": {
    "id": "b455821b-e481-4969-825d-817ee4e85184",
    "email": "usuario@empresa.com",
    "name": "Pablo Santos",
    "role": "ADMIN"
  }
}
```

### Usar Token en Requests

```http
GET /api/v2/informes/partes?date=2026-02-14&pageSize=50
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 📊 Endpoint: GET /api/v2/informes/partes

### Descripción
Obtiene listado de partes de trabajo con paginación, filtros y ordenamiento.

### URL
```
GET https://tu-api.com/api/v2/informes/partes
```

### Parámetros Query (Query String)

#### 🔴 **Filtros de Fecha (OBLIGATORIO - elegir uno)**

| Parámetro | Tipo | Descripción | Ejemplo |
|-----------|------|-------------|---------|
| `date` | string | Día específico (YYYY-MM-DD) | `2026-02-14` |
| `weekIso` | string | Semana ISO (YYYY-Www) | `2026-W07` |
| `from` + `to` | string | Rango de fechas (ambos obligatorios) | `from=2026-02-01&to=2026-02-28` |

#### 🟢 **Filtros de Agente**

| Parámetro | Tipo | Descripción | Ejemplo |
|-----------|------|-------------|---------|
| `agentId` | guid | ID de un agente específico | `b455821b-e481-4969-825d-817ee4e85184` |
| `agentIds` | string | Lista de IDs separados por coma | `id1,id2,id3` |

**⚠️ Restricciones por rol:**
- **USER**: Solo puede ver sus propios datos (agentId/agentIds ignorados)
- **EDITOR/ADMIN**: Pueden filtrar por cualquier agente o ver todos

#### 🟡 **Filtros de Catálogos (Opcionales)**

| Parámetro | Tipo | Descripción | Ejemplo |
|-----------|------|-------------|---------|
| `clientId` | int | ID del cliente | `1` |
| `groupId` | int | ID del grupo | `2` |
| `typeId` | int | ID del tipo de trabajo | `3` |

#### 🔵 **Búsqueda de Texto (Opcional)**

| Parámetro | Tipo | Descripción | Ejemplo |
|-----------|------|-------------|---------|
| `q` | string | Búsqueda en ticket, acción, tienda, cliente | `instalación` |

#### 🟣 **Paginación**

| Parámetro | Tipo | Descripción | Default | Rango |
|-----------|------|-------------|---------|-------|
| `page` | int | Número de página | `1` | ≥ 1 |
| `pageSize` | int | Elementos por página | `100` | 1-200 |

#### 🟠 **Ordenamiento (Opcional)**

| Parámetro | Tipo | Descripción | Default | Ejemplo |
|-----------|------|-------------|---------|---------|
| `sort` | string | Campos y dirección (campo:dir,campo:dir) | `fecha_trabajo:desc,hora_inicio:asc` | `duracion_min:desc` |

**Campos ordenables:**
- `fecha_trabajo` - Fecha de trabajo
- `hora_inicio` - Hora de inicio
- `hora_fin` - Hora de fin
- `duracion_min` - Duración en minutos
- `agente_nombre` - Nombre del agente
- `cliente_nombre` - Nombre del cliente

**Direcciones:**
- `asc` - Ascendente
- `desc` - Descendente

### Ejemplo de Request

```http
GET /api/v2/informes/partes?date=2026-02-14&clientId=1&q=instalación&pageSize=50&sort=duracion_min:desc
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Respuesta Exitosa (200 OK)

```json
{
  "generatedAt": "2026-02-14T15:10:57.000Z",
  "filtersApplied": {
    "date": "2026-02-14",
    "weekIso": null,
    "from": null,
    "to": null,
    "agentId": null,
    "agentIds": null,
    "clientId": 1,
    "groupId": null,
    "typeId": null,
    "q": "instalación"
  },
  "page": 1,
  "pageSize": 50,
  "total": 127,
  "items": [
    {
      "fechaTrabajo": "2026-02-14",
      "horaInicio": "08:30",
      "horaFin": "10:15",
      "duracionHoras": 1.75,
      "duracionMin": 105,
      "accion": "Instalación de router WiFi 6",
      "ticket": "TK-2026-0214-001",
      "idCliente": 1,
      "tienda": "Madrid Centro",
      "idGrupo": 2,
      "idTipo": 3,
      "idUsuario": "b455821b-e481-4969-825d-817ee4e85184",
      "estado": "APPROVED",
      "tags": ["instalación", "wifi"],
      "semanaIso": "7",
      "mes": 2,
      "anio": 2026,
      "agenteNombre": "Pablo Santos",
      "agenteEmail": "psantos@global-retail.com",
      "clienteNombre": "Global Retail SL",
      "grupoNombre": "Instalaciones",
      "tipoNombre": "WiFi"
    }
  ]
}
```

---

## 📈 Endpoint: GET /api/v2/informes/resumen

### Descripción
Obtiene estadísticas agregadas con cálculo de minutos cubiertos, solapes y gaps usando algoritmo sweep line.

### URL
```
GET https://tu-api.com/api/v2/informes/resumen
```

### Parámetros Query (Query String)

#### 🔴 **Scope (OBLIGATORIO)**

| Parámetro | Tipo | Descripción | Valores Permitidos |
|-----------|------|-------------|--------------------|
| `scope` | string | Alcance temporal del resumen | `day`, `week`, `range` |

**Combinaciones obligatorias:**
- `scope=day` + `date=YYYY-MM-DD`
- `scope=week` + `weekIso=YYYY-Www`
- `scope=range` + `from=YYYY-MM-DD` + `to=YYYY-MM-DD`

#### 🔴 **Filtros de Fecha (según scope)**

| Parámetro | Tipo | Requerido Para | Ejemplo |
|-----------|------|----------------|---------|
| `date` | string | `scope=day` | `2026-02-14` |
| `weekIso` | string | `scope=week` | `2026-W07` |
| `from` + `to` | string | `scope=range` | `from=2026-02-01&to=2026-02-28` |

#### 🟢 **Filtros de Agente (Opcional)**

| Parámetro | Tipo | Descripción | Ejemplo |
|-----------|------|-------------|---------|
| `agentId` | guid | ID de un agente específico | `b455821b-e481-4969-825d-817ee4e85184` |
| `agentIds` | string | Lista de IDs separados por coma | `id1,id2,id3` |

#### 🟡 **Filtros de Catálogos (Opcionales)**

| Parámetro | Tipo | Descripción | Ejemplo |
|-----------|------|-------------|---------|
| `clientId` | int | ID del cliente | `1` |
| `groupId` | int | ID del grupo | `2` |
| `typeId` | int | ID del tipo de trabajo | `3` |

### Ejemplo de Request

```http
GET /api/v2/informes/resumen?scope=week&weekIso=2026-W07&clientId=1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Respuesta Exitosa (200 OK)

```json
{
  "generatedAt": "2026-02-14T15:10:57.000Z",
  "filtersApplied": {
    "scope": "week",
    "date": null,
    "weekIso": "2026-W07",
    "from": null,
    "to": null,
    "agentId": null,
    "agentIds": null,
    "clientId": 1,
    "groupId": null,
    "typeId": null
  },
  "partsCount": 35,
  "recordedMinutes": 1820,
  "coveredMinutes": 1650,
  "overlapMinutes": 170,
  "mergedIntervals": [
    {
      "start": "2026-02-09T08:00:00Z",
      "end": "2026-02-09T14:30:00Z",
      "minutes": 390
    },
    {
      "start": "2026-02-10T09:00:00Z",
      "end": "2026-02-10T17:00:00Z",
      "minutes": 480
    }
  ],
  "gaps": [
    {
      "start": "2026-02-09T14:30:00Z",
      "end": "2026-02-10T09:00:00Z",
      "minutes": 1110
    }
  ],
  "firstStart": "2026-02-09T08:00:00Z",
  "lastEnd": "2026-02-15T18:30:00Z",
  "byDay": [
    {
      "date": "2026-02-09",
      "partsCount": 5,
      "recordedMinutes": 420,
      "coveredMinutes": 390,
      "overlapMinutes": 30
    },
    {
      "date": "2026-02-10",
      "partsCount": 7,
      "recordedMinutes": 540,
      "coveredMinutes": 480,
      "overlapMinutes": 60
    }
  ]
}
```

**Explicación de campos:**
- `recordedMinutes`: Suma de duración de todos los partes (puede tener solapes)
- `coveredMinutes`: Tiempo real trabajado (sin contar solapes)
- `overlapMinutes`: Minutos duplicados (solapes entre partes)
- `mergedIntervals`: Intervalos de tiempo unificados (sin solapes)
- `gaps`: Huecos entre intervalos de trabajo
- `byDay`: Resumen diario (solo para `scope=week` o `scope=range`)

---

## 💻 Implementación en C# Desktop

### 1. Clase de Cliente API

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GestionTimeDesktop.Services
{
    /// <summary>Cliente HTTP para API de GestionTime con autenticación JWT.</summary>
    public class GestionTimeApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private string? _jwtToken;

        public GestionTimeApiClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>Login y obtención de token JWT.</summary>
        public async Task<LoginResponse> LoginAsync(string email, string password)
        {
            var request = new { email, password };
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("/api/v1/auth/login-desktop", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<LoginResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _jwtToken = loginResponse.Token;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);

            return loginResponse;
        }

        /// <summary>Obtiene listado de partes con filtros.</summary>
        public async Task<PartesResponse> GetPartesAsync(PartesQuery query)
        {
            var queryString = BuildQueryString(query);
            var response = await _httpClient.GetAsync($"/api/v2/informes/partes?{queryString}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PartesResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        /// <summary>Obtiene resumen estadístico.</summary>
        public async Task<ResumenResponse> GetResumenAsync(ResumenQuery query)
        {
            var queryString = BuildQueryString(query);
            var response = await _httpClient.GetAsync($"/api/v2/informes/resumen?{queryString}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ResumenResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        private string BuildQueryString(object query)
        {
            var properties = query.GetType().GetProperties();
            var queryParams = new System.Collections.Generic.List<string>();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(query);
                if (value != null)
                {
                    var paramName = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
                    queryParams.Add($"{paramName}={Uri.EscapeDataString(value.ToString())}");
                }
            }

            return string.Join("&", queryParams);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
```

### 2. Modelos de Datos

```csharp
using System;
using System.Collections.Generic;

namespace GestionTimeDesktop.Models
{
    // === LOGIN ===
    public class LoginResponse
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public UserInfo User { get; set; }
    }

    public class UserInfo
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
    }

    // === PARTES QUERY ===
    public class PartesQuery
    {
        // Filtros de fecha (elegir uno)
        public string Date { get; set; }
        public string WeekIso { get; set; }
        public string From { get; set; }
        public string To { get; set; }

        // Filtros de agente
        public Guid? AgentId { get; set; }
        public string AgentIds { get; set; }

        // Filtros de catálogos
        public int? ClientId { get; set; }
        public int? GroupId { get; set; }
        public int? TypeId { get; set; }

        // Búsqueda
        public string Q { get; set; }

        // Paginación
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 100;

        // Ordenamiento
        public string Sort { get; set; } = "fecha_trabajo:desc,hora_inicio:asc";
    }

    // === PARTES RESPONSE ===
    public class PartesResponse
    {
        public DateTime GeneratedAt { get; set; }
        public object FiltersApplied { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<ParteItem> Items { get; set; }
    }

    public class ParteItem
    {
        public DateTime FechaTrabajo { get; set; }
        public string HoraInicio { get; set; }
        public string HoraFin { get; set; }
        public decimal? DuracionHoras { get; set; }
        public int? DuracionMin { get; set; }
        public string Accion { get; set; }
        public string Ticket { get; set; }
        public int? IdCliente { get; set; }
        public string Tienda { get; set; }
        public int? IdGrupo { get; set; }
        public int? IdTipo { get; set; }
        public Guid IdUsuario { get; set; }
        public string Estado { get; set; }
        public List<string> Tags { get; set; }
        public string SemanaIso { get; set; }
        public int? Mes { get; set; }
        public int? Anio { get; set; }
        public string AgenteNombre { get; set; }
        public string AgenteEmail { get; set; }
        public string ClienteNombre { get; set; }
        public string GrupoNombre { get; set; }
        public string TipoNombre { get; set; }
    }

    // === RESUMEN QUERY ===
    public class ResumenQuery
    {
        // Scope obligatorio
        public string Scope { get; set; } // "day", "week", "range"

        // Filtros de fecha (según scope)
        public string Date { get; set; }
        public string WeekIso { get; set; }
        public string From { get; set; }
        public string To { get; set; }

        // Filtros de agente
        public Guid? AgentId { get; set; }
        public string AgentIds { get; set; }

        // Filtros de catálogos
        public int? ClientId { get; set; }
        public int? GroupId { get; set; }
        public int? TypeId { get; set; }
    }

    // === RESUMEN RESPONSE ===
    public class ResumenResponse
    {
        public DateTime GeneratedAt { get; set; }
        public object FiltersApplied { get; set; }
        public int PartsCount { get; set; }
        public int RecordedMinutes { get; set; }
        public int CoveredMinutes { get; set; }
        public int OverlapMinutes { get; set; }
        public List<MergedInterval> MergedIntervals { get; set; }
        public List<Gap> Gaps { get; set; }
        public DateTime? FirstStart { get; set; }
        public DateTime? LastEnd { get; set; }
        public List<DailySummary> ByDay { get; set; }
    }

    public class MergedInterval
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public int Minutes { get; set; }
    }

    public class Gap
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public int Minutes { get; set; }
    }

    public class DailySummary
    {
        public DateTime Date { get; set; }
        public int PartsCount { get; set; }
        public int RecordedMinutes { get; set; }
        public int CoveredMinutes { get; set; }
        public int OverlapMinutes { get; set; }
    }
}
```

---

## 🎯 Ejemplos de Uso

### Ejemplo 1: Login y Obtener Partes de Hoy

```csharp
using var apiClient = new GestionTimeApiClient("https://tu-api.com");

// 1. Login
var loginResponse = await apiClient.LoginAsync("usuario@empresa.com", "password123");
Console.WriteLine($"Login OK: {loginResponse.User.Name} ({loginResponse.User.Role})");

// 2. Obtener partes de hoy
var partesQuery = new PartesQuery
{
    Date = DateTime.Today.ToString("yyyy-MM-dd"),
    PageSize = 50,
    Sort = "hora_inicio:asc"
};

var partesResponse = await apiClient.GetPartesAsync(partesQuery);
Console.WriteLine($"Total partes hoy: {partesResponse.Total}");

foreach (var parte in partesResponse.Items)
{
    Console.WriteLine($"[{parte.HoraInicio}-{parte.HoraFin}] {parte.Accion} ({parte.DuracionMin} min)");
}
```

### Ejemplo 2: Resumen Semanal por Cliente

```csharp
using var apiClient = new GestionTimeApiClient("https://tu-api.com");
await apiClient.LoginAsync("usuario@empresa.com", "password123");

// Obtener resumen de la semana actual para cliente específico
var resumenQuery = new ResumenQuery
{
    Scope = "week",
    WeekIso = GetCurrentIsoWeek(), // "2026-W07"
    ClientId = 1
};

var resumenResponse = await apiClient.GetResumenAsync(resumenQuery);

Console.WriteLine($"Partes: {resumenResponse.PartsCount}");
Console.WriteLine($"Minutos registrados: {resumenResponse.RecordedMinutes}");
Console.WriteLine($"Minutos cubiertos: {resumenResponse.CoveredMinutes}");
Console.WriteLine($"Solapes: {resumenResponse.OverlapMinutes}");

// Resumen diario
foreach (var day in resumenResponse.ByDay)
{
    Console.WriteLine($"{day.Date:dd/MM}: {day.PartsCount} partes, {day.CoveredMinutes} min");
}
```

### Ejemplo 3: Búsqueda con Paginación

```csharp
using var apiClient = new GestionTimeApiClient("https://tu-api.com");
await apiClient.LoginAsync("usuario@empresa.com", "password123");

// Buscar "instalación" en último mes, paginado
var query = new PartesQuery
{
    From = DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd"),
    To = DateTime.Today.ToString("yyyy-MM-dd"),
    Q = "instalación",
    PageSize = 20,
    Page = 1,
    Sort = "fecha_trabajo:desc"
};

var response = await apiClient.GetPartesAsync(query);

Console.WriteLine($"Resultados: {response.Items.Count} de {response.Total}");
Console.WriteLine($"Páginas: {Math.Ceiling((double)response.Total / response.PageSize)}");

// Obtener siguiente página
query.Page = 2;
var response2 = await apiClient.GetPartesAsync(query);
```

### Ejemplo 4: Filtros Múltiples

```csharp
using var apiClient = new GestionTimeApiClient("https://tu-api.com");
await apiClient.LoginAsync("admin@empresa.com", "password123");

// Obtener partes de múltiples agentes, cliente específico, último mes
var query = new PartesQuery
{
    From = "2026-02-01",
    To = "2026-02-28",
    AgentIds = "b455821b-e481-4969-825d-817ee4e85184,c566932c-f592-5a7a-936e-928ff5f96295",
    ClientId = 1,
    GroupId = 2,
    PageSize = 100,
    Sort = "duracion_min:desc"
};

var response = await apiClient.GetPartesAsync(query);
Console.WriteLine($"Partes filtrados: {response.Total}");
```

### Ejemplo 5: Resumen Mensual con Gaps

```csharp
using var apiClient = new GestionTimeApiClient("https://tu-api.com");
await apiClient.LoginAsync("usuario@empresa.com", "password123");

var query = new ResumenQuery
{
    Scope = "range",
    From = "2026-02-01",
    To = "2026-02-28"
};

var response = await apiClient.GetResumenAsync(query);

Console.WriteLine($"=== RESUMEN FEBRERO 2026 ===");
Console.WriteLine($"Partes: {response.PartsCount}");
Console.WriteLine($"Horas trabajadas: {response.CoveredMinutes / 60.0:F2}h");
Console.WriteLine($"Horas solapadas: {response.OverlapMinutes / 60.0:F2}h");

// Mostrar huecos (gaps)
Console.WriteLine($"\n=== HUECOS DETECTADOS ({response.Gaps.Count}) ===");
foreach (var gap in response.Gaps)
{
    if (gap.Minutes > 60) // Solo mostrar gaps > 1 hora
    {
        Console.WriteLine($"{gap.Start:dd/MM HH:mm} → {gap.End:dd/MM HH:mm} ({gap.Minutes / 60.0:F1}h)");
    }
}
```

---

## ⚠️ Manejo de Errores

### Códigos de Error HTTP

| Código | Descripción | Acción Recomendada |
|--------|-------------|-------------------|
| 400 | Parámetros inválidos | Verificar filtros de fecha y formato |
| 401 | No autenticado | Hacer login nuevamente |
| 403 | Sin permisos | Usuario USER intenta ver datos de otros |
| 500 | Error interno del servidor | Contactar soporte |

### Implementación de Manejo de Errores

```csharp
public async Task<PartesResponse> GetPartesConManejo(PartesQuery query)
{
    try
    {
        return await apiClient.GetPartesAsync(query);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
    {
        // Error 400: Parámetros inválidos
        MessageBox.Show(
            "Filtros inválidos. Verifique las fechas y parámetros.",
            "Error de Validación",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning
        );
        return null;
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
    {
        // Error 401: Token expirado
        MessageBox.Show(
            "Sesión expirada. Por favor, inicie sesión nuevamente.",
            "Sesión Expirada",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning
        );
        // Redirigir a login
        return null;
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
        // Error 403: Sin permisos
        MessageBox.Show(
            "No tiene permisos para acceder a estos datos.",
            "Acceso Denegado",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        );
        return null;
    }
    catch (TaskCanceledException)
    {
        // Timeout
        MessageBox.Show(
            "La solicitud tardó demasiado tiempo. Intente nuevamente.",
            "Timeout",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning
        );
        return null;
    }
    catch (Exception ex)
    {
        // Error general
        MessageBox.Show(
            $"Error al obtener datos: {ex.Message}",
            "Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        );
        return null;
    }
}
```

### Validación de Respuestas de Error

```csharp
public async Task<PartesResponse> GetPartesSeguro(PartesQuery query)
{
    var response = await _httpClient.GetAsync($"/api/v2/informes/partes?{BuildQueryString(query)}");
    
    if (!response.IsSuccessStatusCode)
    {
        var errorJson = await response.Content.ReadAsStringAsync();
        var error = JsonSerializer.Deserialize<ErrorResponse>(errorJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        throw new ApiException(
            (int)response.StatusCode,
            error?.Error ?? "Error desconocido"
        );
    }

    var json = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<PartesResponse>(json, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });
}

public class ErrorResponse
{
    public string Error { get; set; }
}

public class ApiException : Exception
{
    public int StatusCode { get; }
    
    public ApiException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
```

---

## 📌 Notas Importantes

### Seguridad
- ✅ **Siempre usar HTTPS** en producción
- ✅ **Guardar token JWT de forma segura** (CredentialManager en Windows)
- ✅ **Renovar token antes de expiración** (12 horas)
- ❌ **NUNCA** guardar contraseñas en texto plano

### Performance
- ✅ **Usar paginación** (máx 200 items por página)
- ✅ **Aplicar filtros** para reducir resultados
- ✅ **Cachear respuestas** cuando sea posible
- ✅ **Timeout de 30 segundos** para requests largos

### Validación
- ✅ **Validar fechas** antes de enviar (formato YYYY-MM-DD)
- ✅ **Validar semana ISO** (1-53)
- ✅ **Verificar rol del usuario** para agentIds
- ✅ **Manejar respuestas vacías** (total=0)

---

## 🔗 Referencias

- [API Informes v2 - Documentación Completa](./API_INFORMES_V2.md)
- [Testing Informes v2](./TESTING_INFORMES_V2.md)
- [Troubleshooting](./TROUBLESHOOTING-EMOJI.md)

---

**Versión:** 1.0  
**Fecha:** 14 febrero 2026  
**Autor:** GestionTime Team

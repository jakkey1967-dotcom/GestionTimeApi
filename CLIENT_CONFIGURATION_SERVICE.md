# 🎯 Servicio Centralizado de Configuración de Clientes

## 📋 Descripción

El `ClientConfigurationService` es un **servicio centralizado** que gestiona toda la configuración multi-tenant de la aplicación, leyendo desde `clients.config.json` y proporcionando acceso unificado a todas las propiedades del cliente.

---

## ✅ **Ventajas de la Centralización**

### **ANTES (Descentralizado):**
```csharp
// ❌ Código repetido en múltiples lugares
var clientId = Environment.GetEnvironmentVariable("DB_SCHEMA") 
               ?? builder.Configuration["Database:Schema"] 
               ?? "pss_dvnx";

// ❌ Leer clients.config.json manualmente cada vez
var configPath = Path.Combine(Directory.GetCurrentDirectory(), "clients.config.json");
var json = await File.ReadAllTextAsync(configPath);
var config = JsonSerializer.Deserialize<ClientConfig>(json);

// ❌ Buscar cliente manualmente
var client = config.Clients.FirstOrDefault(c => c.Id == clientId);
```

### **AHORA (Centralizado):**
```csharp
// ✅ Una sola línea, siempre consistente
var clientName = _clientConfig.GetClientName();
var clientId = _clientConfig.GetClientId();
var logoPath = _clientConfig.GetLogoPath();
```

---

## 📚 **API del Servicio**

### **Métodos Principales:**

```csharp
// Obtener ID del cliente (schema)
string GetClientId()
// → "pss_dvnx"

// Obtener configuración completa del cliente actual
ClientConfig GetCurrentClient()
// → { Id: "pss_dvnx", Name: "GestionTime Global-retail.com", ... }

// Obtener todos los clientes configurados
List<ClientConfig> GetAllClients()
// → [{ Id: "pss_dvnx", ... }, { Id: "cliente_abc", ... }]

// Obtener schema de base de datos
string GetDatabaseSchema()
// → "pss_dvnx"

// Obtener nombre descriptivo
string GetClientName()
// → "GestionTime Global-retail.com"

// Obtener URL de la API
string GetApiUrl()
// → "https://gestiontimeapi.onrender.com"

// Obtener nombre del logo
string GetLogoFileName()
// → "pss_dvnx_logo.png"

// Obtener ruta completa del logo
string GetLogoPath()
// → "/images/pss_dvnx_logo.png"

// Obtener directorio wwwroot del cliente
string GetClientWwwrootPath()
// → "C:\GestionTime\GestionTimeApi\wwwroot-pss_dvnx"

// Verificar si existe wwwroot específico
bool HasClientSpecificWwwroot()
// → true / false

// Obtener cliente por ID
ClientConfig? GetClientById(string clientId)
// → { Id: "cliente_abc", Name: "Cliente ABC", ... }

// Verificar si existe un cliente
bool ClientExists(string clientId)
// → true / false
```

---

## 🎯 **Uso en Program.cs**

### **Registrar Servicio:**
```csharp
// Singleton - se carga una vez y se reutiliza
builder.Services.AddSingleton<ClientConfigurationService>();
```

### **Configurar DbContext:**
```csharp
var tempClientConfig = new ClientConfigurationService(builder.Configuration, builder.Environment);
var dbSchema = tempClientConfig.GetDatabaseSchema();

builder.Services.AddSingleton(new DatabaseSchemaConfig { Schema = dbSchema });
```

### **Endpoint Health Check:**
```csharp
app.MapGet("/health", async (GestionTimeDbContext db, ClientConfigurationService clientConfig) =>
{
    var currentClient = clientConfig.GetCurrentClient();
    
    return Results.Ok(new
    {
        client = currentClient.Name,        // "GestionTime Global-retail.com"
        clientId = currentClient.Id,        // "pss_dvnx"
        schema = clientConfig.GetDatabaseSchema()  // "pss_dvnx"
    });
});
```

### **Endpoint Raíz:**
```csharp
app.MapGet("/", async (ClientConfigurationService clientConfig) =>
{
    var logoPath = clientConfig.GetLogoPath();
    var html = $@"<img src=""{logoPath}"" />";
    return Results.Content(html, "text/html");
});
```

### **Archivos Estáticos:**
```csharp
var clientConfigService = app.Services.GetRequiredService<ClientConfigurationService>();

if (clientConfigService.HasClientSpecificWwwroot())
{
    var clientWwwroot = clientConfigService.GetClientWwwrootPath();
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(clientWwwroot),
        RequestPath = ""
    });
}
else
{
    app.UseStaticFiles();
}
```

---

## 🎯 **Uso en Controllers**

### **Inyección de Dependencia:**
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class ClientInfoController : ControllerBase
{
    private readonly ClientConfigurationService _clientConfig;

    public ClientInfoController(ClientConfigurationService clientConfig)
    {
        _clientConfig = clientConfig;
    }

    [HttpGet("current")]
    public IActionResult GetCurrentClient()
    {
        var client = _clientConfig.GetCurrentClient();
        
        return Ok(new
        {
            id = client.Id,
            name = client.Name,
            apiUrl = client.ApiUrl,
            logo = _clientConfig.GetLogoPath()
        });
    }

    [HttpGet("all")]
    public IActionResult GetAllClients()
    {
        var clients = _clientConfig.GetAllClients();
        return Ok(clients);
    }
}
```

---

## 🎯 **Uso en Services**

```csharp
public class ReportService
{
    private readonly ClientConfigurationService _clientConfig;
    private readonly GestionTimeDbContext _db;

    public ReportService(
        ClientConfigurationService clientConfig,
        GestionTimeDbContext db)
    {
        _clientConfig = clientConfig;
        _db = db;
    }

    public async Task<Report> GenerateReport()
    {
        var clientName = _clientConfig.GetClientName();
        var schema = _clientConfig.GetDatabaseSchema();

        return new Report
        {
            ClientName = clientName,
            Schema = schema,
            GeneratedAt = DateTime.UtcNow
        };
    }
}
```

---

## 📝 **Modelo de Configuración**

```csharp
public class ClientConfig
{
    public string Id { get; set; }       // "pss_dvnx"
    public string Name { get; set; }     // "GestionTime Global-retail.com"
    public string ApiUrl { get; set; }   // "https://gestiontimeapi.onrender.com"
    public string Logo { get; set; }     // "pss_dvnx_logo.png"
    public List<ClientConfig> Clients { get; set; }  // Para deserializar array
}
```

---

## 🔍 **Fuentes de Configuración (Prioridad)**

El servicio busca la configuración del cliente en este orden:

1. **Variable de entorno `DB_SCHEMA`** (Render, Docker)
2. **`appsettings.json`** → `Database:Schema`
3. **Valor por defecto:** `"pss_dvnx"`

```csharp
public string GetClientId()
{
    return Environment.GetEnvironmentVariable("DB_SCHEMA")   // 1. Primero ENV
           ?? _configuration["Database:Schema"]              // 2. Luego Config
           ?? "pss_dvnx";                                    // 3. Por defecto
}
```

---

## 🎨 **Ejemplo Completo: Multi-Tenant**

### **Cliente 1: PSS DVNX**
```
Render Service: gestiontimeapi
Variable ENV: DB_SCHEMA=pss_dvnx

Servicio retorna:
- GetClientId() → "pss_dvnx"
- GetClientName() → "GestionTime Global-retail.com"
- GetLogoPath() → "/images/pss_dvnx_logo.png"
- GetDatabaseSchema() → "pss_dvnx"
```

### **Cliente 2: Cliente ABC**
```
Render Service: gestiontimeapi-abc
Variable ENV: DB_SCHEMA=cliente_abc

Servicio retorna:
- GetClientId() → "cliente_abc"
- GetClientName() → "Cliente ABC"
- GetLogoPath() → "/images/cliente_abc_logo.png"
- GetDatabaseSchema() → "cliente_abc"
```

---

## ✅ **Beneficios**

1. ✅ **Una sola fuente de verdad** - `clients.config.json`
2. ✅ **Código DRY** - No repetir lógica de lectura de configuración
3. ✅ **Consistencia** - Mismo comportamiento en toda la app
4. ✅ **Caché automático** - Lee el archivo una sola vez
5. ✅ **Thread-safe** - Usa lock para acceso concurrente
6. ✅ **Fácil testing** - Servicio inyectable y mockeable
7. ✅ **IntelliSense** - Tipado fuerte con autocompletado

---

## 🧪 **Testing**

```csharp
[Fact]
public void GetClientId_ReturnsCorrectId()
{
    // Arrange
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            { "Database:Schema", "test_client" }
        })
        .Build();
    
    var service = new ClientConfigurationService(config, Mock.Of<IWebHostEnvironment>());

    // Act
    var clientId = service.GetClientId();

    // Assert
    Assert.Equal("test_client", clientId);
}
```

---

## 📚 **Archivos Relacionados**

- **Servicio:** `Services/ClientConfigurationService.cs`
- **Configuración:** `clients.config.json`
- **Uso:** `Program.cs`, Controllers, Services
- **Documentación:** Este archivo

---

## 🚀 **Migración desde Código Descentralizado**

### **Paso 1: Buscar Patrones a Reemplazar**

Busca en tu código:
```csharp
Environment.GetEnvironmentVariable("DB_SCHEMA")
builder.Configuration["Database:Schema"]
Path.Combine(Directory.GetCurrentDirectory(), "clients.config.json")
```

### **Paso 2: Reemplazar con Servicio**

```csharp
// ANTES
var schema = Environment.GetEnvironmentVariable("DB_SCHEMA") ?? "default";

// DESPUÉS
var schema = _clientConfig.GetDatabaseSchema();
```

---

## 📋 **Checklist de Integración**

- [x] Servicio creado: `ClientConfigurationService.cs`
- [x] Registrado en `Program.cs` como Singleton
- [x] `Program.cs` usa servicio para schema de BD
- [x] Endpoint `/health` usa servicio
- [x] Endpoint `/` usa servicio para logo
- [x] Archivos estáticos usan servicio
- [ ] Controllers actualizados (opcional)
- [ ] Services actualizados (opcional)

---

**¡Toda la configuración de clientes ahora está centralizada en un solo lugar!** 🎉

# 🚀 Guía Completa de Configuración para Deploy Multi-Tenant

## 📋 **Resumen**

Esta guía documenta **TODAS las configuraciones necesarias** para desplegar un cliente en el sistema multi-tenant de GestionTime.

---

## 🎯 **Archivo Principal: `clients.config.json`**

Este archivo centraliza **TODA** la configuración por cliente. Cada cliente tiene:

### **1. Identificación Básica**
```json
{
  "Id": "pss_dvnx",           // ✅ ID único del cliente (usado en DB_SCHEMA)
  "Name": "GestionTime Global-retail.com",  // ✅ Nombre descriptivo
  "ApiUrl": "https://gestiontimeapi.onrender.com",  // ✅ URL de la API
  "Logo": "pss_dvnx_logo.png"  // ✅ Archivo de logo
}
```

### **2. Configuración de Base de Datos**
```json
{
  "Database": {
    "Schema": "pss_dvnx",           // ✅ Schema PostgreSQL
    "ConnectionString": "${DATABASE_URL}"  // ✅ Connection string (usa variable ENV)
  }
}
```

### **3. Configuración JWT (Autenticación)**
```json
{
  "Jwt": {
    "Issuer": "GestionTime",      // ✅ Emisor del token
    "Audience": "GestionTime.Api", // ✅ Audiencia del token
    "Key": "${JWT_SECRET_KEY}",    // ✅ Clave secreta (variable ENV)
    "AccessMinutes": 15,           // ✅ Duración del access token
    "RefreshDays": 14              // ✅ Duración del refresh token
  }
}
```

### **4. Configuración CORS**
```json
{
  "Cors": {
    "Origins": [
      "https://gestiontime-pss-dvnx.app",  // ✅ Frontend del cliente
      "https://localhost:5173",            // ✅ Desarrollo local
      "http://localhost:5173"
    ]
  }
}
```

### **5. Configuración de Email (SMTP)**
```json
{
  "Email": {
    "SmtpHost": "${SMTP_HOST}",          // ✅ Servidor SMTP (variable ENV)
    "SmtpPort": 587,                     // ✅ Puerto SMTP
    "SmtpUser": "${SMTP_USER}",          // ✅ Usuario SMTP (variable ENV)
    "SmtpPassword": "${SMTP_PASSWORD}",  // ✅ Contraseña SMTP (variable ENV)
    "From": "noreply@global-retail.com", // ✅ Email remitente
    "FromName": "GestionTime Global-retail.com",  // ✅ Nombre remitente
    "EnableSsl": true                    // ✅ Usar SSL/TLS
  }
}
```

### **6. Características del Cliente**
```json
{
  "Features": {
    "RequireEmailConfirmation": true,  // ✅ Requiere verificar email
    "AllowSelfRegistration": false,    // ✅ Permite auto-registro
    "PasswordExpirationDays": 90,      // ✅ Días para expiración de contraseña
    "MaxLoginAttempts": 5,             // ✅ Intentos de login antes de bloqueo
    "LockoutMinutes": 30               // ✅ Minutos de bloqueo tras intentos fallidos
  }
}
```

### **7. Branding Personalizado**
```json
{
  "Branding": {
    "CompanyName": "GestionTime Global-retail.com",  // ✅ Nombre de la empresa
    "PrimaryColor": "#0B8C99",       // ✅ Color primario
    "SecondaryColor": "#0A7A85",     // ✅ Color secundario
    "LogoDark": "pss_dvnx_logo.png", // ✅ Logo para tema oscuro
    "LogoLight": "pss_dvnx_logo_light.png",  // ✅ Logo para tema claro
    "Favicon": "favicon-pss-dvnx.ico"  // ✅ Favicon
  }
}
```

### **8. Información de Contacto**
```json
{
  "ContactInfo": {
    "SupportEmail": "soporte@global-retail.com",  // ✅ Email de soporte
    "SupportPhone": "+34 900 123 456",            // ✅ Teléfono de soporte
    "Address": "Calle Principal 123, Madrid, España"  // ✅ Dirección física
  }
}
```

### **9. Límites del Tenant**
```json
{
  "Limits": {
    "MaxUsersPerTenant": 100,          // ✅ Máximo de usuarios
    "MaxStorageGB": 50,                // ✅ Máximo almacenamiento
    "MaxApiRequestsPerMinute": 1000    // ✅ Rate limiting
  }
}
```

---

## 🔧 **Variables de Entorno en Render**

Las siguientes variables **DEBEN** configurarse en Render Dashboard → Environment:

### **Variables Obligatorias:**
```sh
# Base de datos
DATABASE_URL=postgresql://user:password@host:5432/dbname

# Schema del cliente
DB_SCHEMA=pss_dvnx

# JWT
JWT_SECRET_KEY=tu-clave-secreta-256-bits-minimo-aleatoria

# SMTP
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=noreply@global-retail.com
SMTP_PASSWORD=tu-app-password-gmail

# Entorno
ASPNETCORE_ENVIRONMENT=Production

# Puerto (automático en Render)
PORT=10000
```

### **Cómo Generar JWT_SECRET_KEY Segura:**
```powershell
# PowerShell
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object {[char]$_})
```

---

## 📂 **Estructura de Archivos por Cliente**

Cada cliente puede tener sus propios assets:

```
GestionTimeApi/
├── clients.config.json          # ✅ Configuración centralizada
│
├── wwwroot/                     # Assets comunes (fallback)
│   ├── images/
│   │   └── logo-default.png
│   └── favicon.ico
│
├── wwwroot-pss_dvnx/           # Assets específicos PSS DVNX
│   ├── images/
│   │   ├── pss_dvnx_logo.png
│   │   └── pss_dvnx_logo_light.png
│   └── favicon-pss-dvnx.ico
│
├── wwwroot-cliente_abc/        # Assets específicos Cliente ABC
│   ├── images/
│   │   ├── cliente_abc_logo.png
│   │   └── cliente_abc_logo_light.png
│   └── favicon-abc.ico
│
└── wwwroot-cliente_xyz/        # Assets específicos Cliente XYZ
    ├── images/
    │   ├── cliente_xyz_logo.png
    │   └── cliente_xyz_logo_light.png
    └── favicon-xyz.ico
```

---

## 🎯 **Acceso a la Configuración en Código**

### **En Program.cs:**
```csharp
// Obtener cliente actual
var clientConfig = app.Services.GetRequiredService<ClientConfigurationService>();
var client = clientConfig.GetCurrentClient();

// Configurar JWT
var jwtConfig = clientConfig.GetJwtConfig();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtConfig.Issuer,
            ValidAudience = jwtConfig.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Key))
        };
    });

// Configurar CORS
var corsOrigins = clientConfig.GetCorsOrigins();
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("WebClient", p => p.WithOrigins(corsOrigins));
});

// Configurar Email
var emailConfig = clientConfig.GetEmailConfig();
// Usar emailConfig para configurar SMTP
```

### **En Controllers:**
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ClientConfigurationService _clientConfig;

    public SettingsController(ClientConfigurationService clientConfig)
    {
        _clientConfig = clientConfig;
    }

    [HttpGet("branding")]
    public IActionResult GetBranding()
    {
        return Ok(new
        {
            companyName = _clientConfig.GetCompanyName(),
            primaryColor = _clientConfig.GetPrimaryColor(),
            secondaryColor = _clientConfig.GetSecondaryColor(),
            logo = _clientConfig.GetLogoPath()
        });
    }

    [HttpGet("contact")]
    public IActionResult GetContactInfo()
    {
        var contact = _clientConfig.GetContactInfo();
        return Ok(contact);
    }

    [HttpGet("limits")]
    public IActionResult GetLimits()
    {
        return Ok(new
        {
            maxUsers = _clientConfig.GetMaxUsersAllowed(),
            maxStorageGB = _clientConfig.GetMaxStorageGB(),
            maxApiRequests = _clientConfig.GetMaxApiRequestsPerMinute()
        });
    }
}
```

---

## 📋 **Checklist de Deploy de Nuevo Cliente**

### **1. Configuración en `clients.config.json`**
- [ ] Agregar entrada en el array `Clients`
- [ ] Configurar `Id`, `Name`, `ApiUrl`, `Logo`
- [ ] Configurar `Database.Schema`
- [ ] Configurar `Jwt` (Issuer, Audience, AccessMinutes, RefreshDays)
- [ ] Configurar `Cors.Origins` (frontend del cliente)
- [ ] Configurar `Email` (From, FromName)
- [ ] Configurar `Features` (confirmación email, auto-registro, etc.)
- [ ] Configurar `Branding` (colores, logos)
- [ ] Configurar `ContactInfo` (soporte)
- [ ] Configurar `Limits` (usuarios, storage, API requests)

### **2. Variables de Entorno en Render**
- [ ] Crear nuevo Web Service en Render
- [ ] Configurar `DB_SCHEMA=nuevo_cliente`
- [ ] Configurar `JWT_SECRET_KEY` (única por cliente)
- [ ] Configurar `DATABASE_URL` (misma BD, diferente schema)
- [ ] Configurar `SMTP_*` (si es diferente por cliente)
- [ ] Configurar `ASPNETCORE_ENVIRONMENT=Production`

### **3. Base de Datos PostgreSQL**
- [ ] Crear schema: `CREATE SCHEMA nuevo_cliente;`
- [ ] Verificar: `SELECT schema_name FROM information_schema.schemata WHERE schema_name = 'nuevo_cliente';`
- [ ] Las migraciones se aplicarán automáticamente en el primer arranque

### **4. Assets y Branding**
- [ ] Crear carpeta `wwwroot-nuevo_cliente/images/`
- [ ] Agregar logo del cliente
- [ ] Agregar favicon personalizado (opcional)
- [ ] Agregar CSS personalizado (opcional)

### **5. Commit y Deploy**
- [ ] `git add clients.config.json wwwroot-nuevo_cliente/`
- [ ] `git commit -m "feat: Add configuration for nuevo_cliente"`
- [ ] `git push origin main`
- [ ] Esperar deploy automático en Render (~5 min)

### **6. Verificación Post-Deploy**
- [ ] `curl https://gestiontimeapi-nuevo-cliente.onrender.com/health`
- [ ] Verificar: `"client": "Nuevo Cliente"`
- [ ] Verificar: `"schema": "nuevo_cliente"`
- [ ] Verificar: `"database": "connected"`
- [ ] Probar login en frontend
- [ ] Verificar colores de branding
- [ ] Verificar logo se carga correctamente

---

## 🎨 **Ejemplo Completo: Agregar "Cliente Demo"**

### **1. Editar `clients.config.json`:**
```json
{
  "Id": "cliente_demo",
  "Name": "Cliente Demo S.A.",
  "ApiUrl": "https://gestiontimeapi-demo.onrender.com",
  "Logo": "cliente_demo_logo.png",
  
  "Database": {
    "Schema": "cliente_demo",
    "ConnectionString": "${DATABASE_URL}"
  },
  
  "Jwt": {
    "Issuer": "GestionTime",
    "Audience": "GestionTime.Api",
    "Key": "${JWT_SECRET_KEY}",
    "AccessMinutes": 30,
    "RefreshDays": 7
  },
  
  "Cors": {
    "Origins": [
      "https://demo.gestiontime.app",
      "https://localhost:5173"
    ]
  },
  
  "Email": {
    "SmtpHost": "${SMTP_HOST}",
    "SmtpPort": 587,
    "SmtpUser": "${SMTP_USER}",
    "SmtpPassword": "${SMTP_PASSWORD}",
    "From": "demo@gestiontime.app",
    "FromName": "Cliente Demo - GestionTime",
    "EnableSsl": true
  },
  
  "Features": {
    "RequireEmailConfirmation": false,
    "AllowSelfRegistration": true,
    "PasswordExpirationDays": 999,
    "MaxLoginAttempts": 999,
    "LockoutMinutes": 1
  },
  
  "Branding": {
    "CompanyName": "Cliente Demo S.A.",
    "PrimaryColor": "#6366F1",
    "SecondaryColor": "#4F46E5",
    "LogoDark": "cliente_demo_logo.png",
    "LogoLight": "cliente_demo_logo_light.png",
    "Favicon": "favicon-demo.ico"
  },
  
  "ContactInfo": {
    "SupportEmail": "soporte@demo.gestiontime.app",
    "SupportPhone": "+34 900 000 000",
    "Address": "Demo Address 123"
  },
  
  "Limits": {
    "MaxUsersPerTenant": 10,
    "MaxStorageGB": 5,
    "MaxApiRequestsPerMinute": 100
  }
}
```

### **2. Configurar Render:**
```sh
# Crear Web Service: gestiontimeapi-demo

# Variables de entorno:
DB_SCHEMA=cliente_demo
JWT_SECRET_KEY=demo-secret-key-super-secure-256-bits
DATABASE_URL=postgresql://user:pass@host:5432/pss_dvnx
ASPNETCORE_ENVIRONMENT=Production
```

### **3. PostgreSQL:**
```sql
-- Conectar a pgAdmin y ejecutar:
CREATE SCHEMA cliente_demo;

-- Verificar:
SELECT schema_name FROM information_schema.schemata 
WHERE schema_name = 'cliente_demo';
```

### **4. Assets:**
```bash
# Local
mkdir wwwroot-cliente_demo/images
cp logo-demo.png wwwroot-cliente_demo/images/cliente_demo_logo.png

# Commit
git add clients.config.json wwwroot-cliente_demo/
git commit -m "feat: Add Cliente Demo configuration"
git push origin main
```

### **5. Verificar:**
```bash
# Esperar ~5 minutos, luego:
curl https://gestiontimeapi-demo.onrender.com/health

# Respuesta esperada:
{
  "status": "OK",
  "client": "Cliente Demo S.A.",
  "clientId": "cliente_demo",
  "schema": "cliente_demo",
  "database": "connected"
}
```

---

## 📚 **Archivos Relacionados**

- **Configuración:** `clients.config.json`
- **Servicio:** `Services/ClientConfigurationService.cs`
- **Documentación del Servicio:** `CLIENT_CONFIGURATION_SERVICE.md`
- **Guía Multi-Tenant:** `MULTI_TENANT_INTEGRATION_GUIDE.md`
- **Guía wwwroot:** `WWWROOT_CLIENT_CONFIG.md`

---

## ✅ **Resumen: Configuraciones Completas**

| Categoría | Incluida | Ubicación |
|-----------|----------|-----------|
| **Identidad** | ✅ | `Id`, `Name`, `ApiUrl`, `Logo` |
| **Base de Datos** | ✅ | `Database.Schema`, `Database.ConnectionString` |
| **JWT** | ✅ | `Jwt.*` (Issuer, Audience, Key, tiempos) |
| **CORS** | ✅ | `Cors.Origins[]` |
| **Email/SMTP** | ✅ | `Email.*` (Host, Port, User, Password, From) |
| **Características** | ✅ | `Features.*` (confirmación, registro, contraseñas) |
| **Branding** | ✅ | `Branding.*` (colores, logos, favicon) |
| **Contacto** | ✅ | `ContactInfo.*` (email, teléfono, dirección) |
| **Límites** | ✅ | `Limits.*` (usuarios, storage, API requests) |
| **Assets estáticos** | ✅ | `wwwroot-{clientId}/` |

**¡Ahora tienes TODAS las configuraciones necesarias para un deploy completo!** 🎉

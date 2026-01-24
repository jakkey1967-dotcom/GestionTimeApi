# 🏠 Configuración para Desarrollo Local - GestionTime API

Guía completa para configurar y ejecutar el proyecto en tu máquina local.

---

## 📋 Requisitos Previos

Antes de empezar, asegúrate de tener instalado:

| Software | Versión | Enlace |
|----------|---------|--------|
| **.NET SDK** | 8.0 o superior | https://dotnet.microsoft.com/download |
| **PostgreSQL** | 16.x | https://www.postgresql.org/download/ |
| **Git** | Última | https://git-scm.com/ |
| **Visual Studio 2022** | Última (opcional) | https://visualstudio.microsoft.com/ |
| **VS Code** | Última (opcional) | https://code.visualstudio.com/ |

---

## 🗄️ Paso 1: Configurar PostgreSQL Local

### Opción A: Instalación Estándar

1. **Instalar PostgreSQL 16**
   - Durante la instalación, establece password para usuario `postgres`
   - Puerto por defecto: `5432`

2. **Crear Base de Datos**

```sql
-- Conectar como usuario postgres
psql -U postgres

-- Crear la base de datos
CREATE DATABASE pss_dvnx;

-- Conectar a la BD
\c pss_dvnx

-- Crear schema
CREATE SCHEMA pss_dvnx;

-- Verificar
\dn
```

### Opción B: Docker (Recomendado)

```bash
# Descargar imagen de PostgreSQL 16
docker pull postgres:16

# Crear y ejecutar contenedor
docker run --name postgres-gestiontime \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=pss_dvnx \
  -p 5432:5432 \
  -d postgres:16

# Verificar que está corriendo
docker ps

# Conectar al contenedor
docker exec -it postgres-gestiontime psql -U postgres -d pss_dvnx

# Crear schema
CREATE SCHEMA pss_dvnx;
```

### Verificar Conexión

```powershell
# Desde PowerShell
$env:PGPASSWORD = "postgres"
psql -h localhost -p 5432 -U postgres -d pss_dvnx -c "\dn"
```

---

## ⚙️ Paso 2: Configurar appsettings.Development.json

Tu archivo actual está en:
```
C:\GestionTime\GestionTimeApi\appsettings.Development.json
```

### Configuración Actual (Revisar)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning"
    },
    "GestionTime": {
      "EnableDebugLogs": true
    }
  },
  "Jwt": {
    "Key": "v7ZpQ9mL3H2kN8xR1aT6yW4cE0sB5dU9jF2hK7nP3qL8rM1tX6zA4gS9uV2bC5e"
  },
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5434;Database=gestiontime_test;Username=postgres;Password=postgres;Include Error Detail=true"
  },
  "Database": {
    "Schema": "pss_dvnx"
  },
  "Email": {
    "SmtpHost": "smtp.ionos.es",
    "SmtpPort": "587",
    "SmtpUser": "envio_noreplica@tdkportal.com",
    "SmtpPassword": "A4gS9uV2bC5e",
    "From": "envio_noreplica@tdkportal.com",
    "FromName": "GestionTime"
  },
  "App": {
    "BaseUrl": "http://localhost:2501"
  }
}
```

### ⚠️ Ajustes Necesarios

**Si usas PostgreSQL estándar (puerto 5432):**
```json
"ConnectionStrings": {
  "Default": "Host=localhost;Port=5432;Database=pss_dvnx;Username=postgres;Password=TU_PASSWORD;Include Error Detail=true"
}
```

**Si usas Docker:**
```json
"ConnectionStrings": {
  "Default": "Host=localhost;Port=5432;Database=pss_dvnx;Username=postgres;Password=postgres;Include Error Detail=true"
}
```

**Si usas tu configuración actual (puerto 5434):**
- Ya está configurado correctamente
- Solo cambia el password si es diferente

---

## 🔧 Paso 3: Aplicar Migraciones

```powershell
# Navegar al directorio del proyecto
cd C:\GestionTime\GestionTimeApi

# Verificar que tienes EF Core Tools
dotnet tool install --global dotnet-ef

# Aplicar migraciones
dotnet ef database update --project GestionTime.Infrastructure/GestionTime.Infrastructure.csproj --startup-project GestionTime.Api.csproj

# Verificar las migraciones aplicadas
dotnet ef migrations list --project GestionTime.Infrastructure/GestionTime.Infrastructure.csproj --startup-project GestionTime.Api.csproj
```

### Salida Esperada

```
✅ 20250115000000_InitialCreate
✅ 20250115000001_AddUserProfiles
✅ 20250115000002_AddRefreshTokens
✅ 20250124090758_AddUserSessionsForPresence
```

---

## 👤 Paso 4: Crear Usuario Administrador

```powershell
# Opción 1: Con script PowerShell
.\scripts\create-admin-user.ps1 `
  -Email "admin@local.com" `
  -Password "Admin123!" `
  -FullName "Admin Local"

# Opción 2: Con herramienta CLI .NET
dotnet run --project GestionTime.Api.csproj -- seed-admin `
  --email "admin@local.com" `
  --password "Admin123!" `
  --full-name "Admin Local"
```

### Credenciales por Defecto

Si usas el script sin parámetros:
```
Email: admin@admin.com
Password: Admin@2025
```

---

## 🚀 Paso 5: Ejecutar la API

### Desde Línea de Comandos

```powershell
cd C:\GestionTime\GestionTimeApi
dotnet run --project GestionTime.Api.csproj
```

### Desde Visual Studio

1. Abrir `GestionTime.Api.sln`
2. Presionar `F5` o click en ▶️ Start
3. Seleccionar profile: **GestionTime.Api**

### Desde VS Code

1. Abrir carpeta: `C:\GestionTime\GestionTimeApi`
2. Presionar `F5`
3. Seleccionar: **.NET Core Launch (web)**

---

## ✅ Paso 6: Verificar que Funciona

### 1. Health Check

```powershell
# Con script
.\scripts\check-health.ps1

# Con curl
curl http://localhost:2501/health

# Con navegador
# Abre: http://localhost:2501/health
```

**Salida esperada:**
```json
{
  "status": "OK",
  "timestamp": "2025-01-24T11:00:00Z",
  "service": "GestionTime API",
  "version": "1.0.0",
  "client": "PSS_DVNX",
  "schema": "pss_dvnx",
  "database": "connected"
}
```

### 2. Swagger UI

Abre en navegador:
```
http://localhost:2501/swagger
```

Deberías ver la documentación interactiva de la API.

### 3. Login Desktop

```powershell
# PowerShell
$body = @{
    email = "admin@admin.com"
    password = "Admin@2025"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:2501/api/v1/auth/login-desktop" `
  -Method POST `
  -ContentType "application/json" `
  -Body $body | ConvertTo-Json
```

**Salida esperada:**
```json
{
  "message": "ok",
  "userName": "Admin Local",
  "userEmail": "admin@admin.com",
  "userRole": "ADMIN",
  "accessToken": "eyJhbGciOiJI...",
  "refreshToken": "abc123...",
  "sessionId": "guid-here"
}
```

### 4. Consultar Usuarios Online

```powershell
# Guardar el token del login anterior
$token = "eyJhbGciOiJI..."

Invoke-RestMethod -Uri "http://localhost:2501/api/v1/presence/users" `
  -Method GET `
  -Headers @{ Authorization = "Bearer $token" } | ConvertTo-Json
```

---

## 🔧 Configuración de CORS (Si usas frontend local)

Si vas a conectar un frontend (React, Angular, etc.) en otro puerto, necesitas configurar CORS.

### Editar Program.cs (ya configurado)

El proyecto ya tiene CORS configurado para desarrollo:

```csharp
// En Program.cs (ya existe)
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
    {
        policy.WithOrigins(
            "https://localhost:5173",  // Vite
            "http://localhost:5173",
            "https://localhost:2501",  // API
            "http://localhost:2501",
            "http://localhost:3000",   // React
            "http://localhost:4200"    // Angular
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});
```

Si necesitas agregar otro puerto:

```csharp
policy.WithOrigins(
    // ... orígenes existentes
    "http://localhost:TU_PUERTO"  // ← Agregar aquí
)
```

---

## 📁 Estructura de Archivos Locales

```
C:\GestionTime\GestionTimeApi\
├── appsettings.json                  # Config base (no editar)
├── appsettings.Development.json      # ✏️ Config local (editar aquí)
├── GestionTime.Api.csproj
├── Program.cs
├── Controllers/
├── Services/
├── wwwroot/                          # Archivos estáticos comunes
├── wwwroot-pss_dvnx/                 # Archivos del cliente actual
│   └── images/
│       └── LogoOscuro.png           # Logo del cliente
├── scripts/                          # Scripts PowerShell
│   ├── check-health.ps1
│   ├── create-admin-user.ps1
│   └── verify-github-sync.ps1
└── docs/                             # Documentación
```

---

## 🔍 Verificar Configuración Actual

```powershell
# Verificar puerto de PostgreSQL
netstat -an | findstr "5432"
netstat -an | findstr "5434"

# Verificar conexión a BD
$env:PGPASSWORD = "postgres"
psql -h localhost -p 5432 -U postgres -d pss_dvnx -c "SELECT version();"

# Ver schemas
psql -h localhost -p 5432 -U postgres -d pss_dvnx -c "\dn"

# Ver tablas en schema
psql -h localhost -p 5432 -U postgres -d pss_dvnx -c "\dt pss_dvnx.*"
```

---

## 🐛 Troubleshooting

### Problema 1: No puede conectar a PostgreSQL

**Error:**
```
Npgsql.NpgsqlException: Connection refused
```

**Soluciones:**
```powershell
# Verificar que PostgreSQL está corriendo
# Windows
Get-Service -Name postgresql*

# Docker
docker ps

# Verificar puerto correcto
netstat -an | findstr "5432"
```

### Problema 2: Base de datos no existe

**Error:**
```
42P01: relation "pss_dvnx.users" does not exist
```

**Solución:**
```powershell
# Aplicar migraciones
dotnet ef database update --project GestionTime.Infrastructure/GestionTime.Infrastructure.csproj --startup-project GestionTime.Api.csproj
```

### Problema 3: Schema no existe

**Error:**
```
3F000: schema "pss_dvnx" does not exist
```

**Solución:**
```sql
-- Conectar a psql
psql -h localhost -p 5432 -U postgres -d pss_dvnx

-- Crear schema
CREATE SCHEMA pss_dvnx;

-- Salir y aplicar migraciones
\q
```

### Problema 4: Puerto 2501 en uso

**Error:**
```
Only one usage of each socket address is normally permitted
```

**Solución:**
```powershell
# Encontrar proceso usando el puerto
netstat -ano | findstr "2501"

# Matar proceso (cambia PID)
taskkill /PID <PID> /F

# O cambiar puerto en launchSettings.json
# "applicationUrl": "http://localhost:2502"
```

### Problema 5: Compilación falla

**Error:**
```
The type or namespace name could not be found
```

**Solución:**
```powershell
# Restaurar paquetes
dotnet restore

# Limpiar y compilar
dotnet clean
dotnet build
```

---

## 📊 Puertos Usados (Configuración Actual)

| Servicio | Puerto | URL |
|----------|--------|-----|
| **API HTTP** | 2500 | http://localhost:2500 |
| **API HTTPS** | 2501 | https://localhost:2501 |
| **PostgreSQL** | 5434 | localhost:5434 (tu config actual) |
| **PostgreSQL estándar** | 5432 | localhost:5432 (si cambias) |
| **Swagger** | 2501 | http://localhost:2501/swagger |

---

## 🔐 Credenciales de Desarrollo

### Base de Datos
```
Host: localhost
Port: 5434 (o 5432 si cambias)
Database: pss_dvnx
Schema: pss_dvnx
Username: postgres
Password: postgres (o el tuyo)
```

### Usuario Admin
```
Email: admin@admin.com
Password: Admin@2025
Role: ADMIN
```

---

## 🚀 Comandos Rápidos (Cheat Sheet)

```powershell
# Iniciar API
dotnet run --project GestionTime.Api.csproj

# Aplicar migraciones
dotnet ef database update --project GestionTime.Infrastructure --startup-project GestionTime.Api.csproj

# Crear admin
.\scripts\create-admin-user.ps1

# Health check
.\scripts\check-health.ps1

# Compilar
dotnet build

# Limpiar
dotnet clean

# Restaurar paquetes
dotnet restore

# Ver logs en tiempo real
dotnet run --project GestionTime.Api.csproj | Select-String "INF|WRN|ERR"

# Verificar GitHub
.\scripts\verify-github-sync.ps1
```

---

## 📚 Próximos Pasos

1. ✅ API corriendo en local
2. ✅ Usuario admin creado
3. ✅ Health check pasando
4. 📱 Conectar aplicación cliente WinUI
5. 🧪 Testing de endpoints
6. 🔄 Desarrollo de nuevas features

---

## 🔗 Enlaces Útiles

- **Swagger Local:** http://localhost:2501/swagger
- **Health:** http://localhost:2501/health
- **API Root:** http://localhost:2501
- **Documentación:** [docs/INDEX.md](./INDEX.md)
- **Scripts:** [scripts/README.md](../scripts/README.md)

---

## 📝 Notas Finales

### Variables de Entorno Opcionales

Si quieres sobrescribir configuración sin editar archivos:

```powershell
# Cambiar puerto PostgreSQL
$env:DB_PORT = "5432"

# Cambiar schema
$env:DB_SCHEMA = "pss_dvnx"

# Cambiar base de datos
$env:DB_NAME = "pss_dvnx"

# Ejecutar API
dotnet run --project GestionTime.Api.csproj
```

### Desarrollo Multi-Cliente

Si necesitas probar con otro cliente:

1. Duplicar `wwwroot-pss_dvnx` → `wwwroot-cliente2`
2. Editar `clients.config.json`
3. Cambiar variable `CURRENT_CLIENT=cliente2`

Ver: [docs/MULTI_TENANT_INTEGRATION_GUIDE.md](./MULTI_TENANT_INTEGRATION_GUIDE.md)

---

**¡Configuración local completa!** 🎉

Si tienes problemas, consulta la sección de **Troubleshooting** o revisa los logs de la aplicación.

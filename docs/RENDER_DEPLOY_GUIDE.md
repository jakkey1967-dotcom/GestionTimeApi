# 🚀 GUÍA DE DEPLOY EN RENDER - GESTIONTIME API

**Fecha**: 25 de Enero de 2026  
**Servicio**: Render.com  
**Tipo**: Web Service (Docker)

---

## 📋 ÍNDICE

1. [Configuración de Variables de Entorno](#1-configuración-de-variables-de-entorno)
2. [Configuración del Servicio en Render](#2-configuración-del-servicio-en-render)
3. [Base de Datos PostgreSQL](#3-base-de-datos-postgresql)
4. [Verificación Post-Deploy](#4-verificación-post-deploy)
5. [Troubleshooting](#5-troubleshooting)

---

## 1. CONFIGURACIÓN DE VARIABLES DE ENTORNO

### 🔐 Variables REQUERIDAS

Configurar en Render Dashboard → Environment:

```bash
# ========================================
# 🔑 AUTENTICACIÓN Y SEGURIDAD
# ========================================
JWT_secret_key=TU_SECRET_KEY_AQUI_MINIMO_32_CARACTERES_SUPER_SEGURO_123456789
ASPNETCORE_ENVIRONMENT=Production

# ========================================
# 🗄️ BASE DE DATOS
# ========================================
DATABASE_URL=postgresql://usuario:password@host:5432/nombre_db
DB_SCHEMA=pss_dvnx

# ========================================
# 📧 EMAIL (IONOS)
# ========================================
EMAIL__SMTPHOST=smtp.ionos.es
EMAIL__SMTPPORT=587
EMAIL__SMTPUSER=envio_noreplica@tdkportal.com
EMAIL__SMTPPASSWORD=A4gS9uV2bC5e
EMAIL__FROM=envio_noreplica@tdkportal.com
EMAIL__FROMNAME=GestionTime

# ========================================
# 🌐 CORS Y URL BASE
# ========================================
APP__BASEURL=https://gestiontimeapi.onrender.com
CORS__ORIGINS__0=https://gestiontime.vercel.app
CORS__ORIGINS__1=https://gestiontime.tdkportal.com

# ========================================
# 📞 FRESHDESK
# ========================================
FRESHDESK__DOMAIN=alterasoftware
FRESHDESK__APIKEY=9i1AtT08nkY1BlBmjtLk
FRESHDESK__SYNCINTERVALHOURS=24
FRESHDESK__SYNCENABLED=true

# ========================================
# 🔧 CONFIGURACIÓN OPCIONAL
# ========================================
# Habilitar endpoint manual de sync de tags (solo Admin)
FRESHDESK_TAGS_SYNC_API_ENABLED=true

# Logging
LOGGING__LOGLEVEL__DEFAULT=Information
LOGGING__LOGLEVEL__MICROSOFT_ASPNETCORE=Warning
```

---

## 2. CONFIGURACIÓN DEL SERVICIO EN RENDER

### 📦 Crear Web Service

1. **Login en Render**: https://dashboard.render.com

2. **New → Web Service**

3. **Conectar Repositorio**:
   - Repository: `jakkey1967-dotcom/GestionTimeApi`
   - Branch: `main`

4. **Configuración Básica**:

```yaml
Name: gestiontime-api
Region: Frankfurt (EU Central)
Branch: main
Root Directory: (dejar vacío)
Runtime: Docker
```

5. **Build Command**:
```bash
# Render detecta automáticamente el Dockerfile
# No necesita build command
```

6. **Start Command**:
```bash
# Render usa el CMD del Dockerfile
# No necesita start command
```

---

### 🐳 Dockerfile (Verificar que existe)

**Archivo**: `Dockerfile` (en la raíz del proyecto)

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files
COPY ["GestionTime.Api.csproj", "./"]
COPY ["GestionTime.Application/GestionTime.Application.csproj", "GestionTime.Application/"]
COPY ["GestionTime.Domain/GestionTime.Domain.csproj", "GestionTime.Domain/"]
COPY ["GestionTime.Infrastructure/GestionTime.Infrastructure.csproj", "GestionTime.Infrastructure/"]

# Restore
RUN dotnet restore "GestionTime.Api.csproj"

# Copy everything else
COPY . .

# Build
WORKDIR "/src"
RUN dotnet build "GestionTime.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "GestionTime.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Instalar herramientas necesarias
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

# Crear directorio de logs
RUN mkdir -p /app/logs && chmod 777 /app/logs

# Puerto
EXPOSE 8080

# Variables de entorno por defecto
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "GestionTime.Api.dll"]
```

---

### ⚙️ Configuración de Health Checks

**En Render Dashboard → Health & Alerts**:

```yaml
Health Check Path: /health
```

**Asegurar que existe el endpoint** en `Program.cs`:

```csharp
app.MapHealthChecks("/health");
```

---

## 3. BASE DE DATOS POSTGRESQL

### 🗄️ Opción A: Usar PostgreSQL de Render

1. **Crear Base de Datos en Render**:
   - Dashboard → New → PostgreSQL
   - Name: `gestiontime-db`
   - Region: **Frankfurt (mismo que la API)**
   - Plan: Free o Starter

2. **Conectar a la API**:
   - Copiar **Internal Database URL**
   - Pegar en variable de entorno `DATABASE_URL`

**Formato**:
```
postgresql://usuario:password@dpg-XXXXX-frankfurt-postgres:5432/gestiontime_XXXX
```

---

### 🗄️ Opción B: Usar Base de Datos Externa

Si ya tienes PostgreSQL en otro servidor:

```bash
DATABASE_URL=postgresql://usuario:password@tu-host.com:5432/nombre_db
```

---

### 📋 Aplicar Migraciones

**Opción 1**: Automático al iniciar (recomendado)

En `Program.cs` ya existe:

```csharp
// Aplicar migraciones automáticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GestionTimeDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("🔍 Verificando estado de base de datos...");
    await db.Database.EnsureCreatedAsync();
    
    var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
    if (pendingMigrations.Any())
    {
        logger.LogInformation("⏳ Aplicando {Count} migraciones pendientes...", pendingMigrations.Count());
        await db.Database.MigrateAsync();
    }
}
```

**Opción 2**: Manual desde CLI

```bash
# Conectar vía SSH a Render (si está habilitado)
dotnet ef database update --project GestionTime.Infrastructure
```

---

## 4. VERIFICACIÓN POST-DEPLOY

### ✅ Checklist de Verificación

#### 1. **API Arrancó Correctamente**

```bash
curl https://gestiontimeapi.onrender.com/health
```

**Respuesta esperada**:
```
Healthy
```

---

#### 2. **Swagger Accesible**

```bash
https://gestiontimeapi.onrender.com/swagger
```

**Debe cargar la UI de Swagger**

---

#### 3. **Login Funciona**

```bash
curl -X POST https://gestiontimeapi.onrender.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@admin.com","password":"Admin123!"}'
```

**Respuesta esperada**:
```json
{
  "accessToken": "eyJhbG...",
  "refreshToken": "...",
  "user": {
    "id": "...",
    "email": "admin@admin.com",
    "fullName": "Admin User"
  }
}
```

---

#### 4. **Base de Datos Conectada**

Verificar logs en Render:

```
✅ Conexión a BD establecida
✅ Base de datos actualizada (sin migraciones pendientes)
```

---

#### 5. **Freshdesk Funcionando**

```bash
# Login
TOKEN=$(curl -X POST https://gestiontimeapi.onrender.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@admin.com","password":"Admin123!"}' \
  | jq -r '.accessToken')

# Buscar tickets
curl https://gestiontimeapi.onrender.com/api/v1/freshdesk/tickets/suggest?term=&limit=5 \
  -H "Authorization: Bearer $TOKEN"
```

**Respuesta esperada**: Lista de tickets

---

#### 6. **Tags de Partes Funcionando**

```bash
# Crear parte con tags
curl -X POST https://gestiontimeapi.onrender.com/api/v1/partes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fecha_trabajo": "2026-01-25",
    "hora_inicio": "09:00",
    "hora_fin": "11:00",
    "id_cliente": 1,
    "accion": "Test desde Render",
    "tags": ["test", "render", "deploy"]
  }'
```

---

## 5. TROUBLESHOOTING

### 🔧 Problemas Comunes

#### ❌ Error: "Application failed to start"

**Causa**: Variables de entorno mal configuradas

**Solución**:
1. Verificar que `DATABASE_URL` está configurado
2. Verificar que `JWT_secret_key` tiene al menos 32 caracteres
3. Revisar logs en Render Dashboard

---

#### ❌ Error: "Cannot connect to database"

**Causa**: String de conexión incorrecto

**Solución**:
1. Verificar formato de `DATABASE_URL`:
   ```
   postgresql://usuario:password@host:5432/db_name
   ```
2. Si usas Render Postgres, usar **Internal Database URL**
3. Verificar que las reglas de firewall permiten la conexión

---

#### ❌ Error: "Migraciones pendientes"

**Causa**: Base de datos no tiene las tablas

**Solución**:

**Opción 1** (Recomendada): Dejar que la app las aplique automáticamente al iniciar

**Opción 2**: Aplicar manualmente vía psql:

```bash
# Conectar a la BD
psql $DATABASE_URL

# Verificar schema
\dt pss_dvnx.*

# Si faltan tablas, ejecutar desde local:
dotnet ef database update --connection "$DATABASE_URL"
```

---

#### ❌ Error: "CORS policy blocked"

**Causa**: Frontend no está en la lista de orígenes permitidos

**Solución**:
1. Agregar variable de entorno:
   ```bash
   CORS__ORIGINS__2=https://tu-frontend.com
   ```
2. Reiniciar el servicio en Render

---

#### ❌ Error: "Freshdesk sync failed"

**Causa**: API Key de Freshdesk inválida o rate limit

**Solución**:
1. Verificar `FRESHDESK__APIKEY`
2. Verificar `FRESHDESK__DOMAIN`
3. Si quieres deshabilitar sync automático temporalmente:
   ```bash
   FRESHDESK__SYNCENABLED=false
   ```

---

### 📊 Ver Logs en Tiempo Real

**En Render Dashboard**:
1. Ir a tu servicio
2. Click en **Logs**
3. Ver logs en tiempo real

**Filtrar logs importantes**:
```
✅ = Todo OK
⚠️ = Warning
❌ = Error
🔍 = Info de debug
```

---

## 6. CONFIGURACIÓN AVANZADA

### 🔄 Auto-Deploy desde GitHub

**Ya configurado por defecto en Render**:
- Cada push a `main` → Deploy automático
- Pull Requests → Preview deploy (opcional)

**Para deshabilitar auto-deploy**:
- Dashboard → Settings → Auto-Deploy → Off

---

### 🌍 Custom Domain

**Si tienes dominio propio**:

1. Render Dashboard → Settings → Custom Domains
2. Agregar: `api.tudominio.com`
3. Configurar DNS:
   ```
   CNAME api.tudominio.com → gestiontime-api.onrender.com
   ```
4. Render genera SSL automáticamente (Let's Encrypt)

**Actualizar CORS**:
```bash
CORS__ORIGINS__3=https://api.tudominio.com
```

---

### 📈 Monitoreo y Alertas

**Health Checks** (ya configurado):
- Path: `/health`
- Intervalo: 30s
- Timeout: 5s

**Alertas por Email**:
- Dashboard → Health & Alerts
- Configurar notificaciones si el servicio está down > 5 min

---

### 🔒 Secretos Sensibles

**Mejores prácticas**:

1. **Nunca** commitear secretos en `appsettings.Production.json`
2. Usar variables de entorno para TODO lo sensible
3. Rotar secretos cada 3-6 meses:
   - `JWT_secret_key`
   - `FRESHDESK__APIKEY`
   - Passwords de BD

---

## 7. SCRIPT DE DEPLOY

**Archivo**: `scripts/deploy-render.ps1`

```powershell
# Script para verificar configuración antes de deploy

Write-Host "🚀 Verificando configuración para Render..." -ForegroundColor Cyan

# 1. Verificar Dockerfile
if (!(Test-Path "Dockerfile")) {
    Write-Host "❌ Falta Dockerfile" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Dockerfile encontrado" -ForegroundColor Green

# 2. Verificar appsettings.Production.json
if (!(Test-Path "appsettings.Production.json")) {
    Write-Host "❌ Falta appsettings.Production.json" -ForegroundColor Red
    exit 1
}
Write-Host "✅ appsettings.Production.json encontrado" -ForegroundColor Green

# 3. Verificar que no hay secretos hardcodeados
$prodConfig = Get-Content "appsettings.Production.json" -Raw
if ($prodConfig -match '"Key":\s*"[^$]') {
    Write-Host "⚠️ ADVERTENCIA: Posible secret hardcodeado en appsettings.Production.json" -ForegroundColor Yellow
}

# 4. Build local para verificar compilación
Write-Host "`n🔨 Compilando proyecto..." -ForegroundColor Cyan
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error de compilación" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Compilación exitosa" -ForegroundColor Green

# 5. Verificar migraciones
Write-Host "`n📋 Verificando migraciones..." -ForegroundColor Cyan
$migrations = dotnet ef migrations list --project GestionTime.Infrastructure
Write-Host "Migraciones disponibles:"
Write-Host $migrations

Write-Host "`n✅ Todo listo para deploy en Render!" -ForegroundColor Green
Write-Host "`nPróximos pasos:" -ForegroundColor Cyan
Write-Host "1. Hacer commit de los cambios"
Write-Host "2. Push a main: git push origin main"
Write-Host "3. Render detectará el cambio y hará deploy automáticamente"
Write-Host "4. Verificar en: https://dashboard.render.com"
```

---

## 8. CHECKLIST PRE-DEPLOY

Antes de hacer deploy, verificar:

- [ ] ✅ `Dockerfile` existe y está actualizado
- [ ] ✅ `appsettings.Production.json` sin secretos hardcodeados
- [ ] ✅ Variables de entorno configuradas en Render
- [ ] ✅ Base de datos PostgreSQL creada y accesible
- [ ] ✅ `DATABASE_URL` apunta a la BD correcta
- [ ] ✅ `JWT_secret_key` tiene al menos 32 caracteres
- [ ] ✅ CORS configurado con los dominios del frontend
- [ ] ✅ Freshdesk API Key válida
- [ ] ✅ Compilación local exitosa (`dotnet build -c Release`)
- [ ] ✅ Migraciones listas para aplicar
- [ ] ✅ Health check endpoint `/health` funciona

---

## 9. POST-DEPLOY

Después del deploy:

1. **Verificar logs** en Render Dashboard
2. **Probar** `/health` endpoint
3. **Login** en Swagger
4. **Crear un parte de prueba** con tags
5. **Verificar Freshdesk** sync
6. **Monitorear** por 24h para detectar errores

---

## 📞 SOPORTE

**Errores comunes**: Ver sección [Troubleshooting](#5-troubleshooting)

**Logs**: Render Dashboard → Logs

**Render Docs**: https://render.com/docs

**GitHub Issues**: https://github.com/jakkey1967-dotcom/GestionTimeApi/issues

---

**Fin de la Guía de Deploy**

---

*Última actualización: 25 de Enero de 2026*  
*GestionTime API - Deploy en Render.com*

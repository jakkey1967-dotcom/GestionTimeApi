# 🚀 GestionTime API

Sistema de gestión de tiempo y recursos empresariales con arquitectura multi-tenant.

## 📋 Características Principales

### 🔐 Autenticación y Seguridad
- ✅ JWT con cookies HttpOnly
- ✅ Refresh tokens con rotación automática
- ✅ Sistema de activación de usuarios por email
- ✅ Recuperación de contraseña por email
- ✅ Control de expiración de contraseñas
- ✅ Roles y permisos (USER, ADMIN, MANAGER)

### 📧 Sistema de Email
- ✅ **MailKit** para envío robusto de emails
- ✅ Soporte **STARTTLS** (puerto 587)
- ✅ SMTP configurado con **IONOS**
- ✅ Templates HTML responsive
- ✅ Logo embebido en Base64
- ✅ Emails de activación, recuperación y verificación

### 🗄️ Base de Datos Multi-Tenant
- ✅ **PostgreSQL** con schema por cliente
- ✅ Base de datos única: `pss_dvnx`
- ✅ Migraciones automáticas con EF Core
- ✅ Seed automático de datos iniciales
- ✅ Scripts SQL de verificación incluidos

### 👥 Gestión de Usuarios
- ✅ Registro con verificación de email
- ✅ Activar/Desactivar usuarios (Admin)
- ✅ Cambio obligatorio de contraseña
- ✅ Perfiles de usuario extendidos
- ✅ Auditoría de cambios de contraseña

### 📊 API Features
- ✅ Swagger UI integrado
- ✅ Health checks con métricas detalladas
- ✅ Logging estructurado con Serilog
- ✅ CORS configurado para múltiples orígenes
- ✅ Data Protection con claves persistentes

## 🛠️ Tecnologías

- **.NET 8.0** - Framework principal
- **PostgreSQL 16** - Base de datos
- **Entity Framework Core 8** - ORM
- **MailKit** - Envío de emails
- **Serilog** - Logging estructurado
- **BCrypt.Net** - Hash de contraseñas
- **JWT** - Tokens de autenticación
- **Swagger/OpenAPI** - Documentación API

## 📦 Instalación

### Prerrequisitos
- .NET 8 SDK
- PostgreSQL 16+
- Editor (Visual Studio / VS Code / Rider)

### 1. Clonar Repositorio
```bash
git clone https://github.com/jakkey1967-dotcom/GestionTimeApi.git
cd GestionTimeApi
```

### 2. Configurar Base de Datos
```bash
# Crear base de datos
psql -U postgres
CREATE DATABASE pss_dvnx;
\q

# Aplicar migraciones
dotnet ef database update
```

### 3. Configurar Variables de Entorno

**Development** (`appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=pss_dvnx;Username=postgres;Password=postgres"
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

**Production** (Variables de entorno en Render):
```sh
DATABASE_URL=<auto-configurada>
DB_SCHEMA=pss_dvnx
JWT_SECRET_KEY=<tu-secret-key-seguro>
APP_BASE_URL=https://gestiontimeapi.onrender.com
ASPNETCORE_ENVIRONMENT=Production
```

### 4. Ejecutar API
```bash
dotnet run
```

La API estará disponible en:
- **HTTP**: `http://localhost:2501`
- **HTTPS**: `https://localhost:2502`
- **Swagger**: `http://localhost:2501/swagger`

## 🚀 Deploy en Render

### 1. Crear Servicio PostgreSQL
1. New → PostgreSQL
2. Name: `pss_dvnx`
3. Database: `pss_dvnx`
4. Plan: Free o Starter

### 2. Crear Web Service
1. New → Web Service
2. Connect repository: `GestionTimeApi`
3. Build Command: `dotnet publish -c Release -o out`
4. Start Command: `dotnet out/GestionTime.Api.dll`

### 3. Configurar Variables de Entorno
```sh
DATABASE_URL      # Auto-configurada al conectar PostgreSQL
DB_SCHEMA=pss_dvnx
JWT_SECRET_KEY=v7ZpQ9mL3H2kN8xR1aT6yW4cE0sB5dU9jF2hK7nP3qL8rM1tX6zA4gS9uV2bC5e
APP_BASE_URL=https://gestiontimeapi.onrender.com
ASPNETCORE_ENVIRONMENT=Production
```

### 4. Deploy Automático
- Cada push a `main` despliega automáticamente
- Logs visibles en Render Dashboard
- Migraciones se aplican automáticamente

## 📖 Uso de la API

### Registro de Usuario
```http
POST /api/v1/auth/register
Content-Type: application/json

{
  "email": "usuario@ejemplo.com",
  "fullName": "Usuario Test",
  "password": "123456"
}
```

**Respuesta**:
```json
{
  "success": true,
  "message": "Registro exitoso. Revisa tu email para activar tu cuenta."
}
```

### Activación por Email
El usuario recibe un email con un enlace:
```
https://gestiontime-api.onrender.com/api/v1/auth/activate/{token}
```

Al hacer clic, se muestra una página de confirmación y la cuenta se activa.

### Login
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "usuario@ejemplo.com",
  "password": "123456"
}
```

**Respuesta**:
```json
{
  "message": "ok",
  "userName": "Usuario Test",
  "userEmail": "usuario@ejemplo.com",
  "userRole": "USER"
}
```

Los tokens se envían como cookies HttpOnly (`access_token`, `refresh_token`).

### Gestión de Usuarios (Admin)

**Listar Usuarios**:
```http
GET /api/v1/admin/users
Authorization: Bearer {admin-jwt-token}
```

**Desactivar Usuario**:
```http
PUT /api/v1/admin/users/{userId}/enabled
Authorization: Bearer {admin-jwt-token}
Content-Type: application/json

{
  "enabled": false
}
```

**Respuesta**:
```json
{
  "message": "Estado actualizado.",
  "enabled": false
}
```

## 🔧 Scripts Útiles

### Crear Usuario Admin
```bash
dotnet run -- seed-admin
```

O ejecutar SQL:
```sql
-- Ver Tools/SQL/create_admin_user_complete.sql
```

### Verificar Estado de BD
```bash
psql -U postgres -d pss_dvnx -f Tools/SQL/verify_database.sql
```

### Backup de Cliente
```bash
dotnet run -- backup-client pss_dvnx
```

## 📊 Health Check

```http
GET /health
```

**Respuesta**:
```json
{
  "status": "OK",
  "timestamp": "2025-01-01T12:00:00Z",
  "service": "GestionTime API",
  "version": "1.0.0",
  "client": "PSS Desarrollo",
  "clientId": "pss_dvnx",
  "schema": "pss_dvnx",
  "environment": "Production",
  "uptime": "0d 2h 15m 30s",
  "database": "connected",
  "configuration": {
    "jwtAccessMinutes": 15,
    "jwtRefreshDays": 14,
    "emailConfirmationRequired": false,
    "maxUsers": 50
  }
}
```

## 🐛 Troubleshooting

### Email no se envía
```bash
# Verificar configuración SMTP
dotnet user-secrets list

# Test de conexión
curl -X POST http://localhost:2501/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","fullName":"Test","password":"123456"}'

# Ver logs
tail -f logs/gestiontime-*.log
```

### Error de migraciones
```bash
# Resetear migraciones
dotnet ef database drop -f
dotnet ef database update

# Ver migraciones pendientes
dotnet ef migrations list
```

### Usuario deshabilitado
```sql
-- Habilitar usuario
UPDATE pss_dvnx."Users" 
SET "Enabled" = true 
WHERE "Email" = 'usuario@ejemplo.com';
```

## 📝 Documentación

- **Swagger UI**: `http://localhost:2501/swagger`
- **Changelog**: [CHANGELOG_2025-01-01.md](CHANGELOG_2025-01-01.md)
- **Quick Start**: [QUICK_START_DATABASE.md](QUICK_START_DATABASE.md)
- **Troubleshooting**: Ver carpeta `Tools/`

## 🤝 Contribuir

1. Fork el proyecto
2. Crear feature branch (`git checkout -b feature/nueva-funcionalidad`)
3. Commit cambios (`git commit -m 'feat: Nueva funcionalidad'`)
4. Push al branch (`git push origin feature/nueva-funcionalidad`)
5. Abrir Pull Request

## 📄 Licencia

Propietario: TDK Portal  
Todos los derechos reservados © 2025

## 👥 Contacto

- **Email**: soporte@tdkportal.com
- **Web**: https://tdkportal.com
- **GitHub**: https://github.com/jakkey1967-dotcom/GestionTimeApi

---

**Última actualización**: 01 Enero 2025  
**Versión**: 1.0.0  
**Estado**: ✅ Producción
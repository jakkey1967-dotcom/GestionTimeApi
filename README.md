# GestionTime API

Sistema de gestión de tiempo y recursos empresariales con autenticación robusta y sistema de presencia online.

## 🚀 Características Principales

- ✅ **Autenticación JWT** con refresh tokens
- ✅ **Sistema de Presencia** (usuarios online/offline en tiempo real)
- ✅ **Multi-tenant** con soporte para múltiples clientes
- ✅ **Gestión de Usuarios** con roles (ADMIN, EDITOR, USER)
- ✅ **Partes de Trabajo** con estados y tracking
- ✅ **Perfiles de Usuario** extendidos
- ✅ **Recuperación de Contraseña** con códigos de verificación
- ✅ **Registro con Activación** por email

## 🛠️ Stack Tecnológico

- **Framework:** ASP.NET Core 8.0
- **Base de Datos:** PostgreSQL 16
- **ORM:** Entity Framework Core
- **Autenticación:** JWT Bearer
- **Logging:** Serilog
- **Deploy:** Render.com

## 📋 Requisitos

- .NET 8.0 SDK
- PostgreSQL 16
- Visual Studio 2022 / VS Code / Rider

## 🚀 Inicio Rápido

### 1. Clonar Repositorio

```bash
git clone https://github.com/jakkey1967-dotcom/GestionTimeApi.git
cd GestionTimeApi
```

### 2. Configurar Base de Datos

```bash
# Editar appsettings.Development.json con tu connection string
# Aplicar migraciones
dotnet ef database update
```

### 3. Ejecutar

```bash
dotnet run --project GestionTime.Api.csproj
```

La API estará disponible en:
- HTTP: http://localhost:2501
- HTTPS: https://localhost:2502
- Swagger: http://localhost:2501/swagger

## 📚 Documentación

Toda la documentación está en la carpeta [`/docs`](./docs/):

### Guías Principales
- [**IMPLEMENTACION_PRESENCIA.md**](./docs/IMPLEMENTACION_PRESENCIA.md) - Sistema de presencia online
- [**REFRESH_TOKEN_IMPLEMENTATION.md**](./docs/REFRESH_TOKEN_IMPLEMENTATION.md) - Implementación de refresh tokens
- [**DEPLOY_CONFIGURATION_COMPLETE.md**](./docs/DEPLOY_CONFIGURATION_COMPLETE.md) - Deploy en Render
- [**CREATE_ADMIN_USER_GUIDE.md**](./docs/CREATE_ADMIN_USER_GUIDE.md) - Crear usuario admin

### Configuración
- [**CONFIGURATION_VARIABLES.md**](./docs/CONFIGURATION_VARIABLES.md) - Variables de entorno
- [**CLIENT_CONFIGURATION_SERVICE.md**](./docs/CLIENT_CONFIGURATION_SERVICE.md) - Multi-tenant
- [**SCHEMA_CONFIG.md**](./docs/SCHEMA_CONFIG.md) - Configuración de schemas

### Troubleshooting
- [**SAFE_MIGRATION_GUIDE.md**](./docs/SAFE_MIGRATION_GUIDE.md) - Migraciones seguras
- [**FIX_*.md**](./docs/) - Soluciones a problemas comunes

## 🔐 Endpoints Principales

### Autenticación
```
POST /api/v1/auth/login               # Login web (cookies)
POST /api/v1/auth/login-desktop       # Login desktop (JSON tokens)
POST /api/v1/auth/refresh             # Renovar tokens
POST /api/v1/auth/logout              # Logout
POST /api/v1/auth/logout-desktop      # Logout desktop
GET  /api/v1/auth/me                  # Info usuario actual
```

### Presencia (Usuarios Online)
```
GET  /api/v1/presence/users                       # Lista usuarios con estado
POST /api/v1/admin/presence/users/{id}/kick       # Desconectar usuario (admin)
```

### Usuarios (Admin)
```
GET    /api/v1/admin/users            # Listar usuarios
POST   /api/v1/admin/users            # Crear usuario
PUT    /api/v1/admin/users/{id}       # Actualizar usuario
DELETE /api/v1/admin/users/{id}       # Eliminar usuario
```

### Partes de Trabajo
```
GET    /api/v1/partes-trabajo         # Listar partes
POST   /api/v1/partes-trabajo         # Crear parte
PUT    /api/v1/partes-trabajo/{id}    # Actualizar parte
DELETE /api/v1/partes-trabajo/{id}    # Eliminar parte
```

Ver documentación completa en [Swagger](https://gestiontimeapi.onrender.com/swagger).

## 🗄️ Estructura del Proyecto

```
GestionTimeApi/
├── Controllers/           # Controladores API
├── Domain/               # Entidades del dominio
├── Infrastructure/       # Acceso a datos (EF Core)
├── Middleware/          # Middleware personalizado
├── Security/            # JWT, autenticación
├── Services/            # Servicios de negocio
├── Tools/               # Herramientas CLI
├── docs/                # 📚 Documentación completa
├── wwwroot/            # Archivos estáticos comunes
└── wwwroot-{client}/   # Archivos estáticos por cliente
```

## 🔧 Herramientas CLI

El proyecto incluye varias herramientas útiles:

```bash
# Exportar schema de BD
dotnet run --project GestionTime.Api.csproj -- export-schema

# Crear usuario admin
dotnet run --project GestionTime.Api.csproj -- seed-admin

# Verificar estado de Render
dotnet run --project GestionTime.Api.csproj -- check-render

# Backup de base de datos
dotnet run --project GestionTime.Api.csproj -- backup-client
```

Ver más en [`docs/TOOLS_README.md`](./docs/TOOLS_README.md).

## 🚀 Deploy en Producción

La API está desplegada en Render:
- **URL:** https://gestiontimeapi.onrender.com
- **Health:** https://gestiontimeapi.onrender.com/health
- **Swagger:** https://gestiontimeapi.onrender.com/swagger

Ver guía completa: [`docs/DEPLOY_CONFIGURATION_COMPLETE.md`](./docs/DEPLOY_CONFIGURATION_COMPLETE.md)

## 🔒 Seguridad

- ✅ JWT con expiración (15 min access, 7 días refresh)
- ✅ Refresh tokens con rotación
- ✅ Passwords hasheados con BCrypt
- ✅ HttpOnly cookies para web
- ✅ CORS configurado
- ✅ Rate limiting (opcional)
- ✅ Logs de auditoría

## 🧪 Testing

```bash
# Compilar
dotnet build

# Ejecutar tests (cuando existan)
dotnet test

# Verificar sincronización con GitHub
.\scripts\verify-github-sync.ps1

# Verificar health de la API
.\scripts\check-health.ps1
```

## 📊 Estado del Proyecto

**Versión Actual:** v1.2.0  
**Estado:** ✅ Producción

### Últimas Actualizaciones
- ✅ Sistema de presencia implementado (v1.2.0)
- ✅ Refresh tokens dual (web + desktop) (v1.1.0)
- ✅ Multi-tenant configurado (v1.0.5)
- ✅ Autenticación robusta (v1.0.0)

Ver [`docs/CHANGELOG_2025-01-01.md`](./docs/CHANGELOG_2025-01-01.md) para más detalles.

## 📝 Licencia

Propietario: TDK Portal  
Uso exclusivo para GestionTime

## 🤝 Contribuciones

Este es un proyecto privado. Para reportar bugs o sugerencias, contactar al equipo de desarrollo.

## 📞 Contacto

- **Email:** soporte@gestiontime.com
- **GitHub:** https://github.com/jakkey1967-dotcom/GestionTimeApi

---

**Documentación completa en [`/docs`](./docs/)** 📚

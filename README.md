# GestionTime API ??

API RESTful para el sistema de gestión de tiempo y partes de trabajo, desarrollada con .NET 8 y PostgreSQL.

## ?? Características

- ? **Autenticación JWT** con refresh tokens
- ? **Sistema de roles** (ADMIN, USER)
- ? **Gestión de partes de trabajo** completa
- ? **Catálogos** (Clientes, Grupos, Tipos)
- ? **Cambio obligatorio de contraseñas**
- ? **Recuperación de contraseñas** por email
- ? **Logging estructurado** con Serilog
- ? **Documentación automática** con Swagger
- ? **Health checks** para monitoreo

## ??? Tecnologías

- **.NET 8** - Framework principal
- **ASP.NET Core** - Web API
- **Entity Framework Core** - ORM
- **PostgreSQL** - Base de datos
- **JWT** - Autenticación
- **BCrypt** - Hash de contraseñas
- **Serilog** - Logging
- **Swagger** - Documentación API

## ?? Arranque Rápido

### Prerrequisitos

- .NET 8.0 SDK
- PostgreSQL 15+ (o Docker)
- Git

### 1. Clonar el repositorio

```bash
git clone https://github.com/jakkey1967-dotcom/GestionTimeApi.git
cd GestionTimeApi
```

### 2. Configuración

Copia y ajusta el archivo de configuración:

```bash
cp appsettings.json appsettings.Development.json
```

Edita `appsettings.Development.json` con tu configuración local.

### 3. Arranque (Windows)

```cmd
start-dev.bat
```

### 3. Arranque (Linux/macOS)

```bash
chmod +x start-dev.sh
./start-dev.sh
```

### 4. Acceso

- **API:** https://localhost:2501
- **Swagger:** https://localhost:2501/swagger
- **Health:** https://localhost:2501/health

## ?? Docker

### Desarrollo con Docker Compose

```bash
docker-compose up -d
```

Esto levanta:
- API en puerto 8080
- PostgreSQL en puerto 5433
- PgAdmin en puerto 5050

### Producción con Docker

```bash
# Construir imagen
docker build -t gestiontime-api .

# Ejecutar contenedor
docker run -d \
  --name gestiontime-api \
  -p 8080:8080 \
  -e ConnectionStrings__Default="tu-connection-string" \
  gestiontime-api
```

## ?? API Endpoints

### Autenticación

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/v1/auth/login` | Login con email/password |
| POST | `/api/v1/auth/logout` | Cerrar sesión |
| POST | `/api/v1/auth/refresh` | Renovar tokens |
| GET | `/api/v1/auth/me` | Información del usuario |

### Gestión de Contraseñas

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/v1/auth/forgot-password` | Solicitar código recuperación |
| POST | `/api/v1/auth/reset-password` | Resetear con código |
| POST | `/api/v1/auth/change-password` | Cambio obligatorio |

### Partes de Trabajo

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/v1/partes` | Listar partes |
| GET | `/api/v1/partes/{id}` | Obtener parte |
| POST | `/api/v1/partes` | Crear parte |
| PUT | `/api/v1/partes/{id}` | Actualizar parte |
| DELETE | `/api/v1/partes/{id}` | Eliminar parte |

### Catálogos

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/v1/clientes` | Listar clientes |
| GET | `/api/v1/grupos` | Listar grupos |
| GET | `/api/v1/tipos` | Listar tipos |

## ?? Configuración

### Variables de Entorno

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución | `Development` |
| `ConnectionStrings__Default` | Cadena de conexión BD | `Host=localhost;Database=...` |
| `Jwt__Key` | Clave secreta JWT | `tu-clave-secreta-aqui` |

### Base de Datos

Las migraciones se aplican automáticamente al iniciar. Para aplicarlas manualmente:

```bash
dotnet ef database update
```

### Datos de Prueba (Seed)

El sistema incluye datos iniciales:
- Usuarios de prueba con roles
- Catálogos básicos (tipos, grupos, clientes)

Credenciales por defecto:
- **Admin:** `admin@gestiontime.local` / `admin123`
- **Usuario:** `psantos@global-retail.com` / `psantos123`

## ?? Monitoreo

### Health Checks

```bash
curl https://localhost:2501/health
```

### Logs

Los logs se almacenan en:
- **Desarrollo:** `C:\GestionTime\src\GestionTime.Api\logs`
- **Producción:** `/app/logs` (Docker)

## ?? Testing

### Pruebas con Swagger

1. Ve a https://localhost:2501/swagger
2. Haz login con las credenciales de prueba
3. Prueba los endpoints disponibles

### Pruebas con curl

```bash
# Login
curl -X POST https://localhost:2501/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@gestiontime.local","password":"admin123"}'

# Obtener partes (requiere autenticación)
curl https://localhost:2501/api/v1/partes \
  -H "Cookie: access_token=tu-token-aqui"
```

## ?? Seguridad

- **Autenticación:** JWT con refresh tokens en cookies HttpOnly
- **Autorización:** Basada en roles (ADMIN, USER)
- **Contraseñas:** Hasheadas con BCrypt
- **CORS:** Configurado para orígenes específicos
- **Headers de seguridad:** HTTPS, HSTS, etc.

## ?? Logging

El sistema utiliza logging estructurado con niveles:

- **Information:** Operaciones normales
- **Warning:** Situaciones anómalas no críticas
- **Error:** Errores que requieren atención
- **Debug:** Información detallada para desarrollo

## ?? Contribución

1. Fork del repositorio
2. Crear rama de feature (`git checkout -b feature/nueva-funcionalidad`)
3. Commit de cambios (`git commit -am 'Agregar nueva funcionalidad'`)
4. Push a la rama (`git push origin feature/nueva-funcionalidad`)
5. Crear Pull Request

## ?? Soporte

- **Documentación:** https://localhost:2501/swagger
- **Issues:** [GitHub Issues](https://github.com/jakkey1967-dotcom/GestionTimeApi/issues)
- **Email:** soporte@gestiontime.com

## ?? Licencia

Este proyecto está bajo la Licencia MIT. Ver `LICENSE` para más detalles.

---

**?? Estado del Proyecto:** ? Producción Ready

**?? Última Actualización:** Diciembre 2024
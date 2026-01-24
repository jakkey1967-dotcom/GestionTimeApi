# 📜 Scripts PowerShell - GestionTime API

Colección de scripts útiles para administración, deploy y mantenimiento de la API.

---

## 📋 Índice de Scripts

| Script | Descripción | Uso Principal |
|--------|-------------|---------------|
| [setup-local.ps1](#setup-localps1) | **Setup inicial completo** | Primera configuración |
| [check-health.ps1](#check-healthps1) | Verificar estado del endpoint /health | Health check local/producción |
| [create-admin-user.ps1](#create-admin-userps1) | Crear usuario admin + datos iniciales | Setup inicial |
| [export_gtdefault.ps1](#export_gtdefaultps1) | Exportar schema gtdefault a CSV | Backup de datos antiguos |
| [export_schema_dotnet.ps1](#export_schema_dotnetps1) | Exportar cualquier schema a CSV | Backup/migración flexible |
| [verify-github-sync.ps1](#verify-github-syncps1) | Verificar sincronización con GitHub | Pre-deploy, respaldos |

---

## 🚀 setup-local.ps1

**Propósito:** Configuración automática completa para desarrollo local (TODO EN UNO).

### Características
- ✅ Verifica/configura PostgreSQL (local o Docker)
- ✅ Crea base de datos y schema
- ✅ Restaura paquetes NuGet
- ✅ Compila el proyecto
- ✅ Aplica migraciones automáticamente
- ✅ Crea usuario administrador
- ✅ Verifica que todo funciona (health check)

### Uso

```powershell
# Setup completo automático
.\scripts\setup-local.ps1

# Setup con Docker (recomendado)
.\scripts\setup-local.ps1 -UseDocker

# Setup con credenciales personalizadas
.\scripts\setup-local.ps1 `
  -AdminEmail "admin@local.com" `
  -AdminPassword "MiPassword123!" `
  -PostgresPassword "mipassword"

# Solo setup de BD (sin admin)
.\scripts\setup-local.ps1 -SkipAdmin

# Solo setup de BD (sin migraciones)
.\scripts\setup-local.ps1 -SkipMigrations
```

### Parámetros

| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `-PostgresPassword` | string | `postgres` | Password de PostgreSQL |
| `-PostgresPort` | int | `5432` | Puerto de PostgreSQL |
| `-AdminEmail` | string | `admin@local.com` | Email del admin |
| `-AdminPassword` | string | `Admin123!` | Password del admin |
| `-UseDocker` | switch | - | Usar Docker en lugar de PostgreSQL local |
| `-SkipMigrations` | switch | - | No aplicar migraciones |
| `-SkipAdmin` | switch | - | No crear usuario admin |

### Salida

```
╔══════════════════════════════════════════════════════════════╗
║      🚀 SETUP INICIAL - DESARROLLO LOCAL 🚀                 ║
╚══════════════════════════════════════════════════════════════╝

1️⃣  VERIFICANDO POSTGRESQL
   ✅ PostgreSQL corriendo en Docker

2️⃣  CREANDO BASE DE DATOS Y SCHEMA
   ✅ Base de datos configurada

3️⃣  RESTAURANDO PAQUETES NUGET
   ✅ Paquetes restaurados

4️⃣  COMPILANDO PROYECTO
   ✅ Compilación exitosa

5️⃣  APLICANDO MIGRACIONES
   ✅ Migraciones aplicadas

6️⃣  CREANDO USUARIO ADMINISTRADOR
   ✅ Usuario admin creado

7️⃣  INICIANDO API Y VERIFICANDO HEALTH
   ✅ API FUNCIONANDO

╔══════════════════════════════════════════════════════════════╗
║              ✅ SETUP COMPLETADO EXITOSAMENTE ✅             ║
╚══════════════════════════════════════════════════════════════╝

📋 RESUMEN:
   ✅ PostgreSQL configurado
   ✅ Base de datos creada: pss_dvnx
   ✅ Migraciones aplicadas
   ✅ Usuario admin creado

🔑 CREDENCIALES DE ADMIN:
   📧 Email: admin@local.com
   🔐 Password: Admin123!

🚀 PARA INICIAR LA API:
   dotnet run --project GestionTime.Api.csproj
```

---

## 🏥 check-health.ps1

**Propósito:** Verificar el estado del endpoint `/health` de la API.

### Características
- ✅ Verifica conectividad con la API
- ✅ Mide latencia de respuesta
- ✅ Muestra información detallada (JSON)
- ✅ Soporta local y producción (Render)

### Uso

```powershell
# Local (desarrollo)
.\scripts\check-health.ps1

# Local con puerto personalizado
.\scripts\check-health.ps1 -Url "http://localhost:2501"

# Producción (Render)
.\scripts\check-health.ps1 -Render

# Con detalles completos
.\scripts\check-health.ps1 -Render -Detailed
```

### Parámetros

| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `-Url` | string | `http://localhost:5000` | URL base de la API |
| `-Render` | switch | - | Usar URL de producción Render |
| `-Detailed` | switch | - | Mostrar JSON completo de respuesta |

### Salida

```
╔══════════════════════════════════════════════════════════╗
║         🏥 COMPROBACIÓN DE HEALTH CHECK 🏥              ║
╚══════════════════════════════════════════════════════════╝

URL: https://gestiontimeapi.onrender.com/health

✅ API Responde Correctamente

📊 Estado: OK
⏱️  Latencia: 245 ms
🗓️  Timestamp: 2025-01-24T10:30:00Z
🏷️  Service: GestionTime API
📦 Version: 1.0.0
🗄️  Database: connected
```

---

## 👤 create-admin-user.ps1

**Propósito:** Crear usuario administrador con todos los permisos y datos iniciales del sistema.

### Características
- ✅ Crea usuario admin con rol ADMIN
- ✅ Genera roles (ADMIN, EDITOR, USER)
- ✅ Crea tipos de trabajo iniciales
- ✅ Crea grupos de trabajo
- ✅ Soporta base de datos local y Render
- ✅ Validaciones de seguridad

### Uso

```powershell
# Crear admin con valores por defecto
.\scripts\create-admin-user.ps1

# Crear admin personalizado (local)
.\scripts\create-admin-user.ps1 `
  -Email "admin@miempresa.com" `
  -Password "MiPassword123!" `
  -FullName "Juan Pérez"

# Crear admin en Render (producción)
.\scripts\create-admin-user.ps1 -Render

# Recrear usuario (forzar si ya existe)
.\scripts\create-admin-user.ps1 -Force

# Solo crear usuario (sin datos iniciales)
.\scripts\create-admin-user.ps1 -SkipSeedData
```

### Parámetros

| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `-Email` | string | `admin@admin.com` | Email del administrador |
| `-Password` | string | `Admin@2025` | Contraseña del administrador |
| `-FullName` | string | `Administrador del Sistema` | Nombre completo |
| `-Schema` | string | `pss_dvnx` | Schema de base de datos |
| `-Render` | switch | - | Usar base de datos de Render |
| `-Force` | switch | - | Recrear usuario si existe |
| `-SkipSeedData` | switch | - | Omitir datos iniciales |

### Datos Creados

**Roles:**
- ADMIN - Acceso total
- EDITOR - Edición de partes
- USER - Solo lectura

**Tipos de Trabajo:**
- Instalación
- Mantenimiento
- Reparación
- Soporte Técnico
- Revisión
- Configuración
- Actualización
- Diagnóstico

**Grupos:**
- Técnicos
- Soporte
- Administración
- Mantenimiento

### Salida

```
╔══════════════════════════════════════════════════════════╗
║    👤 CREAR USUARIO ADMINISTRADOR + DATOS INICIALES 👤   ║
╚══════════════════════════════════════════════════════════╝

📋 Configuración:
   Email: admin@admin.com
   Password: ********
   Schema: pss_dvnx

✅ Roles creados: ADMIN, EDITOR, USER
✅ Tipos de trabajo creados: 8 tipos
✅ Grupos creados: 4 grupos
✅ Usuario administrador creado exitosamente

📧 Email: admin@admin.com
🔑 Password: Admin@2025
👤 Nombre: Administrador del Sistema
🎭 Rol: ADMIN
```

---

## 📦 export_gtdefault.ps1

**Propósito:** Exportar schema `gtdefault` (antiguo) a archivos CSV para respaldo o migración.

### Características
- ✅ Exporta todas las tablas del schema
- ✅ Genera CSV con datos completos
- ✅ Crea carpeta con timestamp
- ✅ Muestra progreso y estadísticas

### Uso

```powershell
# Exportar gtdefault (requiere DATABASE_URL en variables entorno)
$env:DATABASE_URL = "postgresql://user:pass@host/db"
.\scripts\export_gtdefault.ps1
```

### Salida

```
╔══════════════════════════════════════════════════════════╗
║       📦 EXPORTANDO SCHEMA GTDEFAULT A CSV 📦           ║
╚══════════════════════════════════════════════════════════╝

🔍 Buscando tablas en schema 'gtdefault'...
✅ 12 tablas encontradas

📄 Exportando cliente... (1500 registros)
📄 Exportando grupo... (25 registros)
📄 Exportando tipo... (30 registros)
...

✅ Exportación completada
📁 Archivos generados en: .\gtdefault_export_20250124_103000
```

---

## 🔧 export_schema_dotnet.ps1

**Propósito:** Exportar cualquier schema de PostgreSQL a CSV usando .NET (más flexible que el anterior).

### Características
- ✅ Exporta cualquier schema especificado
- ✅ Conversión automática de DATABASE_URL de Render
- ✅ Usa Npgsql (.NET) para mejor performance
- ✅ Soporte para SSL/TLS

### Uso

```powershell
# Exportar schema específico
.\scripts\export_schema_dotnet.ps1 -Schema "pss_dvnx"

# Con connection string explícito
.\scripts\export_schema_dotnet.ps1 `
  -Schema "pss_dvnx" `
  -ConnectionString "Host=localhost;Database=pss_dvnx;..."

# Desde variable de entorno (Render)
$env:DATABASE_URL = "postgresql://..."
.\scripts\export_schema_dotnet.ps1 -Schema "pss_dvnx"
```

### Parámetros

| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `-Schema` | string | (requerido) | Nombre del schema a exportar |
| `-ConnectionString` | string | `$env:DATABASE_URL` | Connection string PostgreSQL |

### Salida

```
📦 Convirtiendo formato Render a Npgsql...
✅ Connection string convertido

🔍 Buscando tablas en schema 'pss_dvnx'...
✅ 15 tablas encontradas

📄 Exportando users... (350 registros)
📄 Exportando roles... (3 registros)
📄 Exportando partes_trabajo... (12500 registros)
...

✅ Exportación completada
📁 Carpeta: .\pss_dvnx_export_20250124_103000
```

---

## 🔍 verify-github-sync.ps1

**Propósito:** Verificar el estado de sincronización del repositorio con GitHub (útil antes de cambios importantes).

### Características
- ✅ Verifica working tree limpio
- ✅ Compara commits locales vs remoto
- ✅ Lista tags disponibles
- ✅ Muestra últimos commits
- ✅ Verifica compilación
- ✅ Detecta cambios no pusheados

### Uso

```powershell
# Verificación completa
.\scripts\verify-github-sync.ps1
```

### No requiere parámetros

### Salida

```
╔══════════════════════════════════════════════════════════╗
║     🔍 VERIFICACIÓN DE RESPALDO - GitHub Sync Status    ║
╚══════════════════════════════════════════════════════════╝

📋 Estado del Working Tree:
   ✅ Limpio (nada por commitear)

💾 Último Commit Local:
   5b73c4a docs: reorganizar documentación

🌐 Sincronización con GitHub:
   ✅ Sincronizado con origin/main

🏷️  Tags Disponibles:
   📌 v1.2.0-presence-implemented → 9fc166d

📜 Últimos 3 Commits:
   5b73c4a (HEAD -> main, origin/main) docs: reorganizar...
   4d8d1e5 tools: script de verificación...
   00d113d docs: punto de respaldo...

🔗 Remotes Configurados:
   origin  https://github.com/jakkey1967-dotcom/GestionTimeApi.git

🔨 Verificando Compilación:
   ✅ Compilación exitosa

╔══════════════════════════════════════════════════════════╗
║                    📊 RESUMEN FINAL                     ║
╚══════════════════════════════════════════════════════════╝

✅ TODO OK: Repositorio limpio y sincronizado con GitHub
✅ Seguro para hacer cambios en la aplicación cliente

🔄 Para restaurar este punto en el futuro:
   git checkout 5b73c4a
```

---

## 🚀 Casos de Uso Comunes

### Setup Inicial del Proyecto

```powershell
# 1. Crear usuario admin
.\scripts\create-admin-user.ps1

# 2. Verificar que la API funciona
.\scripts\check-health.ps1
```

### Deploy a Producción (Render)

```powershell
# 1. Verificar sincronización con GitHub
.\scripts\verify-github-sync.ps1

# 2. Crear admin en Render (primera vez)
.\scripts\create-admin-user.ps1 -Render

# 3. Verificar health en producción
.\scripts\check-health.ps1 -Render
```

### Respaldo/Migración de Datos

```powershell
# 1. Exportar schema antiguo
.\scripts\export_gtdefault.ps1

# 2. Exportar schema nuevo
.\scripts\export_schema_dotnet.ps1 -Schema "pss_dvnx"
```

### Desarrollo y Testing

```powershell
# Verificar API local
.\scripts\check-health.ps1 -Url "http://localhost:2501"

# Verificar antes de commit importante
.\scripts\verify-github-sync.ps1

# Resetear datos de desarrollo
.\scripts\create-admin-user.ps1 -Force
```

---

## 📝 Notas Importantes

### Variables de Entorno Requeridas

Para scripts que acceden a base de datos:

```powershell
# Para scripts de exportación
$env:DATABASE_URL = "postgresql://user:pass@host:5432/database"

# Para scripts con Render
# DATABASE_URL ya debe estar configurado en Render
```

### Permisos

Todos los scripts requieren:
- ✅ Ejecución de PowerShell habilitada
- ✅ Conexión a internet (scripts con -Render)
- ✅ Acceso a base de datos (scripts de BD)

### Logs

Los scripts generan logs en:
- Consola (stdout)
- Algunos crean carpetas de salida (exportaciones)

---

## 🔧 Troubleshooting

### Error: "No se puede ejecutar scripts"

```powershell
# Habilitar ejecución de scripts (una vez)
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

### Error: "DATABASE_URL no encontrado"

```powershell
# Configurar variable de entorno
$env:DATABASE_URL = "tu_connection_string_aqui"
```

### Error: "No se puede conectar a la API"

```powershell
# Verificar que la API está corriendo
dotnet run --project GestionTime.Api.csproj

# En otra terminal
.\scripts\check-health.ps1
```

---

## 📚 Ver También

- [**Documentación Completa**](../docs/INDEX.md) - Índice de toda la documentación
- [**TOOLS_README.md**](../docs/TOOLS_README.md) - Herramientas CLI en C#
- [**DEPLOY_CONFIGURATION_COMPLETE.md**](../docs/DEPLOY_CONFIGURATION_COMPLETE.md) - Guía de deploy

---

**Última actualización:** 2025-01-24

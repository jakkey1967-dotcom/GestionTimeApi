# 🔧 GestionTime API - Configuración Local v1.9.0

## 📋 Pre-requisitos

1. **.NET 8 SDK** instalado
2. **PostgreSQL** corriendo en puerto `5434` (o el que prefieras)
3. **Visual Studio 2022** o VS Code con extensión C#

---

## 🔑 Claves de Configuración Local

### 1. JWT Secret Key
```
v7ZpQ9mL3H2kN8xR1aT6yW4cE0sB5dU9jF2hK7nP3qL8rM1tX6zA4gS9uV2bC5e
```

### 2. Base de Datos PostgreSQL
```plaintext
Host:     localhost
Port:     5434
Database: pss_dvnx
Username: postgres
Password: postgres
Schema:   pss_dvnx
```

**Connection String completo:**
```
Host=localhost;Port=5434;Database=pss_dvnx;Username=postgres;Password=postgres;Include Error Detail=true;Search Path=pss_dvnx
```

### 3. Email SMTP (Opcional - para desarrollo)
```plaintext
Host:     smtp.ionos.es
Port:     587
User:     envio_noreplica@tdkportal.com
Password: A4gS9uV2bC5e
From:     envio_noreplica@tdkportal.com
```

### 4. Freshdesk API (Opcional)
```plaintext
Domain:  alterasoftware
ApiKey:  [Solicitar al administrador]
Enabled: false (deshabilitado por defecto en local)
```

---

## 🚀 Pasos de Instalación

### Opción A: Usando appsettings.Development.json (Recomendado)

El archivo `appsettings.Development.json` ya tiene todas las claves configuradas. Solo necesitas:

1. **Asegurar PostgreSQL corriendo:**
   ```powershell
   # Verificar que PostgreSQL está activo
   Test-NetConnection localhost -Port 5434
   ```

2. **Crear la base de datos (si no existe):**
   ```powershell
   # Conéctate a PostgreSQL
   psql -h localhost -p 5434 -U postgres
   
   # Ejecuta en psql:
   CREATE DATABASE pss_dvnx;
   \c pss_dvnx
   CREATE SCHEMA pss_dvnx;
   CREATE EXTENSION IF NOT EXISTS pgcrypto;
   ```

3. **Ejecutar la API:**
   ```powershell
   cd C:\GestionTime\GestionTimeApi\GestionTime.Api
   dotnet run
   ```

4. **Acceder a:**
   - API Root: http://localhost:2501
   - Swagger: http://localhost:2501/swagger
   - Health: http://localhost:2501/health

---

### Opción B: Usando Variables de Entorno

Si prefieres usar variables de entorno (útil para probar diferentes configuraciones):

1. **Crear archivo `.env` en la raíz del proyecto:**
   ```bash
   # Copiar plantilla
   cp .env.example .env
   ```

2. **Editar `.env` con tus valores:**
   ```env
   DATABASE_URL=Host=localhost;Port=5434;Database=pss_dvnx;Username=postgres;Password=postgres
   DB_SCHEMA=pss_dvnx
   JWT_SECRET_KEY=v7ZpQ9mL3H2kN8xR1aT6yW4cE0sB5dU9jF2hK7nP3qL8rM1tX6zA4gS9uV2bC5e
   Freshdesk__Domain=alterasoftware
   Freshdesk__ApiKey=DISABLED
   Freshdesk__SyncEnabled=false
   ```

3. **Cargar variables en PowerShell:**
   ```powershell
   # Cargar .env
   Get-Content .env | ForEach-Object {
       if ($_ -match '^([^=]+)=(.*)$') {
           $key = $matches[1]
           $value = $matches[2]
           [Environment]::SetEnvironmentVariable($key, $value, "Process")
           Write-Host "✅ $key = $value"
       }
   }
   
   # Ejecutar API
   dotnet run
   ```

---

## 🗃️ Inicializar Base de Datos

### Script SQL Rápido

```sql
-- 1. Crear base de datos y schema
CREATE DATABASE pss_dvnx;
\c pss_dvnx

CREATE SCHEMA pss_dvnx;
SET search_path TO pss_dvnx;

-- 2. Habilitar extensiones
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 3. Crear usuario admin por defecto
-- (La API hará seed automático al arrancar)
```

### Seed Automático

La API automáticamente:
- ✅ Crea las tablas necesarias
- ✅ Crea usuario admin: `admin@gestiontime.com` / `Admin123!`
- ✅ Crea datos de catálogo iniciales

Para forzar re-seed:
```powershell
dotnet run seed-admin
```

---

## 🧪 Probar la Configuración

### Test 1: Verificar API está corriendo
```powershell
Invoke-RestMethod -Uri "http://localhost:2501/health" | ConvertTo-Json -Depth 5
```

**Respuesta esperada:**
```json
{
  "status": "OK",
  "version": "1.9.0",
  "database": "connected",
  "client": "PSS DVNX",
  "environment": "Development"
}
```

### Test 2: Login con usuario admin
```powershell
$body = @{
    Email = "admin@gestiontime.com"
    Password = "Admin123!"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:2501/api/auth/login" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body

Write-Host "✅ Token recibido: $($response.accessToken.Substring(0, 50))..."
```

### Test 3: Acceder a endpoint protegido
```powershell
$token = $response.accessToken

$headers = @{
    Authorization = "Bearer $token"
}

Invoke-RestMethod -Uri "http://localhost:2501/api/auth/me" `
    -Method GET `
    -Headers $headers
```

---

## 🛠️ Herramientas Útiles

### Resetear Contraseña de Usuario
```powershell
dotnet run reset-password admin@gestiontime.com NuevaPassword123!
```

### Verificar Estado de la BD
```powershell
.\scripts\check-tables.ps1
```

### Ejecutar Migraciones
```powershell
# Ver migraciones pendientes
dotnet ef migrations list

# Aplicar migraciones
dotnet ef database update
```

### Verificar Usuario Admin
```powershell
.\scripts\check-user.ps1
```

---

## 📡 Endpoints Principales (localhost:2501)

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `/health` | GET | Estado del sistema |
| `/api/auth/login` | POST | Login |
| `/api/auth/me` | GET | Info usuario actual |
| `/api/auth/logout` | POST | Cerrar sesión |
| `/api/partes` | GET | Listar partes de trabajo |
| `/api/clientes` | GET | Listar clientes |
| `/api/tipos` | GET | Tipos de trabajo |
| `/api/grupos` | GET | Grupos de clientes |
| `/api/tags` | GET | Tags disponibles |
| `/swagger` | GET | Documentación interactiva |

---

## 🔐 Usuarios de Prueba

### Admin
```
Email:    admin@gestiontime.com
Password: Admin123!
Rol:      Administrador
```

### Usuario Beta Tester (si existe)
```
Email:    betatester@gestiontime.com
Password: Beta123!
Rol:      Usuario normal
```

---

## ⚠️ Troubleshooting

### Error: "No se puede conectar a la base de datos"
```powershell
# Verificar PostgreSQL está corriendo
Test-NetConnection localhost -Port 5434

# Verificar credenciales
psql -h localhost -p 5434 -U postgres -d pss_dvnx
```

### Error: "Las tablas no existen"
```powershell
# Verificar schema
psql -h localhost -p 5434 -U postgres -d pss_dvnx -c "\dt pss_dvnx.*"

# Si no hay tablas, la API las creará al arrancar
# O ejecuta:
dotnet ef database update
```

### Error: "401 Unauthorized"
```powershell
# Regenerar token
$body = @{Email="admin@gestiontime.com";Password="Admin123!"} | ConvertTo-Json
$response = Invoke-RestMethod -Uri "http://localhost:2501/api/auth/login" -Method POST -ContentType "application/json" -Body $body
$token = $response.accessToken
```

### Error: "CORS policy"
- Asegúrate de que el origen del cliente esté en `appsettings.json` > `Cors:Origins`
- Por defecto incluye: `http://localhost:5173`, `http://localhost:2501`

---

## 📝 Notas Importantes

1. **JWT Token Expira:** Cada 12 horas (configurable en `Jwt:AccessMinutes`)
2. **Refresh Token:** Válido por 14 días
3. **Logs:** Se guardan en `logs/log-YYYY-MM-DD.txt`
4. **Schema PostgreSQL:** Siempre usar `pss_dvnx` (multi-tenant en futuro)
5. **Puerto API:** 2501 (HTTP) y 2502 (HTTPS en desarrollo)

---

## 🎯 Próximos Pasos

1. ✅ Configurar entorno local
2. ✅ Probar login y endpoints básicos
3. 🔧 Desarrollar nuevas funcionalidades
4. 🧪 Ejecutar tests con scripts en `/scripts`
5. 🚀 Probar en Render antes de deployment

---

## 📞 Soporte

- **Documentación API:** http://localhost:2501/swagger
- **Health Check:** http://localhost:2501/health
- **Logs:** `logs/log-YYYY-MM-DD.txt`

---

**Versión:** 1.9.0  
**Última actualización:** 2025-01-27

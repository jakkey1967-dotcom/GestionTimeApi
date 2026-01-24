# 👤 Guía Completa: Crear Usuario Administrador + Datos Iniciales

## 📋 **Descripción**

Esta guía documenta **dos métodos** para inicializar la base de datos con:
- ✅ Usuario administrador (`admin@admin.com`) con rol **ADMIN**
- ✅ **Roles** del sistema (ADMIN, EDITOR, USER)
- ✅ **Tipos de Trabajo** (10 tipos: Incidencia, Instalación, etc.)
- ✅ **Grupos de Trabajo** (8 grupos: Administración, Comercial, etc.)

---

## 🎯 **Método 1: Script PowerShell (Recomendado)**

### **Ventajas:**
- ✅ Generación automática de hash BCrypt
- ✅ Soporte para Local y Render
- ✅ Inicializa datos completos del sistema
- ✅ Validaciones y confirmaciones
- ✅ Salida colorida y detallada

### **Requisitos:**
- PowerShell 5.1 o superior
- `psql` instalado y en PATH
- Acceso a la base de datos (local o Render)

### **Uso Básico:**

```powershell
# Local (desarrollo) - Con datos iniciales
.\create-admin-user.ps1

# Render (producción)
.\create-admin-user.ps1 -Render

# Solo crear usuario (sin datos iniciales)
.\create-admin-user.ps1 -SkipSeedData

# Personalizado
.\create-admin-user.ps1 -Email "superadmin@empresa.com" -Password "MiPassword123!" -Schema "cliente_abc"

# Forzar sin confirmación
.\create-admin-user.ps1 -Force
```

### **Parámetros:**

| Parámetro | Tipo | Por Defecto | Descripción |
|-----------|------|-------------|-------------|
| `-Email` | String | `admin@admin.com` | Email del administrador |
| `-Password` | String | `Admin@2025` | Contraseña (mín. 8 caracteres) |
| `-FullName` | String | `Administrador del Sistema` | Nombre completo |
| `-Schema` | String | `pss_dvnx` | Schema de PostgreSQL según cliente |
| `-Render` | Switch | `false` | Usar configuración de Render |
| `-Force` | Switch | `false` | No pedir confirmación |
| `-SkipSeedData` | Switch | `false` | Omitir datos iniciales (solo usuario) |

### **Ejemplos Detallados:**

```powershell
# 1. Inicialización completa (Local)
.\create-admin-user.ps1
# Crea: Usuario + Roles + Tipos + Grupos

# 2. Solo usuario admin (sin datos)
.\create-admin-user.ps1 -SkipSeedData
# Crea: Solo usuario + Roles básicos

# 3. Producción en Render
.\create-admin-user.ps1 -Render -Force
# Usa DATABASE_URL de variable de entorno

# 4. Cliente específico
.\create-admin-user.ps1 -Schema "cliente_abc"
# Schema personalizado

# 5. Admin personalizado para cliente XYZ
.\create-admin-user.ps1 `
    -Email "jefe@clientexyz.com" `
    -Password "Segura2025!" `
    -FullName "Jefe de Operaciones XYZ" `
    -Schema "cliente_xyz"

# 6. Automatizado (CI/CD)
.\create-admin-user.ps1 -Force -Render -SkipSeedData
# Sin interacción, sin datos iniciales
```

---

## 🎯 **Método 2: Script SQL Directo**

### **Ventajas:**
- ✅ No requiere PowerShell
- ✅ Ejecutable directamente en pgAdmin
- ✅ Compatible con cualquier cliente SQL
- ✅ Incluye todos los datos iniciales
- ✅ Documentación inline completa

### **Requisitos:**
- pgAdmin, DBeaver, o psql
- Extensión `pgcrypto` habilitada

### **Ubicación:**
```
Tools/SQL/create_admin_user_complete.sql
```

### **Uso en pgAdmin:**

1. **Abrir pgAdmin** y conectar a tu base de datos
2. **Abrir Query Tool** (F5)
3. **Abrir el archivo** `Tools/SQL/create_admin_user_complete.sql`
4. **(Opcional) Modificar variables:**
   ```sql
   v_email := 'tu-email@ejemplo.com';
   v_password_plain := 'TuContraseña';
   v_full_name := 'Tu Nombre';
   v_schema := 'pss_dvnx';  -- Cambiar según cliente
   ```
5. **Ejecutar** (F5 o botón Execute)

### **Uso con psql:**

```bash
# Local
psql -h localhost -U postgres -d gestiontime -f Tools/SQL/create_admin_user_complete.sql

# Render
psql "postgresql://user:pass@host:5432/dbname" -f Tools/SQL/create_admin_user_complete.sql
```

### **Habilitar pgcrypto (si es necesario):**

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;
```

---

## 📊 **Datos Iniciales Creados**

### **🎭 Roles (3):**
```
- ADMIN  : Acceso completo al sistema
- EDITOR : Puede editar pero no administrar
- USER   : Usuario estándar
```

### **📋 Tipos de Trabajo (10):**
```
1.  Incidencia
2.  Instalación
3.  Aviso
4.  Petición
5.  Facturable
6.  Duda
7.  Desarrollo
8.  Tarea
9.  Ofertado
10. Llamada Overlay
```

### **👥 Grupos de Trabajo (8):**
```
1. Administración
2. Comercial
3. Desarrollo
4. Gestión Central
5. Logística
6. Movilidad
7. Post-Venta
8. Tiendas
```

---

## 🗂️ **Schemas Multi-Tenant**

El sistema soporta múltiples schemas para clientes diferentes:

| Schema | Cliente | Descripción |
|--------|---------|-------------|
| `pss_dvnx` | GestionTime Global-retail.com | Cliente principal (por defecto) |
| `cliente_abc` | Cliente ABC | Cliente secundario con configuración personalizada |
| `cliente_xyz` | Cliente XYZ | Cliente terciario |
| `gestiontime` | Desarrollo | Schema por defecto para desarrollo local |

### **Cambiar Schema en PowerShell:**
```powershell
.\create-admin-user.ps1 -Schema "cliente_abc"
```

### **Cambiar Schema en SQL:**
```sql
v_schema := 'cliente_abc';  -- Línea 29 del script SQL
```

---

## 📊 **Comparación de Métodos**

| Característica | PowerShell | SQL Directo |
|----------------|------------|-------------|
| Fácil de usar | ✅✅✅ | ✅✅ |
| Requiere instalación | psql | pgcrypto |
| Soporte Render | ✅ | ✅ |
| Datos iniciales | ✅ Configurable | ✅ Siempre |
| Validaciones | ✅ | ⚠️ Básicas |
| Salida colorida | ✅ | ❌ |
| Portable | ⚠️ Windows | ✅ Universal |
| Automatizable | ✅ | ✅ |
| Multi-schema | ✅ | ✅ |

---

## 🔐 **Credenciales por Defecto**

```
Email: admin@admin.com
Password: Admin@2025
Schema: pss_dvnx
```

⚠️ **IMPORTANTE:** Cambia la contraseña después del primer login.

---

## 👤 **Características del Usuario Creado**

- ✅ **Email:** `admin@admin.com` (configurable)
- ✅ **Rol:** `ADMIN` (todos los permisos)
- ✅ **Email confirmado:** Sí (puede hacer login inmediatamente)
- ✅ **Usuario habilitado:** Sí
- ✅ **Requiere cambio de contraseña:** No
- ✅ **Contraseña expira en:** 999 días (prácticamente nunca)
- ✅ **Perfil creado:** Sí (con datos básicos)
- ✅ **Datos iniciales:** Roles, Tipos, Grupos (si no se omite)

---

## 🎭 **Permisos del Rol ADMIN**

El rol **ADMIN** tiene acceso completo a:

### **✅ Gestión de Usuarios:**
- `GET /api/v1/admin/users` - Listar usuarios
- `POST /api/v1/admin/users` - Crear usuario
- `PUT /api/v1/admin/users/{id}` - Actualizar usuario
- `DELETE /api/v1/admin/users/{id}` - Eliminar usuario
- `POST /api/v1/admin/users/{id}/enable` - Habilitar usuario
- `POST /api/v1/admin/users/{id}/disable` - Deshabilitar usuario

### **✅ Gestión de Catálogos:**
- `GET /api/v1/tipos` - Listar tipos de trabajo
- `POST /api/v1/tipos` - Crear tipo
- `GET /api/v1/grupos` - Listar grupos
- `POST /api/v1/grupos` - Crear grupo

### **✅ Acceso a Todos los Endpoints:**
- Partes de trabajo
- Clientes
- Estadísticas
- Reportes

---

## 🧪 **Verificar Creación**

### **1. Via SQL:**

```sql
-- Verificar usuario
SELECT 
    u.email,
    u.full_name,
    u.enabled,
    u.email_confirmed,
    array_agg(r.name) as roles
FROM pss_dvnx.users u
LEFT JOIN pss_dvnx.user_roles ur ON u.id = ur.user_id
LEFT JOIN pss_dvnx.roles r ON ur.role_id = r.id
WHERE u.email = 'admin@admin.com'
GROUP BY u.email, u.full_name, u.enabled, u.email_confirmed;

-- Verificar datos iniciales
SELECT 'Roles' as tabla, COUNT(*) as total FROM pss_dvnx.roles
UNION ALL
SELECT 'Tipos', COUNT(*) FROM pss_dvnx.tipo
UNION ALL
SELECT 'Grupos', COUNT(*) FROM pss_dvnx.grupo;
```

### **2. Via API (login):**

```powershell
# PowerShell
$body = @{
    email = "admin@admin.com"
    password = "Admin@2025"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/v1/auth/login" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

```bash
# cURL
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@admin.com","password":"Admin@2025"}'
```

### **3. Verificar catálogos:**

```powershell
# Listar tipos
Invoke-RestMethod http://localhost:5000/api/v1/tipos

# Listar grupos
Invoke-RestMethod http://localhost:5000/api/v1/grupos
```

---

## 🔧 **Solución de Problemas**

### **❌ Error: "Usuario ya existe"**

**Causa:** El email ya está registrado

**Solución 1 (eliminar usuario existente):**
```sql
DELETE FROM pss_dvnx.users WHERE email = 'admin@admin.com';
```

**Solución 2 (usar otro email):**
```powershell
.\create-admin-user.ps1 -Email "admin2@admin.com"
```

---

### **❌ Error: "duplicate key value violates unique constraint"**

**Causa:** Los datos ya existen en la BD

**Solución:** Esto es normal si ejecutas el script múltiples veces. El script usa `ON CONFLICT DO NOTHING`, así que puedes ignorar este error. Solo significa que los datos ya existen.

---

### **❌ Error: "psql: command not found"**

**Causa:** PostgreSQL no está instalado o no está en PATH

**Solución:**
1. Descargar PostgreSQL: https://www.postgresql.org/download/
2. Agregar a PATH: `C:\Program Files\PostgreSQL\16\bin`
3. Reiniciar PowerShell

---

### **❌ Error: "function gen_salt does not exist"**

**Causa:** Extensión `pgcrypto` no habilitada

**Solución:**
```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;
```

---

### **❌ Error: "relation 'tipo' does not exist"**

**Causa:** Las migraciones no se han aplicado o el schema es incorrecto

**Solución:**
```powershell
# Aplicar migraciones
dotnet ef database update

# Verificar schema correcto
.\create-admin-user.ps1 -Schema "pss_dvnx"

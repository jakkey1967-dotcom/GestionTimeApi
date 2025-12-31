# 🚀 Seed Automático de Base de Datos

## 📋 Descripción

El sistema ahora incluye **seed automático** que se ejecuta al iniciar la aplicación. Si la base de datos está vacía (sin usuario admin), se inicializa automáticamente con:

✅ **Usuario Administrador**
✅ **Roles del Sistema** (ADMIN, EDITOR, USER)
✅ **Tipos de Trabajo** (10 tipos)
✅ **Grupos de Trabajo** (8 grupos)

---

## 🎯 ¿Cuándo se ejecuta?

El seed se ejecuta **automáticamente** en el arranque de la aplicación si:

1. ✅ La conexión a la base de datos es exitosa
2. ✅ Las migraciones están aplicadas
3. ✅ **NO existe** el usuario `admin@admin.com`

---

## 📦 ¿Qué se crea?

### 👤 Usuario Administrador

```
Email: admin@admin.com
Password: Admin@2025
Nombre: Administrador del Sistema
Rol: ADMIN
Email Confirmado: Sí ✅
Estado: Habilitado ✅
Expira: 999 días (prácticamente nunca)
```

### 🎭 Roles (3)

```
1. ADMIN   - Acceso completo al sistema
2. EDITOR  - Puede editar pero no administrar
3. USER    - Usuario estándar
```

### 📋 Tipos de Trabajo (10)

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

### 👥 Grupos de Trabajo (8)

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

## 🔧 Configuración del Schema

El seed **respeta el schema configurado** mediante:

### 1️⃣ Variable de Entorno (Prioridad 1)
```bash
DB_SCHEMA=cliente_abc
```

### 2️⃣ Archivo `appsettings.json` (Prioridad 2)
```json
{
  "Database": {
    "Schema": "pss_dvnx"
  }
}
```

### 3️⃣ Valor por Defecto (Prioridad 3)
```
pss_dvnx
```

---

## 🔐 Seguridad

### Hash de Contraseña

El script usa **BCrypt con salt automático**:

```sql
v_password_hash := crypt(v_password_plain, gen_salt('bf', 10));
```

Esto genera un hash compatible con `BCrypt.Net` en C#.

### Requisito: Extensión pgcrypto

El script requiere la extensión `pgcrypto` de PostgreSQL:

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;
```

**Render.com** y la mayoría de servicios PostgreSQL modernos **ya la tienen habilitada** por defecto.

---

## 📊 Logs del Seed

Al arrancar la aplicación, verás en los logs:

```
🔧 Verificando estado de base de datos...
✅ Conexión a BD establecida
📦 Aplicando 1 migraciones pendientes...
✅ Migraciones aplicadas correctamente
🚀 Ejecutando seed de base de datos...
📦 Iniciando creación de datos iniciales...
✅ Inicialización completada:
   👤 Usuario: admin@admin.com
   🔑 Password: Admin@2025
   🎭 Roles: 3
   📋 Tipos: 10
   👥 Grupos: 8
✅ Seed completado exitosamente
```

---

## ⚠️ Comportamiento Idempotente

El script es **idempotente**, lo que significa:

✅ Puede ejecutarse múltiples veces sin causar errores
✅ Si el usuario admin ya existe, **se omite la creación**
✅ Los datos existentes **NO se duplican** (usa `ON CONFLICT DO NOTHING`)
✅ Las secuencias de IDs se resetean automáticamente

---

## 🛠️ Solución de Problemas

### ❌ Error: "Usuario ya existe"

**Causa:** El usuario `admin@admin.com` ya está en la base de datos

**Solución:** Esto es normal. El seed detecta que ya existe y omite la creación:

```
ℹ️  Usuario admin ya existe, omitiendo seed
```

---

### ❌ Error: "function gen_salt does not exist"

**Causa:** Extensión `pgcrypto` no habilitada

**Solución:** Ejecutar en PostgreSQL:

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;
```

---

### ❌ Error: "relation 'users' does not exist"

**Causa:** Las migraciones no se han aplicado

**Solución:** Verificar que las migraciones se apliquen antes del seed:

1. Revisar logs:
   ```
   📦 Aplicando X migraciones pendientes...
   ```

2. Si no se aplican automáticamente:
   ```bash
   dotnet ef database update
   ```

---

## 🔄 Recrear Base de Datos desde Cero

Si necesitas **recrear la base de datos completamente**:

### Opción 1: Eliminar y recrear BD

```sql
-- PostgreSQL
DROP DATABASE gestiontime;
CREATE DATABASE gestiontime;

-- Al arrancar la aplicación, se aplicarán:
-- 1. Migraciones (estructura de tablas)
-- 2. Seed automático (datos iniciales)
```

### Opción 2: Eliminar solo datos

```sql
-- Eliminar usuario admin
DELETE FROM pss_dvnx.users WHERE email = 'admin@admin.com';

-- Eliminar todos los datos
DELETE FROM pss_dvnx.user_roles;
DELETE FROM pss_dvnx.users;
DELETE FROM pss_dvnx.roles;
DELETE FROM pss_dvnx.tipo;
DELETE FROM pss_dvnx.grupo;

-- Al reiniciar la aplicación, el seed se ejecutará nuevamente
```

---

## 🌐 Compatibilidad

### ✅ Entornos Soportados

- **Desarrollo Local** (PostgreSQL local)
- **Render.com** (PostgreSQL managed)
- **Azure Database for PostgreSQL**
- **AWS RDS PostgreSQL**
- **Docker con PostgreSQL**

### ✅ Schemas Multi-Tenant

El seed funciona con **cualquier schema configurado**:

```
pss_dvnx       → Cliente PSS DVNX
cliente_abc    → Cliente ABC
cliente_xyz    → Cliente XYZ
gestiontime    → Desarrollo local
```

---

## 📝 Código Relevante

### Program.cs (líneas 280-302)

```csharp
// 🚀 Seed automático con script SQL completo
try
{
    Log.Information("🚀 Ejecutando seed de base de datos...");
    await GestionTime.Api.Startup.DbSeeder.SeedAsync(app.Services);
    Log.Information("✅ Seed completado exitosamente");
}
catch (Exception ex)
{
    Log.Error(ex, "❌ Error durante el seed");
    
    var message = ex.Message.ToLowerInvariant();
    var isDbAlreadySetup = message.Contains("already exists") || 
                           message.Contains("usuario");
    
    if (isDbAlreadySetup)
    {
        Log.Warning("⚠️ Base de datos ya inicializada. Continuando arranque...");
    }
}
```

### Startup/DbSeeder.cs

```csharp
public static async Task SeedAsync(IServiceProvider services)
{
    // 1. Verificar conexión
    // 2. Obtener schema configurado
    // 3. Verificar si admin existe
    // 4. Ejecutar script SQL completo si es necesario
}
```

---

## 🎓 Documentación Adicional

- **Script SQL Completo:** `Tools/SQL/create_admin_user_complete.sql`
- **Script PowerShell:** `create-admin-user.ps1`
- **Guía Completa:** `CREATE_ADMIN_USER_GUIDE.md`

---

## ✅ Resumen

1. **Al arrancar la aplicación:**
   - ✅ Se aplican las migraciones
   - ✅ Se ejecuta el seed automático (si es necesario)
   - ✅ Se crea usuario admin + roles + datos iniciales

2. **Si la BD ya está inicializada:**
   - ℹ️ Se detecta automáticamente
   - ℹ️ Se omite la creación
   - ℹ️ La aplicación arranca normalmente

3. **Si borras la base de datos:**
   - 🔄 Se recrea automáticamente al arrancar
   - ✅ Con estructura + datos iniciales

---

**🎉 ¡Ya no necesitas crear manualmente el usuario admin!**

El sistema se inicializa automáticamente con todo lo necesario para empezar a trabajar.

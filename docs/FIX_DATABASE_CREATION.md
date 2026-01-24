# ✅ SOLUCIÓN: CREACIÓN AUTOMÁTICA DE BASE DE DATOS Y SCHEMA

## 🔍 Problema Identificado

```
[ERR] ❌ No se puede conectar a la base de datos
[FTL] La aplicación falló al iniciar
System.Exception: No se puede conectar a la base de datos
```

**Y luego:**

```
[ERR] ❌ Error en proceso de seed
Npgsql.PostgresException: 42883: no existe la función gen_salt(unknown, integer)
```

### **Causa Raíz:**
1. La aplicación intentaba conectarse a una base de datos que **no existía**
2. Incluso después de crear la BD, faltaba habilitar la extensión **pgcrypto** necesaria para las funciones de hash de contraseñas (`crypt()`, `gen_salt()`)

---

## ✨ Solución Implementada

### **Nueva función: `EnsureDatabaseAndSchemaExistAsync()`**

Se agregó lógica que se ejecuta **ANTES** de intentar conectarse con Entity Framework:

```csharp
// ✅ CREAR BASE DE DATOS Y SCHEMA SI NO EXISTEN
await EnsureDatabaseAndSchemaExistAsync(connectionString, dbSchema);
```

### **Flujo de inicialización mejorado:**

```
┌─────────────────────────────────────────────────────────────┐
│ 1️⃣  Leer configuración de connection string                 │
│     • DATABASE_URL (Render) o ConnectionStrings:Default     │
├─────────────────────────────────────────────────────────────┤
│ 2️⃣  Obtener schema configurado                              │
│     • DB_SCHEMA (env) o Database:Schema (config)            │
├─────────────────────────────────────────────────────────────┤
│ 3️⃣  🆕 Verificar/Crear BASE DE DATOS                        │
│     • Conectar a 'postgres' (BD de sistema)                 │
│     • Verificar si existe la BD objetivo                    │
│     • Si NO existe: CREATE DATABASE                         │
├─────────────────────────────────────────────────────────────┤
│ 4️⃣  🆕 Verificar/Crear SCHEMA                               │
│     • Conectar a la BD objetivo                             │
│     • Verificar si existe el schema configurado             │
│     • Si NO existe: CREATE SCHEMA                           │
├─────────────────────────────────────────────────────────────┤
│ 5️⃣  🆕 Habilitar extensión PGCRYPTO                         │
│     • CREATE EXTENSION IF NOT EXISTS pgcrypto               │
│     • Necesaria para crypt() y gen_salt()                   │
├─────────────────────────────────────────────────────────────┤
│ 6️⃣  Configurar DbContext (Entity Framework)                 │
├─────────────────────────────────────────────────────────────┤
│ 7️⃣  Aplicar migraciones pendientes                          │
├─────────────────────────────────────────────────────────────┤
│ 8️⃣  Ejecutar seed de datos iniciales                        │
│     • Usar crypt() para hash de contraseñas                 │
├─────────────────────────────────────────────────────────────┤
│ 9️⃣  ✅ Aplicación lista                                     │
└─────────────────────────────────────────────────────────────┘
```

---

## 📝 Código Agregado

### **Función `EnsureDatabaseAndSchemaExistAsync()` (actualizada)**

```csharp
/// <summary>
/// Asegura que la base de datos y el schema existan antes de continuar
/// </summary>
static async Task EnsureDatabaseAndSchemaExistAsync(string connectionString, string schema)
{
    try
    {
        Log.Information("🔍 Verificando existencia de base de datos y schema...");
        
        // Extraer información de la connection string
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;
        var maintenanceDb = "postgres"; // Base de datos por defecto de PostgreSQL
        
        // Crear connection string para conectarse a 'postgres' (BD de sistema)
        var maintenanceConnString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = maintenanceDb
        }.ToString();
        
        // 1. Verificar/Crear la base de datos
        await using (var conn = new NpgsqlConnection(maintenanceConnString))
        {
            await conn.OpenAsync();
            
            // Verificar si la BD existe
            var checkDbCmd = new NpgsqlCommand(
                $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'", 
                conn);
            
            var dbExists = await checkDbCmd.ExecuteScalarAsync() != null;
            
            if (!dbExists)
            {
                Log.Information("📦 Base de datos '{Database}' no existe, creándola...", databaseName);
                
                var createDbCmd = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", conn);
                await createDbCmd.ExecuteNonQueryAsync();
                
                Log.Information("✅ Base de datos '{Database}' creada exitosamente", databaseName);
            }
            else
            {
                Log.Information("✅ Base de datos '{Database}' ya existe", databaseName);
            }
        }
        
        // 2. Verificar/Crear el schema y habilitar extensiones
        await using (var conn = new NpgsqlConnection(connectionString))
        {
            await conn.OpenAsync();
            
            // 2.1 Verificar si el schema existe
            var checkSchemaCmd = new NpgsqlCommand(
                $"SELECT 1 FROM information_schema.schemata WHERE schema_name = '{schema}'", 
                conn);
            
            var schemaExists = await checkSchemaCmd.ExecuteScalarAsync() != null;
            
            if (!schemaExists)
            {
                Log.Information("📦 Schema '{Schema}' no existe, creándolo...", schema);
                
                var createSchemaCmd = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", conn);
                await createSchemaCmd.ExecuteNonQueryAsync();
                
                Log.Information("✅ Schema '{Schema}' creado exitosamente", schema);
            }
            else
            {
                Log.Information("✅ Schema '{Schema}' ya existe", schema);
            }
            
            // 2.2 Habilitar extensión pgcrypto (necesaria para crypt() y gen_salt())
            try
            {
                Log.Information("🔐 Habilitando extensión pgcrypto...");
                
                var enablePgcryptoCmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS pgcrypto", conn);
                await enablePgcryptoCmd.ExecuteNonQueryAsync();
                
                Log.Information("✅ Extensión pgcrypto habilitada");
            }
            catch (Exception ex)
            {
                Log.Warning("⚠️  No se pudo habilitar pgcrypto: {Message}", ex.Message);
                Log.Warning("⚠️  El usuario de BD puede necesitar permisos de superusuario");
                throw;
            }
        }
        
        Log.Information("✅ Verificación de base de datos y schema completada");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "❌ Error verificando/creando base de datos o schema");
        throw;
    }
}
```

### **Using agregado:**

```csharp
using Npgsql; // ✅ Para NpgsqlConnection y NpgsqlConnectionStringBuilder
```

---

## 🎯 Logs Esperados (Arranque Exitoso)

### **Escenario 1: Base de datos NO existe**

```log
[INF] Iniciando GestionTime API...
[INF] Schema de base de datos: pss_dvnx
[INF] 🔍 Verificando existencia de base de datos y schema...
[INF] 📦 Base de datos 'gestiontime_test' no existe, creándola...
[INF] ✅ Base de datos 'gestiontime_test' creada exitosamente
[INF] 📦 Schema 'pss_dvnx' no existe, creándolo...
[INF] ✅ Schema 'pss_dvnx' creado exitosamente
[INF] 🔐 Habilitando extensión pgcrypto...
[INF] ✅ Extensión pgcrypto habilitada
[INF] ✅ Verificación de base de datos y schema completada
[INF] 🔧 Verificando estado de base de datos...
[INF] ✅ Conexión a BD establecida
[INF] 📦 Aplicando 1 migraciones pendientes...
[INF] ✅ Migraciones aplicadas correctamente
[INF] 🚀 Ejecutando seed de base de datos...
[INF] ✅ Script ejecutado correctamente
[INF] 📧 Credenciales: admin@admin.com / Admin@2025
[INF] ✅ Seed completado exitosamente
[INF] GestionTime API iniciada correctamente en puerto 8080
```

### **Escenario 2: Base de datos YA existe**

```log
[INF] Iniciando GestionTime API...
[INF] Schema de base de datos: pss_dvnx
[INF] 🔍 Verificando existencia de base de datos y schema...
[INF] ✅ Base de datos 'gestiontime_test' ya existe
[INF] ✅ Schema 'pss_dvnx' ya existe
[INF] 🔐 Habilitando extensión pgcrypto...
[INF] ✅ Extensión pgcrypto habilitada
[INF] ✅ Verificación de base de datos y schema completada
[INF] 🔧 Verificando estado de base de datos...
[INF] ✅ Conexión a BD establecida
[INF] ✅ Base de datos actualizada (sin migraciones pendientes)
[INF] 🚀 Ejecutando seed de base de datos...
[INF] ⚠️ Base de datos ya inicializada. Continuando arranque...
[INF] GestionTime API iniciada correctamente en puerto 8080
```

---

## 🔧 Configuración Requerida

### **appsettings.Development.json**

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5434;Database=gestiontime_test;Username=postgres;Password=postgres;Include Error Detail=true"
  },
  "Database": {
    "Schema": "pss_dvnx"
  }
}
```

### **Variables de Entorno (Producción/Render)**

```bash
DATABASE_URL=postgresql://user:pass@host:5432/dbname
DB_SCHEMA=pss_dvnx
```

### **⚠️ IMPORTANTE: Permisos de PostgreSQL**

El usuario de base de datos debe tener permisos para:
- Crear bases de datos (si no existe)
- Crear schemas
- **Crear extensiones** (`CREATE EXTENSION`)

En desarrollo local con usuario `postgres` (superusuario), esto no es problema.

En Render u otros servicios managed, estos permisos suelen estar habilitados por defecto.

Si hay problemas de permisos, el administrador debe ejecutar:
```sql
ALTER USER your_user CREATEDB;
-- O conectarse como superusuario y ejecutar:
CREATE EXTENSION IF NOT EXISTS pgcrypto;
```

---

## 🔐 ¿Por qué necesitamos pgcrypto?

La extensión `pgcrypto` proporciona funciones criptográficas para PostgreSQL:

- **`crypt(password, salt)`**: Genera hash bcrypt de una contraseña
- **`gen_salt('bf', rounds)`**: Genera salt bcrypt con N rondas
- **Seguridad**: Bcrypt es resistente a ataques de fuerza bruta

### **Uso en el seed:**

```sql
-- Generar hash bcrypt con 10 rondas
v_password_hash := crypt('Admin@2025', gen_salt('bf', 10));

-- Resultado: $2a$10$... (hash bcrypt de 60 caracteres)
```

---

## ✅ Beneficios de esta Solución

| Antes | Después |
|-------|---------|
| ❌ Error si la BD no existe | ✅ Crea automáticamente la BD |
| ❌ Error si el schema no existe | ✅ Crea automáticamente el schema |
| ❌ Error "no existe la función gen_salt" | ✅ Habilita automáticamente pgcrypto |
| ❌ Arranque fallaba inmediatamente | ✅ Arranque resiliente e idempotente |
| ❌ Requería setup manual de BD | ✅ Setup completamente automático |
| ❌ Difícil deployment en Render | ✅ Deployment sin fricción |

---

## 🧪 Testing

### **Test 1: Base de datos nueva (desde cero)**

```bash
# 1. Eliminar BD si existe
psql -U postgres -c "DROP DATABASE IF EXISTS gestiontime_test;"

# 2. Ejecutar aplicación
cd C:\GestionTime\GestionTimeApi
dotnet run

# ✅ Esperado: BD, schema y pgcrypto creados automáticamente
```

### **Test 2: Base de datos existente (idempotencia)**

```bash
# 1. Ejecutar aplicación (BD ya existe)
cd C:\GestionTime\GestionTimeApi
dotnet run

# ✅ Esperado: Detecta BD existente, verifica pgcrypto
```

### **Test 3: Verificar en PostgreSQL**

```sql
-- Verificar base de datos
SELECT datname FROM pg_database WHERE datname = 'gestiontime_test';

-- Verificar schema
SELECT schema_name FROM information_schema.schemata WHERE schema_name = 'pss_dvnx';

-- Verificar extensión pgcrypto
SELECT extname, extversion 
FROM pg_extension 
WHERE extname = 'pgcrypto';

-- Probar funciones de pgcrypto
SELECT crypt('test', gen_salt('bf', 10));

-- Verificar tablas en el schema
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'pss_dvnx';

-- Verificar usuario admin creado
SELECT email, full_name, enabled, email_confirmed
FROM pss_dvnx.users
WHERE email = 'admin@admin.com';
```

---

## 🚀 Deployment en Render

### **Ventajas:**

1. **Primera ejecución:** 
   - La aplicación crea automáticamente el schema en la base de datos de Render
   - Habilita pgcrypto automáticamente
   - No requiere ejecutar scripts SQL manualmente

2. **Migraciones:**
   - Se aplican automáticamente después de crear el schema
   - Idempotentes: no fallan si ya están aplicadas

3. **Seed:**
   - Datos iniciales (roles, usuario admin) se crean automáticamente
   - Usa bcrypt para hash seguro de contraseñas
   - Maneja gracefully si ya existen

### **Variables de Entorno en Render:**

```bash
DATABASE_URL=postgresql://... (automático)
DB_SCHEMA=pss_dvnx
JWT_SECRET_KEY=...
```

### **Credenciales por Defecto:**

```
Email: admin@admin.com
Password: Admin@2025
```

**⚠️ CAMBIAR en producción después del primer login**

---

## 📚 Archivos Modificados

- ✅ `GestionTimeApi/Program.cs`
  - Agregado: `using Npgsql;`
  - Agregado: Llamada a `EnsureDatabaseAndSchemaExistAsync()`
  - Agregado: Función `EnsureDatabaseAndSchemaExistAsync()` con habilitación de pgcrypto

---

## ✅ Estado Final

```
┌────────────────────────────────────────────────────┐
│  ✅ SOLUCIÓN COMPLETADA E INTEGRADA                │
├────────────────────────────────────────────────────┤
│  • Creación automática de base de datos           │
│  • Creación automática de schema                  │
│  • Habilitación automática de pgcrypto            │
│  • Idempotente (no falla si ya existe)            │
│  • Logs informativos en cada paso                 │
│  • Compatible con desarrollo local y Render       │
│  • Sin errores de compilación                     │
│  • Seed funciona con hash bcrypt                  │
└────────────────────────────────────────────────────┘
```

---

## 🐛 Troubleshooting

### **Error: "no existe la función gen_salt"**

**Causa:** Extension pgcrypto no habilitada  
**Solución:** Ya implementada automáticamente en el código

### **Error: "permission denied to create extension"**

**Causa:** Usuario de BD sin permisos  
**Solución:** 
```sql
-- Como superusuario:
CREATE EXTENSION IF NOT EXISTS pgcrypto;
-- O dar permisos:
ALTER USER your_user SUPERUSER;
```

### **Error: "database does not exist"**

**Causa:** No hay permisos para crear BD  
**Solución:**
```sql
-- Como superusuario:
ALTER USER your_user CREATEDB;
```

---

**Fecha:** 2024-12-31  
**Autor:** GitHub Copilot  
**Versión:** 2.0 (con pgcrypto)  
**Commit:** Agregar creación automática de BD, schema y pgcrypto en Program.cs

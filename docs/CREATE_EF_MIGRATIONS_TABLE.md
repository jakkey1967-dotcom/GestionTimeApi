# ========================================
# 📋 CREAR TABLA __EFMigrationsHistory - GUÍA RÁPIDA
# ========================================

## Opción 1: Desde Render Dashboard (MÁS FÁCIL) ✅

### Pasos:
1. **Ir a Render Dashboard:**
   ```
   https://dashboard.render.com/
   ```

2. **Seleccionar tu PostgreSQL Database**
   - Click en el servicio de base de datos

3. **Abrir PSQL Shell:**
   - Ir a pestaña "Shell"
   - O usar el botón "Connect" → "PSQL Command"

4. **Ejecutar este SQL directamente:**
   ```sql
   -- Conectar al schema
   SET search_path TO pss_dvnx, public;

   -- Crear tabla
   CREATE TABLE IF NOT EXISTS pss_dvnx."__EFMigrationsHistory" (
       "MigrationId" character varying(150) NOT NULL,
       "ProductVersion" character varying(32) NOT NULL,
       CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
   );

   -- Verificar
   SELECT * FROM pss_dvnx."__EFMigrationsHistory";
   ```

5. **Reiniciar el servicio Web:**
   - Ir a tu servicio Web (gestiontimeapi)
   - Click en "Manual Deploy" → "Deploy latest commit"

---

## Opción 2: Desde tu PC con psql

### Requisitos:
- PostgreSQL Client instalado (`psql`)
- Windows: `choco install postgresql`

### Pasos:
1. **Obtener credenciales de Render:**
   - Dashboard → PostgreSQL → Info → Connection
   - Copiar:
     - Host (Internal Database URL)
     - Database Name
     - Username
     - Password

2. **Editar el script:**
   ```powershell
   notepad .\scripts\create-ef-migrations-table-render.ps1
   ```
   - Reemplazar valores de conexión

3. **Ejecutar:**
   ```powershell
   .\scripts\create-ef-migrations-table-render.ps1
   ```

---

## Opción 3: SQL Directo (copiar y pegar)

Si tienes acceso directo a psql en Render, ejecutar:

```sql
-- 1. Verificar schema
SELECT schema_name 
FROM information_schema.schemata 
WHERE schema_name = 'pss_dvnx';

-- 2. Crear tabla
CREATE TABLE IF NOT EXISTS pss_dvnx."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- 3. Verificar creación
\dt pss_dvnx."__EFMigrationsHistory"

-- 4. Ver contenido (debe estar vacío)
SELECT * FROM pss_dvnx."__EFMigrationsHistory";
```

---

## ✅ Verificación Final

Después de crear la tabla:

1. **Verificar tabla existe:**
   ```sql
   SELECT tablename 
   FROM pg_tables 
   WHERE schemaname = 'pss_dvnx' 
     AND tablename = '__EFMigrationsHistory';
   ```

2. **Reiniciar servicio en Render**

3. **Probar API:**
   ```powershell
   curl https://gestiontimeapi.onrender.com/health
   ```

4. **Ejecutar tests:**
   ```powershell
   .\scripts\test-tags-render.ps1
   ```

---

## 🆘 Si sigue fallando:

Verificar logs en Render:
```
Dashboard → Web Service → Logs
```

Buscar errores relacionados con:
- `__EFMigrationsHistory`
- `relation does not exist`
- `schema pss_dvnx`

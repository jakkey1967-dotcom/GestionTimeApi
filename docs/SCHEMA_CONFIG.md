# 🗄️ Configuración del Schema de Base de Datos

## 📋 **Configuración del Schema**

El sistema permite configurar dinámicamente el nombre del schema de PostgreSQL donde se crearán todas las tablas.

### **Opción 1: Configurar en `appsettings.json`**

```json
{
  "Database": {
    "Schema": "nombre_del_schema"
  }
}
```

**Ejemplos:**
- `"gestiontime"` - Schema por defecto
- `"cliente1"` - Para multi-tenancy
- `"pss_dvnx"` - Nombre personalizado

### **Opción 2: Variable de Entorno**

Para Render u otros servicios cloud:

```bash
DB_SCHEMA=nombre_del_schema
```

**La variable de entorno tiene prioridad** sobre `appsettings.json`.

---

## 🚀 **Configuración en Render.com**

1. Ve a tu servicio en Render Dashboard
2. Settings → Environment
3. Agrega variable: `DB_SCHEMA=gestiontime`
4. Guarda y redeploy

---

## 🏗️ **Creación Inicial de la Base de Datos**

Al arrancar la aplicación por primera vez:

1. ✅ Lee la configuración del schema
2. ✅ Crea el schema si no existe: `CREATE SCHEMA IF NOT EXISTS gestiontime;`
3. ✅ Crea todas las tablas dentro del schema
4. ✅ Aplica migraciones de Entity Framework

---

## 📝 **Verificar Schema Actual**

```powershell
dotnet run -- check-render
```

Mostrará:
```
Schema: gestiontime (10 tablas)
├── users
├── roles
└── ...
```

---

## 🔧 **Cambiar de Schema**

### **Método 1: Limpiar y recrear (DESTRUCTIVO)**

```powershell
# 1. Cambiar configuración
# Editar appsettings.json: "Schema": "nuevo_schema"

# 2. Limpiar BD actual
dotnet run -- clean-render

# 3. Arrancar aplicación (creará todo en nuevo schema)
dotnet run
```

### **Método 2: Migrar datos existentes**

```sql
-- Crear nuevo schema
CREATE SCHEMA IF NOT EXISTS nuevo_schema;

-- Mover tablas
ALTER TABLE gestiontime.users SET SCHEMA nuevo_schema;
ALTER TABLE gestiontime.roles SET SCHEMA nuevo_schema;
-- ... (resto de tablas)
```

---

## 🎯 **Multi-Tenancy por Schema**

Si quieres usar un schema diferente por cliente:

```json
// appsettings.Cliente1.json
{
  "Database": {
    "Schema": "cliente1"
  }
}

// appsettings.Cliente2.json
{
  "Database": {
    "Schema": "cliente2"
  }
}
```

Arrancar con:
```bash
dotnet run --environment Cliente1
```

---

## 📊 **Estructura Recomendada**

```
PostgreSQL Database: pss_dvnx
├── Schema: gestiontime
│   ├── users
│   ├── roles
│   ├── user_roles
│   ├── refresh_tokens
│   ├── user_profiles
│   ├── cliente
│   ├── grupo
│   ├── tipo
│   └── partesdetrabajo
│
├── Schema: cliente1 (opcional)
│   └── ... (mismas tablas)
│
└── Schema: public
    └── (vacío - no usar)
```

---

## ⚠️ **Notas Importantes**

1. **El schema debe existir antes de las migraciones** o usar `EnsureSchema()` en la migración
2. **Todas las tablas se crean en el mismo schema** (no mezclar)
3. **Los scripts SQL deben usar el schema correcto** (ej: `gestiontime.users`)
4. **Cambiar el schema requiere recrear las migraciones**

---

## 🛠️ **Troubleshooting**

### Error: "schema does not exist"
**Solución:** Crear schema manualmente:
```sql
CREATE SCHEMA IF NOT EXISTS gestiontime;
```

### Error: "relation does not exist"
**Causa:** Schema incorrecto en queries SQL
**Solución:** Usar `schema.tabla` en todos los queries

### Tablas en schema incorrecto
**Solución:** Moverlas:
```sql
ALTER TABLE public.users SET SCHEMA gestiontime;
```

---

## 📚 **Documentación Adicional**

- [PostgreSQL Schemas](https://www.postgresql.org/docs/current/ddl-schemas.html)
- [EF Core Schema Configuration](https://learn.microsoft.com/en-us/ef/core/modeling/relational/schemas)

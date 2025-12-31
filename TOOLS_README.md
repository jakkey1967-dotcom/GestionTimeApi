# ?? Herramientas de Deployment y Sincronización de Base de Datos

Este proyecto incluye herramientas automatizadas para gestionar deployments a Render y sincronizar bases de datos PostgreSQL.

## ?? Índice

- [Herramientas Disponibles](#herramientas-disponibles)
- [Configuración](#configuración)
- [Uso](#uso)
- [Flujo de Trabajo Recomendado](#flujo-de-trabajo-recomendado)

---

## ??? Herramientas Disponibles

### 1. **deploy-render** - Deployment Automatizado Completo

Realiza un deployment completo a Render con verificación automática.

```powershell
dotnet run -- deploy-render "mensaje del commit"
```

#### ¿Qué hace?

1. ? **Limpieza opcional de BD** (pregunta al usuario)
2. ? **Verifica estado Git** (branch actual, cambios pendientes)
3. ? **Commit y Push automático** a GitHub
4. ? **Espera deployment de Render** (monitorea hasta 10 minutos)
5. ? **Verifica base de datos** (tablas, migraciones)
6. ? **Verifica API** (health check, swagger)
7. ? **Genera log completo** con timestamp

#### Output Ejemplo

```
????????????????????????????????????????????????????????????
?       ?? DEPLOYMENT AUTOMATIZADO A RENDER ??            ?
????????????????????????????????????????????????????????????

??  ¿Deseas limpiar la base de datos de Render primero? (s/N): 
> s

??? LIMPIANDO BASE DE DATOS...
  ?? Conectado a Render
  ??? Limpiando schema 'public'... ?
? Base de datos limpiada

?? PASO 1: Verificando repositorio Git...
  ?? Branch: main
  ?? Cambios detectados:
     M Tools/DeployToRender.cs
  ? OK

?? PASO 2: Commit y Push a GitHub...
  ? Archivos agregados
  ? Commit realizado
  ?? Pusheando a GitHub...
  ? Push exitoso

?? PASO 3: Verificando estado actual de Render...
  ? Conexión a BD establecida
  ?? Tablas en BD: 0

? PASO 4: Esperando deployment (esto puede tardar 5-10 minutos)...
  ?? Intento 1 (0.5 min)... ? Aún no disponible...
  ?? Intento 2 (1.0 min)... ? Aún no disponible...
  ?? Intento 3 (1.5 min)... ? API respondiendo

??? PASO 5: Verificando base de datos...
  ?? Tablas: 10
  ?? Columnas en 'users': 9
  ? Columna 'email_confirmed': Presente

?? PASO 6: Verificando API en línea...
  ?? Health: ? OK
  ?? Swagger: ? OK

????????????????????????????????????????????????????????????
?           ? DEPLOYMENT EXITOSO ?                       ?
????????????????????????????????????????????????????????????

?? RESULTADOS:
  • Tiempo total: 6.8 minutos
  • URL: https://gestiontime-api.onrender.com
  • Swagger: https://gestiontime-api.onrender.com/swagger

?? Log guardado: DEPLOY_LOG_20251231_024530.txt
```

---

### 2. **check-render** - Diagnóstico de Base de Datos

Verifica el estado actual de la base de datos en Render.

```powershell
dotnet run -- check-render
```

#### Información que muestra:

- ? Schemas disponibles
- ? Tablas por schema con número de columnas
- ? Número de registros en cada tabla
- ? Versión de PostgreSQL
- ? Tamaño de la base de datos

---

### 3. **clean-render** - Limpieza de Base de Datos

Elimina completamente todos los schemas y tablas de Render.

```powershell
dotnet run -- clean-render
```

?? **ADVERTENCIA**: Requiere confirmación escribiendo `SI ESTOY SEGURO`

---

### 4. **sync-schema** - Sincronización Directa

Copia datos directamente desde Render a tu base de datos local.

```powershell
dotnet run -- sync-schema
```

#### Configuración:

- **Origen**: Hardcoded a Render (automático)
- **Destino**: Te pide datos de conexión localhost

#### Modos disponibles:

1. **TRUNCATE** - Vacía tablas y copia (recomendado)
2. **APPEND** - Agrega datos (puede causar duplicados)
3. **DROP & CREATE** - Elimina y recrea tablas

---

### 5. **export-schema** - Exportar a CSV

Exporta un schema completo a archivos CSV.

```powershell
dotnet run -- export-schema
```

Interactivo: te pregunta origen y schema a exportar.

---

### 6. **import-schema** - Importar desde CSV

Importa datos desde archivos CSV a la base de datos.

```powershell
dotnet run -- import-schema [ruta_carpeta]
```

Sin argumentos: busca automáticamente carpetas `*_export_*`

---

## ?? Configuración

### Datos de Conexión Hardcoded

Las herramientas ya tienen configurados los datos de Render:

```csharp
// En Tools/DeployToRender.cs y Tools/SyncSchema.cs
private static readonly string RenderConnectionString = 
    "Host=dpg-d57tobm3jp1c73b6i4ug-a.frankfurt-postgres.render.com;" +
    "Port=5432;" +
    "Database=pss_dvnx;" +
    "Username=gestiontime;" +
    "Password=BvCDRFguh9SljJJUZOzGpdvpxgf18qnI;" +
    "SSL Mode=Require;" +
    "Trust Server Certificate=true";

private static readonly string RenderApiUrl = 
    "https://gestiontime-api.onrender.com";
```

### Schema por Defecto

El proyecto usa **`public`** como schema por defecto:

```csharp
// En GestionTimeDbContext.cs
b.HasDefaultSchema("public");
```

---

## ?? Flujo de Trabajo Recomendado

### Deployment a Render

```powershell
# 1. Hacer cambios en el código
# 2. Ejecutar deployment automático
dotnet run -- deploy-render "feat: nueva funcionalidad"

# Responde 's' si quieres limpiar la BD
# Responde 'n' si solo quieres actualizar código
```

### Sincronizar de Render a Localhost

```powershell
# 1. Verificar estado de Render
dotnet run -- check-render

# 2. Si todo está OK, sincronizar
dotnet run -- sync-schema

# Datos localhost:
# Puerto: 5434 (o tu puerto local)
# Base de datos: gestiontime
# Usuario: postgres
# Password: tu_password
```

### Troubleshooting

```powershell
# Ver estado de Render
dotnet run -- check-render

# Limpiar BD si hay problemas
dotnet run -- clean-render

# Deployment forzando limpieza
dotnet run -- deploy-render "fix: rebuild database"
# Responde 's' a la pregunta de limpiar BD
```

---

## ?? Logs Generados

Cada deployment genera un log completo:

```
DEPLOY_LOG_20251231_024530.txt
```

Contiene:
- Fecha y hora de inicio
- Branch y cambios de Git
- Estado de BD antes y después
- Tiempo de deployment
- Estado de API (health, swagger)
- Errores (si los hay)

---

## ?? Cómo Funciona el Push a GitHub

El comando `deploy-render` hace lo siguiente:

```csharp
// 1. Git add
await RunCommand("git", "add -A");

// 2. Git commit
await RunCommand("git", $"commit -m \"{message}\"");

// 3. Git push
await RunCommand("git", "push origin main");
```

**Render detecta automáticamente** el push a GitHub y:
1. Inicia un nuevo deployment
2. Descarga el código actualizado
3. Compila la aplicación
4. Aplica migraciones pendientes
5. Ejecuta el seed de datos
6. Marca como "Live" cuando todo está OK

El script espera monitoreando `/health` hasta que Render responda.

---

## ?? Requisitos

- ? .NET 8.0 SDK
- ? Git instalado y configurado
- ? Conexión a Internet
- ? Acceso a la base de datos de Render
- ? PostgreSQL local (para sync-schema)

---

## ?? Solución de Problemas Comunes

### Error: "Tablas con estructura vieja"

**Solución**: Limpia la BD y redeploy

```powershell
dotnet run -- deploy-render "fix: rebuild database"
# Responde 's' para limpiar BD
```

### Error: "Deployment timeout"

**Causa**: Render tarda más de 10 minutos

**Solución**: 
1. Ve a https://dashboard.render.com
2. Verifica el estado del deployment manualmente
3. Una vez "Live", ejecuta:
```powershell
dotnet run -- check-render
```

### Error: "No hay cambios para commitear"

**Causa**: No hay cambios en Git

**Solución**: El push se hace igual (no es un error real)

---

## ?? Comandos Rápidos

```powershell
# Deployment completo (sin limpieza)
dotnet run -- deploy-render "update: cambios menores"

# Deployment con limpieza forzada
dotnet run -- deploy-render "fix: rebuild complete"
# ? Responde 's' a la pregunta

# Solo verificar estado
dotnet run -- check-render

# Sincronizar a local
dotnet run -- sync-schema

# Limpiar BD de Render
dotnet run -- clean-render
```

---

## ?? Características

- ? **Totalmente automatizado** - Un solo comando para deploy completo
- ? **Verificación automática** - Valida BD y API después del deploy
- ? **Logs detallados** - Guarda todo en archivos con timestamp
- ? **Colores en consola** - Fácil de leer el progreso
- ? **Manejo de errores** - Mensajes claros y soluciones sugeridas
- ? **Sin configuración** - Datos de Render hardcoded
- ? **Seguridad** - Opción de limpieza requiere confirmación explícita

---

## ????? Autor

Desarrollado para el proyecto GestionTime API

**Repositorio**: https://github.com/jakkey1967-dotcom/GestionTimeApi

**Render URL**: https://gestiontime-api.onrender.com

---

## ?? Última Actualización

31 de Diciembre de 2024

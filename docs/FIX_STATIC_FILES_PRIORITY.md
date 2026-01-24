# 🔧 Fix: Prioridad de Archivos Estáticos Multi-Tenant

## ⚠️ Problema Identificado

**Síntoma:**
```
HTTP GET /images/LogoOscuro.png respondió 304 en 3.4169 ms
```

La aplicación estaba sirviendo la imagen desde `wwwroot/` (carpeta por defecto) en lugar de `wwwroot-pss_dvnx/` (carpeta específica del cliente).

**Causa Raíz:**
El middleware de archivos estáticos estaba configurado **solo para el cliente específico**, sin un fallback explícito al directorio común. Además, el navegador hacía **cache (304)** de la imagen anterior.

---

## ✅ Solución Aplicada

### Configuración Anterior (Incorrecta)

```csharp
// ❌ SOLO un middleware (sin fallback)
if (clientConfigService.HasClientSpecificWwwroot())
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(clientWwwroot),
        RequestPath = ""
    });
}
else
{
    app.UseStaticFiles();  // Solo si NO hay cliente específico
}
```

**Problema:**
- Si el archivo **no existe** en `wwwroot-pss_dvnx/`, no hay fallback
- El `else` solo se ejecuta si **NO hay** cliente específico configurado
- No hay segundo intento de búsqueda

---

### Configuración Nueva (Correcta)

```csharp
// ✅ DOS middlewares en cadena (con fallback)

// 1️⃣ PRIMERO: Archivos del cliente (prioridad alta)
if (clientConfigService.HasClientSpecificWwwroot())
{
    var clientWwwroot = clientConfigService.GetClientWwwrootPath();
    var clientWwwrootFullPath = Path.GetFullPath(clientWwwroot);
    
    Log.Information("  1️⃣ Prioridad: {ClientPath} (cliente específico)", clientWwwrootFullPath);
    
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(clientWwwrootFullPath),
        RequestPath = "",
        OnPrepareResponse = ctx =>
        {
            Log.Debug("Sirviendo desde cliente: {Path}", ctx.File.PhysicalPath);
        }
    });
    
    Log.Information("  2️⃣ Fallback: wwwroot (archivos comunes)");
}

// 2️⃣ SIEMPRE: Archivos comunes (fallback)
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        Log.Debug("Sirviendo desde común: {Path}", ctx.File.PhysicalPath);
    }
});
```

**Ventajas:**
- ✅ **Prioridad:** Busca primero en `wwwroot-pss_dvnx/`
- ✅ **Fallback:** Si no encuentra, busca en `wwwroot/`
- ✅ **Logs:** Muestra desde dónde se sirve cada archivo
- ✅ **Compatible:** Funciona con y sin cliente específico

---

## 🎯 Funcionamiento

### Ejemplo 1: Archivo en Cliente Específico

```
Solicitud: GET /images/pss_dvnx_logo.png

1. Middleware 1 (cliente): Busca en wwwroot-pss_dvnx/images/pss_dvnx_logo.png
   ✅ ENCONTRADO → Servir desde cliente específico
   
Log: "Sirviendo desde cliente: C:\...\wwwroot-pss_dvnx\images\pss_dvnx_logo.png"
```

### Ejemplo 2: Archivo Solo en Común

```
Solicitud: GET /images/LogoOscuro.png

1. Middleware 1 (cliente): Busca en wwwroot-pss_dvnx/images/LogoOscuro.png
   ❌ NO ENCONTRADO → Continuar al siguiente middleware
   
2. Middleware 2 (común): Busca en wwwroot/images/LogoOscuro.png
   ✅ ENCONTRADO → Servir desde común
   
Log: "Sirviendo desde común: C:\...\wwwroot\images\LogoOscuro.png"
```

### Ejemplo 3: Archivo en Ambos (Prioridad)

```
Solicitud: GET /images/logo.png

1. Middleware 1 (cliente): Busca en wwwroot-pss_dvnx/images/logo.png
   ✅ ENCONTRADO → Servir desde cliente específico (prioridad)
   
(Middleware 2 nunca se ejecuta porque ya se encontró)

Log: "Sirviendo desde cliente: C:\...\wwwroot-pss_dvnx\images\logo.png"
```

---

## 📊 Logs Esperados al Arrancar

```
🔧 Verificando estado de base de datos...
✅ Conexión a BD establecida
✅ Base de datos actualizada (sin migraciones pendientes)
🚀 Ejecutando seed de base de datos...
ℹ️  Usuario admin ya existe, omitiendo seed
✅ Seed completado exitosamente

Configurando archivos estáticos:
  1️⃣ Prioridad: C:\GestionTime\GestionTimeApi\wwwroot-pss_dvnx (cliente específico)
  2️⃣ Fallback: wwwroot (archivos comunes)

GestionTime API iniciada correctamente en puerto 5000
```

**Al solicitar archivos:**
```
[DEBUG] Sirviendo desde cliente: C:\...\wwwroot-pss_dvnx\images\pss_dvnx_logo.png
[DEBUG] Sirviendo desde común: C:\...\wwwroot\images\LogoOscuro.png
```

---

## 🧪 Pruebas

### Test 1: Archivo Específico del Cliente

```bash
# Solicitar logo del cliente
curl http://localhost:5000/images/pss_dvnx_logo.png

# Resultado esperado:
# - Status: 200 OK
# - Content-Type: image/png
# - Archivo desde: wwwroot-pss_dvnx/images/pss_dvnx_logo.png
```

### Test 2: Archivo Común (Fallback)

```bash
# Solicitar logo por defecto
curl http://localhost:5000/images/LogoOscuro.png

# Resultado esperado:
# - Status: 200 OK
# - Content-Type: image/png
# - Archivo desde: wwwroot/images/LogoOscuro.png
```

### Test 3: Archivo No Existe

```bash
# Solicitar archivo inexistente
curl http://localhost:5000/images/noexiste.png

# Resultado esperado:
# - Status: 404 Not Found
```

---

## 🔍 Verificación en Navegador

### Antes del Fix (Incorrecto)

```
GET /images/LogoOscuro.png
Status: 304 Not Modified (desde cache)
Servido desde: wwwroot/images/LogoOscuro.png
```

**Problema:** Siempre usaba el archivo por defecto

### Después del Fix (Correcto)

1. **Limpiar cache del navegador:**
   - Chrome: F12 → Network → "Disable cache"
   - O usar modo incógnito

2. **Refrescar página:**
   ```
   GET /images/pss_dvnx_logo.png
   Status: 200 OK
   Servido desde: wwwroot-pss_dvnx/images/pss_dvnx_logo.png
   ```

3. **Verificar en HTML:**
   ```html
   <!-- Endpoint raíz (/) -->
   <img src="/images/pss_dvnx_logo.png" alt="Logo" />
   ```

---

## 📝 Estructura de Archivos

```
GestionTimeApi/
│
├── wwwroot/                          # ✅ Archivos COMUNES (todos los clientes)
│   ├── images/
│   │   └── LogoOscuro.png           # Logo por defecto
│   └── favicon.ico
│
└── wwwroot-pss_dvnx/                 # ✅ Archivos ESPECÍFICOS (PSS DVNX)
    └── images/
        ├── pss_dvnx_logo.png        # Logo del cliente
        └── banner.jpg               # Banner personalizado
```

**Prioridad de resolución:**
```
Solicitud → wwwroot-pss_dvnx/ → (si no existe) → wwwroot/ → 404
```

---

## 🎓 Para Agregar Más Clientes

### Cliente ABC

1. **Crear carpeta:**
   ```bash
   mkdir wwwroot-cliente_abc
   mkdir wwwroot-cliente_abc/images
   ```

2. **Agregar archivos:**
   ```bash
   cp logo_abc.png wwwroot-cliente_abc/images/logo.png
   ```

3. **Configurar en clients.config.json:**
   ```json
   {
     "CurrentClient": "cliente_abc",
     "Clients": {
       "cliente_abc": {
         "Id": "cliente_abc",
         "Name": "Cliente ABC",
         "WwwrootPath": "wwwroot-cliente_abc"
       }
     }
   }
   ```

4. **Reiniciar aplicación:**
   ```
   Configurando archivos estáticos:
     1️⃣ Prioridad: wwwroot-cliente_abc (cliente específico)
     2️⃣ Fallback: wwwroot (archivos comunes)
   ```

---

## 🚀 Deployment

### Render.com / Docker

**Verificar que se copien ambas carpetas:**

```dockerfile
# Dockerfile
COPY wwwroot/ ./wwwroot/
COPY wwwroot-pss_dvnx/ ./wwwroot-pss_dvnx/
```

**Logs de deploy:**
```
Building...
Copying wwwroot/
Copying wwwroot-pss_dvnx/
Starting application...
Configurando archivos estáticos:
  1️⃣ Prioridad: /app/wwwroot-pss_dvnx
  2️⃣ Fallback: /app/wwwroot
```

---

## ⚠️ Notas Importantes

### Cache del Navegador

**Problema:** El navegador puede cachear la imagen anterior

**Solución:**
```
1. Limpiar cache: Ctrl+F5 (hard refresh)
2. O agregar query param: /images/logo.png?v=2
3. O deshabilitar cache en DevTools
```

### Orden de Middlewares

**IMPORTANTE:** El orden importa:

```csharp
// ✅ CORRECTO
app.UseStaticFiles(clientSpecific);  // Primero cliente
app.UseStaticFiles(common);          // Luego común

// ❌ INCORRECTO
app.UseStaticFiles(common);          // Siempre encuentra aquí
app.UseStaticFiles(clientSpecific);  // Nunca llega aquí
```

### Logs de Debug

Los logs `Debug` solo aparecen si el nivel de log es `Debug`:

```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"  // ← Habilitar logs detallados
    }
  }
}
```

---

## ✅ Checklist de Verificación

- [x] Middleware cliente configurado correctamente
- [x] Middleware común siempre presente (fallback)
- [x] Logs informativos al arrancar
- [x] Logs debug al servir archivos
- [x] Ambas carpetas incluidas en `.csproj`
- [x] Ambas carpetas copiadas al build
- [x] Cache del navegador limpiado
- [x] Documentación actualizada

---

## 📚 Documentación Relacionada

- `ARCHIVOS_ESTATICOS_MULTI_TENANT.md` - Sistema completo
- `FIX_WWWROOT_VISIBILITY.md` - Visibilidad en VS
- `clients.config.json` - Configuración de clientes

---

**🎉 Problema resuelto: Ahora sirve archivos con prioridad correcta!**

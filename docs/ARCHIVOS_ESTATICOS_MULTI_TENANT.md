# 📁 Sistema de Archivos Estáticos Multi-Tenant

## 📋 Descripción

El sistema soporta **archivos estáticos específicos por cliente** mediante carpetas `wwwroot-{clientId}`. Esto permite personalizar logos, imágenes y otros recursos según el cliente activo.

---

## 🗂️ Estructura de Carpetas

```
GestionTimeApi/
├── wwwroot/                    # ✅ Archivos comunes (todos los clientes)
│   ├── images/
│   │   └── LogoOscuro.png     # Logo por defecto
│   └── ...
│
└── wwwroot-pss_dvnx/           # ✅ Archivos específicos de PSS DVNX
    └── images/
        ├── .gitkeep
        ├── pss_dvnx_logo.png
        └── pss_dvnx_logo.png.png
```

---

## 🎯 ¿Cómo Funciona?

### **1. Configuración del Cliente**

El sistema determina el cliente activo mediante el `ClientConfigurationService`:

```csharp
// Archivo: clients.config.json
{
  "CurrentClient": "pss_dvnx",  // ← Cliente activo
  "Clients": {
    "pss_dvnx": {
      "Id": "pss_dvnx",
      "Name": "GestionTime Global-retail.com",
      "WwwrootPath": "wwwroot-pss_dvnx"  // ← Carpeta específica
    }
  }
}
```

### **2. Servicio de Archivos Estáticos**

En `Program.cs` (líneas 536-551):

```csharp
// Servir archivos estáticos según el cliente usando servicio centralizado
var clientConfigService = app.Services.GetRequiredService<ClientConfigurationService>();

if (clientConfigService.HasClientSpecificWwwroot())
{
    var clientWwwroot = clientConfigService.GetClientWwwrootPath();
    Log.Information("Usando wwwroot específico del cliente: {Path}", clientWwwroot);
    
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(clientWwwroot),
        RequestPath = ""  // Servir en raíz (/)
    });
}
else
{
    Log.Information("Usando wwwroot común");
    app.UseStaticFiles();
}
```

### **3. Resolución de Rutas**

**Ejemplo: Logo del cliente**

```
URL solicitada: http://localhost:5000/images/logo.png

Cliente activo: pss_dvnx
Carpeta específica: wwwroot-pss_dvnx/

Ruta resuelta: wwwroot-pss_dvnx/images/logo.png
```

**Si el archivo no existe:**
- Fallback a `wwwroot/images/logo.png` (común)

---

## 📦 Archivos Incluidos en Repositorio

### **wwwroot-pss_dvnx** (Cliente PSS DVNX)

```
wwwroot-pss_dvnx/
└── images/
    ├── .gitkeep                    # Para mantener carpeta en Git
    ├── pss_dvnx_logo.png          # Logo principal
    └── pss_dvnx_logo.png.png      # Logo alternativo (revisar duplicado)
```

✅ **Estos archivos ESTÁN en el repositorio**

---

## 🔧 Agregar Nuevo Cliente

### **Paso 1: Crear carpeta wwwroot**

```bash
mkdir wwwroot-cliente_abc
mkdir wwwroot-cliente_abc/images
```

### **Paso 2: Agregar archivos del cliente**

```bash
# Copiar logo del cliente
cp logo_cliente_abc.png wwwroot-cliente_abc/images/logo.png

# Agregar otros recursos
cp favicon.ico wwwroot-cliente_abc/
```

### **Paso 3: Configurar en clients.config.json**

```json
{
  "Clients": {
    "cliente_abc": {
      "Id": "cliente_abc",
      "Name": "Cliente ABC",
      "WwwrootPath": "wwwroot-cliente_abc"
    }
  }
}
```

### **Paso 4: Activar el cliente**

```json
{
  "CurrentClient": "cliente_abc"
}
```

### **Paso 5: Agregar al repositorio**

```bash
git add wwwroot-cliente_abc/
git commit -m "feat: agregar archivos estáticos para Cliente ABC"
git push
```

---

## 🌐 Uso en Páginas HTML

### **Endpoint Raíz (/)** en `Program.cs`

```csharp
// Obtener logo del cliente actual
var logoPath = clientConfig.GetLogoPath();

var html = $@"
<div class=""header"">
    <img src=""{logoPath}"" alt=""GestionTime"" class=""logo"" 
         onerror=""this.src='/images/LogoOscuro.png'"" />
    <h1>GestionTime API</h1>
</div>";
```

**Resultado:**
- Cliente `pss_dvnx`: Usa `/images/pss_dvnx_logo.png`
- Si falla: Usa `/images/LogoOscuro.png` (fallback)

---

## 📊 Archivos Comunes vs Específicos

| Tipo de Archivo | Ubicación | Uso |
|-----------------|-----------|-----|
| **Logo por defecto** | `wwwroot/images/LogoOscuro.png` | Todos los clientes (fallback) |
| **Logo PSS DVNX** | `wwwroot-pss_dvnx/images/pss_dvnx_logo.png` | Solo cliente PSS DVNX |
| **Favicon común** | `wwwroot/favicon.ico` | Todos los clientes |
| **CSS común** | `wwwroot/css/site.css` | Todos los clientes |
| **CSS específico** | `wwwroot-{clientId}/css/custom.css` | Solo ese cliente |

---

## 🚨 Importante: Control de Versiones

### **Archivos que DEBEN estar en Git:**

✅ `wwwroot/` (carpeta común)
✅ `wwwroot-pss_dvnx/` (cliente principal)
✅ `wwwroot-{clientId}/` (otros clientes)

### **Archivos que NO deben estar en Git:**

❌ Archivos temporales: `*.tmp`, `*.bak`
❌ Archivos de configuración local: `.env`, `secrets.json`
❌ Archivos muy grandes: `*.zip`, `*.rar`, `*.mp4`

### **Verificar qué está en Git:**

```bash
# Ver archivos trackeados en wwwroot-pss_dvnx
git ls-files wwwroot-pss_dvnx

# Resultado actual:
# wwwroot-pss_dvnx/images/.gitkeep
# wwwroot-pss_dvnx/images/pss_dvnx_logo.png
# wwwroot-pss_dvnx/images/pss_dvnx_logo.png.png
```

---

## 🔍 Solución de Problemas

### **❌ Error: "No se encuentra la imagen"**

**Síntoma:** La imagen no se muestra en el navegador

**Diagnóstico:**

1. Verificar cliente activo:
   ```bash
   # Ver logs al arrancar
   [INFO] Usando wwwroot específico del cliente: wwwroot-pss_dvnx
   ```

2. Verificar que el archivo exista:
   ```bash
   Test-Path wwwroot-pss_dvnx/images/pss_dvnx_logo.png
   # Debe retornar: True
   ```

3. Verificar permisos:
   ```bash
   # La carpeta debe ser accesible por la aplicación
   icacls wwwroot-pss_dvnx
   ```

**Solución:**
- Si el archivo no existe: Copiarlo a la carpeta correcta
- Si el cliente no tiene carpeta: Crear `wwwroot-{clientId}/`
- Si no está en Git: Agregarlo con `git add`

---

### **❌ Error: "wwwroot-pss_dvnx no está en GitHub"**

**Síntoma:** La carpeta no aparece en el repositorio remoto

**Diagnóstico:**

```bash
# Verificar si está en .gitignore
cat .gitignore | Select-String "wwwroot"

# Verificar si está en staging
git ls-files wwwroot-pss_dvnx
```

**Solución:**

```bash
# Si está en .gitignore, quitarlo
# Editar .gitignore y comentar la línea:
# #wwwroot-*/

# Agregar la carpeta
git add wwwroot-pss_dvnx/

# Commit
git commit -m "feat: agregar carpeta wwwroot-pss_dvnx al repositorio"

# Push
git push origin main
```

---

### **⚠️ Advertencia: Archivo duplicado**

**Detectado:**
```
wwwroot-pss_dvnx/images/pss_dvnx_logo.png
wwwroot-pss_dvnx/images/pss_dvnx_logo.png.png  ← Duplicado
```

**Recomendación:** Eliminar el duplicado `.png.png`:

```bash
git rm wwwroot-pss_dvnx/images/pss_dvnx_logo.png.png
git commit -m "fix: eliminar logo duplicado"
git push
```

---

## 🎓 Documentación Relacionada

- **Configuración de Clientes:** `clients.config.json`
- **Servicio de Configuración:** `Services/ClientConfigurationService.cs`
- **Guía de Variables de Entorno:** `CONFIGURATION_VARIABLES_GUIDE.md`
- **Seed Automático:** `SEED_AUTOMATICO.md`

---

## ✅ Checklist de Deployment

Antes de hacer deployment a producción:

- [ ] Verificar que `wwwroot-{clientId}` esté en Git
- [ ] Confirmar que los logos existan y sean accesibles
- [ ] Probar fallback a `wwwroot/` si falla el específico
- [ ] Verificar permisos de lectura en carpetas
- [ ] Configurar `CurrentClient` correcto en `clients.config.json`
- [ ] Probar endpoint `/` para ver logo correcto
- [ ] Revisar logs: `Usando wwwroot específico del cliente: ...`

---

## 🎉 Resumen

✅ **Sistema Multi-Tenant** de archivos estáticos
✅ **Fallback automático** a archivos comunes
✅ **Carpeta wwwroot-pss_dvnx** incluida en Git
✅ **Configuración centralizada** en `ClientConfigurationService`
✅ **Fácil agregar nuevos clientes** con sus recursos propios

**¿Necesitas agregar recursos para otro cliente?** Sigue los pasos en la sección "Agregar Nuevo Cliente".

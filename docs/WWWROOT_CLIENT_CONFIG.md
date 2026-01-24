# 📁 Estructura de wwwroot por Cliente

## 🎯 **Configuración**

Cada cliente puede tener su propia carpeta `wwwroot` con assets personalizados (logos, imágenes, CSS, etc.).

## 📂 **Estructura de Directorios**

```
GestionTimeApi/
├── wwwroot/                    # Assets comunes (fallback)
│   ├── images/
│   │   └── logo-default.png
│   ├── css/
│   └── js/
│
├── wwwroot-pss_dvnx/          # Assets específicos de PSS DVNX
│   ├── images/
│   │   └── LogoOscuro.png
│   └── css/
│       └── custom.css
│
├── wwwroot-cliente_abc/       # Assets específicos de Cliente ABC
│   ├── images/
│   │   └── LogoOscuro.png
│   └── css/
│       └── custom.css
│
└── wwwroot-cliente_xyz/       # Assets específicos de Cliente XYZ
    └── images/
        └── LogoOscuro.png
```

## ⚙️ **Cómo Funciona**

### **1. Variable de Entorno**

El sistema usa `DB_SCHEMA` para identificar el cliente:

```bash
# Render.com
DB_SCHEMA=pss_dvnx
```

### **2. Selección Automática**

El middleware en `Program.cs` selecciona automáticamente:

```csharp
// 1. Intenta usar: wwwroot-pss_dvnx/
// 2. Si no existe, usa: wwwroot/ (común)
```

### **3. Transparente para la API**

Todos los endpoints siguen funcionando igual:

```html
<!-- En el HTML -->
<img src="/images/LogoOscuro.png" alt="Logo" />

<!-- Se sirve desde:
     - wwwroot-pss_dvnx/images/LogoOscuro.png (si existe)
     - wwwroot/images/LogoOscuro.png (fallback)
-->
```

## 📋 **Configurar Nuevo Cliente**

### **Paso 1: Crear Carpeta**

```bash
mkdir wwwroot-nuevo_cliente
mkdir wwwroot-nuevo_cliente/images
```

### **Paso 2: Agregar Assets**

Copia el logo y archivos del cliente:

```bash
cp logo-cliente.png wwwroot-nuevo_cliente/images/LogoOscuro.png
```

### **Paso 3: Configurar Render**

En Render Dashboard → Environment:

```
DB_SCHEMA=nuevo_cliente
```

### **Paso 4: Deploy**

```bash
git add wwwroot-nuevo_cliente/
git commit -m "feat: Add assets for nuevo_cliente"
git push origin main
```

## 🎨 **Assets Personalizables por Cliente**

### **Imágenes:**
- `images/LogoOscuro.png` - Logo principal
- `images/LogoClaro.png` - Logo alternativo
- `images/favicon.ico` - Favicon
- `images/background.jpg` - Fondo personalizado

### **Estilos:**
- `css/custom.css` - Estilos personalizados del cliente
- `css/theme.css` - Tema de colores

### **Scripts:**
- `js/analytics.js` - Analytics específico del cliente
- `js/custom.js` - Scripts personalizados

## ✅ **Ventajas**

1. ✅ **Personalización total** por cliente
2. ✅ **Fallback automático** a assets comunes
3. ✅ **Gestión simple** (una carpeta por cliente)
4. ✅ **Sin duplicación** de archivos comunes
5. ✅ **Deploy independiente** (solo cambias el cliente que necesites)

## 📝 **Ejemplo: Logo por Cliente**

### **Cliente PSS DVNX:**
```
URL: https://gestiontimeapi.onrender.com
Logo: wwwroot-pss_dvnx/images/LogoOscuro.png
```

### **Cliente ABC:**
```
URL: https://gestiontimeapi-abc.onrender.com
Logo: wwwroot-cliente_abc/images/LogoOscuro.png
```

### **Cliente sin carpeta propia:**
```
URL: https://gestiontimeapi-nuevo.onrender.com
Logo: wwwroot/images/logo-default.png (fallback)
```

## 🔄 **Flujo de Resolución**

```
1. Request: GET /images/LogoOscuro.png
2. Obtener cliente: DB_SCHEMA = "pss_dvnx"
3. Buscar: wwwroot-pss_dvnx/images/LogoOscuro.png
   ├─ ✅ Existe → Servir este archivo
   └─ ❌ No existe → Servir wwwroot/images/LogoOscuro.png
```

## 🚀 **Deploy en Render**

### **Build:**
Todos los `wwwroot-*` se incluyen automáticamente en el build.

### **Runtime:**
El middleware selecciona la carpeta correcta según `DB_SCHEMA`.

## 📦 **Estructura Recomendada**

```
wwwroot/                    # ⚠️ COMÚN - Todos los clientes
├── images/
│   └── logo-default.png   # Logo genérico
└── css/
    └── base.css           # Estilos base

wwwroot-pss_dvnx/          # ✨ ESPECÍFICO PSS DVNX
└── images/
    └── LogoOscuro.png     # Logo PSS DVNX

wwwroot-cliente_abc/       # ✨ ESPECÍFICO Cliente ABC
├── images/
│   └── LogoOscuro.png     # Logo Cliente ABC
└── css/
    └── custom.css         # Colores corporativos ABC
```

## 🛠️ **Troubleshooting**

### **El logo no se carga**

1. Verifica que `DB_SCHEMA` está configurado:
```bash
curl https://gestiontimeapi.onrender.com/health
# → { "client": "pss_dvnx", ... }
```

2. Verifica que existe la carpeta:
```bash
ls wwwroot-pss_dvnx/images/
```

3. Verifica los logs:
```
[INFO] Usando wwwroot específico del cliente: /app/wwwroot-pss_dvnx
```

### **Fallback no funciona**

Asegúrate que `wwwroot/` tiene archivos por defecto.

## 📚 **Documentación Relacionada**

- `MULTI_TENANT_INTEGRATION_GUIDE.md` - Configuración multi-tenant
- `SCHEMA_CONFIG.md` - Configuración de schemas
- `clients.config.json` - Lista de clientes configurados

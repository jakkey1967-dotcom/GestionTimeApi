# 📋 Variables de Configuración - Guía Completa

## 🎯 **Variables de Entorno Necesarias**

### **Variables Obligatorias (Production):**

```bash
# ==================== BASE DE DATOS ====================
DATABASE_URL=postgresql://user:password@host:5432/dbname
# Ejemplo Render: postgresql://gestiontime:abc123@dpg-xyz.oregon-postgres.render.com:5432/pss_dvnx

# ==================== CLIENTE/TENANT ====================
DB_SCHEMA=pss_dvnx
# Valores posibles: pss_dvnx, cliente_abc, cliente_xyz, etc.

# ==================== JWT (SEGURIDAD) ====================
JWT_SECRET_KEY=tu-clave-secreta-super-segura-256-bits-minimo-aleatoria
# Generar con: -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object {[char]$_})

# ==================== EMAIL/SMTP ====================
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=noreply@global-retail.com
SMTP_PASSWORD=tu-app-password-de-gmail

# ==================== ENTORNO ====================
ASPNETCORE_ENVIRONMENT=Production
# Valores posibles: Development, Staging, Production

# ==================== PUERTO (AUTOMÁTICO EN RENDER) ====================
PORT=10000
# En Render se asigna automáticamente
```

---

## 📝 **Valores por Defecto (clients.config.json)**

### **Defaults** (aplicados si no se especifica en cliente):

```json
{
  "Defaults": {
    "Database": {
      "Schema": "gestiontime",
      "ConnectionString": "${DATABASE_URL}"
    },
    "Jwt": {
      "Issuer": "GestionTime",
      "Audience": "GestionTime.Api",
      "Key": "${JWT_SECRET_KEY}",
      "AccessMinutes": 15,
      "RefreshDays": 14
    },
    "Cors": {
      "Origins": [
        "https://localhost:5173",
        "http://localhost:5173"
      ]
    },
    "Email": {
      "SmtpHost": "${SMTP_HOST}",
      "SmtpPort": 587,
      "SmtpUser": "${SMTP_USER}",
      "SmtpPassword": "${SMTP_PASSWORD}",
      "From": "noreply@gestiontime.app",
      "FromName": "GestionTime",
      "EnableSsl": true
    },
    "Features": {
      "RequireEmailConfirmation": true,
      "AllowSelfRegistration": false,
      "PasswordExpirationDays": 90,
      "MaxLoginAttempts": 5,
      "LockoutMinutes": 30
    },
    "Branding": {
      "CompanyName": "GestionTime",
      "PrimaryColor": "#0B8C99",
      "SecondaryColor": "#0A7A85",
      "LogoDark": "LogoOscuro.png",
      "LogoLight": "LogoClaro.png",
      "Favicon": "favicon.ico"
    },
    "ContactInfo": {
      "SupportEmail": "soporte@gestiontime.app",
      "SupportPhone": "",
      "Address": ""
    },
    "Limits": {
      "MaxUsersPerTenant": 100,
      "MaxStorageGB": 50,
      "MaxApiRequestsPerMinute": 1000
    }
  }
}
```

---

## 🔍 **Prioridad de Configuración**

El sistema busca valores en este orden:

### **1. Variables de Entorno (Mayor Prioridad)**
```
Render Dashboard → Environment → DB_SCHEMA
```

### **2. clients.config.json (Cliente Específico)**
```json
{
  "Clients": [{
    "Id": "pss_dvnx",
    "Database": { "Schema": "pss_dvnx" }
  }]
}
```

### **3. clients.config.json (Defaults)**
```json
{
  "Defaults": {
    "Database": { "Schema": "gestiontime" }
  }
}
```

### **4. appsettings.json (Fallback)**
```json
{
  "Database": {
    "Schema": "pss_dvnx"
  }
}
```

### **5. Código (Hardcoded - Última Opción)**
```csharp
?? "gestiontime"
```

---

## 🎯 **Configuración por Entorno**

### **Development (Local):**
```json
// appsettings.Development.json
{
  "Database": {
    "Schema": "pss_dvnx"
  },
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=gestiontime;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Key": "development-key-not-for-production-12345678901234567890"
  },
  "Cors": {
    "Origins": [
      "https://localhost:5173",
      "http://localhost:5173",
      "https://localhost:2501",
      "http://localhost:2500"
    ]
  },
  "Email": {
    "SmtpHost": "localhost",
    "SmtpPort": 1025,
    "SmtpUser": "test",
    "SmtpPassword": "test",
    "From": "test@localhost",
    "FromName": "GestionTime Development"
  }
}
```

**Variables de Entorno (Local):**
```bash
ASPNETCORE_ENVIRONMENT=Development
DB_SCHEMA=pss_dvnx
```

---

### **Production (Render):**

**Variables de Entorno en Render Dashboard:**
```bash
# Base de datos
DATABASE_URL=postgresql://gestiontime:abc123@dpg-xyz.oregon-postgres.render.com:5432/pss_dvnx

# Cliente
DB_SCHEMA=pss_dvnx

# Seguridad
JWT_SECRET_KEY=aB3dE6fG9hJ2kL5mN8oP1qR4sT7uV0wX3yZ6aC9bD2eF5gH8iJ1kL4mN7oP0qR3sT6u

# Email
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=noreply@global-retail.com
SMTP_PASSWORD=xyzw abcd efgh ijkl

# Entorno
ASPNETCORE_ENVIRONMENT=Production

# Puerto (automático)
PORT=10000
```

---

## 🧪 **Verificar Configuración**

### **Endpoint `/health`:**
```bash
curl https://gestiontimeapi.onrender.com/health
```

**Respuesta esperada (Production):**
```json
{
  "status": "OK",
  "timestamp": "2025-12-31T14:00:00Z",
  "service": "GestionTime API",
  "version": "1.0.0",
  "client": "GestionTime Global-retail.com",  // ✅ Nombre descriptivo
  "clientId": "pss_dvnx",                     // ✅ ID técnico
  "schema": "pss_dvnx",                       // ✅ Schema BD
  "environment": "Production",                 // ✅ Production (NO Development)
  "uptime": "0d 0h 15m 30s",
  "database": "connected",
  "configuration": {
    "jwtAccessMinutes": 15,
    "jwtRefreshDays": 14,
    "emailConfirmationRequired": true,
    "selfRegistrationAllowed": false,
    "passwordExpirationDays": 90,
    "maxUsers": 100,
    "maxStorageGB": 50,
    "corsOriginsCount": 3
  }
}
```

**Respuesta esperada (Development):**
```json
{
  "status": "OK",
  "client": "GestionTime Global-retail.com",
  "clientId": "pss_dvnx",
  "schema": "pss_dvnx",
  "environment": "Development",  // ✅ Development
  ...
}
```

---

## 🔧 **Solucionar Problemas Comunes**

### **❌ Problema: `"environment": "Development"` en Production**

**Causa:** Variable `ASPNETCORE_ENVIRONMENT` no configurada en Render

**Solución:**
```bash
# En Render Dashboard → Environment
ASPNETCORE_ENVIRONMENT=Production
```

---

### **❌ Problema: `"client": "pss_dvnx"` (ID en lugar de nombre)**

**Causa:** El servicio `ClientConfigurationService` no encuentra `clients.config.json`

**Solución:** Verificar que `clients.config.json` está incluido en el deploy:
```xml
<!-- GestionTime.Api.csproj -->
<ItemGroup>
  <Content Include="clients.config.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

---

### **❌ Problema: Error de JWT**

**Causa:** `JWT_SECRET_KEY` no configurada o muy corta

**Solución:**
```bash
# Generar clave segura (PowerShell)
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object {[char]$_})

# Configurar en Render
JWT_SECRET_KEY=resultado-del-comando-anterior
```

---

### **❌ Problema: Error de CORS**

**Causa:** Frontend no está en la lista de orígenes permitidos

**Solución:** Agregar en `clients.config.json`:
```json
{
  "Cors": {
    "Origins": [
      "https://tu-frontend.app",
      "https://gestiontime-pss-dvnx.app"
    ]
  }
}
```

---

### **❌ Problema: Error de Email**

**Causa:** Credenciales SMTP incorrectas

**Solución (Gmail):**
1. Ir a: https://myaccount.google.com/apppasswords
2. Generar "Contraseña de aplicación"
3. Usar en `SMTP_PASSWORD`

```bash
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=tu-email@gmail.com
SMTP_PASSWORD=xxxx xxxx xxxx xxxx  # App Password de Gmail
```

---

## 📊 **Tabla de Variables**

| Variable | Obligatoria | Entorno | Valor por Defecto | Ejemplo |
|----------|-------------|---------|-------------------|---------|
| `DATABASE_URL` | ✅ Sí | Todas | - | `postgresql://user:pass@host:5432/db` |
| `DB_SCHEMA` | ✅ Sí | Todas | `gestiontime` | `pss_dvnx` |
| `JWT_SECRET_KEY` | ✅ Sí | Production | (dev key) | 64 caracteres aleatorios |
| `SMTP_HOST` | ✅ Sí | Production | `localhost` | `smtp.gmail.com` |
| `SMTP_PORT` | ❌ No | Todas | `587` | `587` |
| `SMTP_USER` | ✅ Sí | Production | `test` | `noreply@global-retail.com` |
| `SMTP_PASSWORD` | ✅ Sí | Production | `test` | App Password de Gmail |
| `ASPNETCORE_ENVIRONMENT` | ⚠️ Recomendado | Todas | `Production` | `Production`, `Development` |
| `PORT` | ❌ No (Render) | Render | `10000` | Automático |

---

## 📚 **Archivos de Configuración**

### **1. `clients.config.json`** (Cliente-específico)
```
Ubicación: Raíz del proyecto
Propósito: Configuración multi-tenant centralizada
Incluye: Defaults + configuración por cliente
```

### **2. `appsettings.json`** (General)
```
Ubicación: Raíz del proyecto
Propósito: Configuración general de la aplicación
No incluir: Secretos (passwords, keys)
```

### **3. `appsettings.Development.json`** (Local)
```
Ubicación: Raíz del proyecto
Propósito: Overrides para desarrollo local
No se despliega: Solo local
```

### **4. Variables de Entorno** (Production)
```
Ubicación: Render Dashboard → Environment
Propósito: Secretos y configuración sensible
Mayor prioridad: Override todo lo demás
```

---

## 🎯 **Checklist de Configuración**

### **Desarrollo Local:**
- [ ] `appsettings.Development.json` configurado
- [ ] `DB_SCHEMA` en launchSettings.json o variable de entorno
- [ ] PostgreSQL local corriendo
- [ ] `clients.config.json` presente

### **Production (Render):**
- [ ] `DATABASE_URL` configurado
- [ ] `DB_SCHEMA` configurado
- [ ] `JWT_SECRET_KEY` configurado (64+ caracteres)
- [ ] `SMTP_*` configurados
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `clients.config.json` incluido en deploy
- [ ] Schema creado en PostgreSQL: `CREATE SCHEMA pss_dvnx;`

---

**¡Con esta guía tienes la referencia completa de todas las variables de configuración!** 📋

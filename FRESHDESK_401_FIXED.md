# 🔧 Solución: Error 401 en /api/v1/freshdesk/ping

## ❌ Problema

Al llamar a `GET /api/v1/freshdesk/ping` se obtiene:

```json
{
  "ok": false,
  "status": 401,
  "message": "❌ Error al conectar con Freshdesk",
  "error": "Credenciales inválidas (API Key incorrecta)",
  "timestamp": "2026-01-31T11:14:46.7228383Z"
}
```

**Causa:** La API Key de Freshdesk está configurada como `"DISABLED"` en los archivos de configuración.

---

## ✅ Solución Rápida

### Paso 1: Configurar Variables de Entorno

Ejecuta el script de configuración:

```powershell
.\scripts\configure-freshdesk-env.ps1
```

El script te pedirá:
1. **API Key de Freshdesk** (obtener desde Admin > API Settings en Freshdesk)
2. **Domain** (ejemplo: si tu URL es https://alterasoftware.freshdesk.com, usa "alterasoftware")

### Paso 2: Reiniciar la API

```powershell
dotnet run --project GestionTime.Api
```

### Paso 3: Verificar

```powershell
# Verificar ping
curl http://localhost:2501/api/v1/freshdesk/ping
```

Deberías ver:
```json
{
  "ok": true,
  "status": 200,
  "message": "✅ Conexión exitosa con Freshdesk",
  "agent": "tu-email@ejemplo.com",
  "timestamp": "..."
}
```

---

## 🔐 Configuración Manual (Alternativa)

### Opción 1: Variables de Entorno (Recomendado)

**PowerShell (temporal para la sesión actual):**
```powershell
$env:Freshdesk__Domain = "alterasoftware"
$env:Freshdesk__ApiKey = "TU_API_KEY_AQUI"
$env:Freshdesk__SyncEnabled = "true"
```

**PowerShell (permanente - Usuario actual):**
```powershell
[System.Environment]::SetEnvironmentVariable("Freshdesk__Domain", "alterasoftware", "User")
[System.Environment]::SetEnvironmentVariable("Freshdesk__ApiKey", "TU_API_KEY_AQUI", "User")
[System.Environment]::SetEnvironmentVariable("Freshdesk__SyncEnabled", "true", "User")
```

**Linux/Mac:**
```bash
export Freshdesk__Domain="alterasoftware"
export Freshdesk__ApiKey="TU_API_KEY_AQUI"
export Freshdesk__SyncEnabled="true"
```

### Opción 2: Archivo .env

1. Copia `.env.example` a `.env`:
```powershell
Copy-Item .env.example .env
```

2. Edita `.env` con tus credenciales reales

3. Carga las variables:
```powershell
Get-Content .env | ForEach-Object {
    if ($_ -match '^([^=]+)=(.*)$') {
        [System.Environment]::SetEnvironmentVariable($matches[1], $matches[2])
    }
}
```

---

## 📁 Archivos Modificados

### ✅ `appsettings.Development.json`
```json
"Freshdesk": {
  "Domain": "alterasoftware",
  "ApiKey": "${FRESHDESK_API_KEY}",
  "SyncIntervalHours": 24,
  "SyncEnabled": true
}
```

### ✅ `appsettings.Production.json`
```json
"Freshdesk": {
  "Domain": "${FRESHDESK_DOMAIN}",
  "ApiKey": "${FRESHDESK_API_KEY}",
  "SyncIntervalHours": 24,
  "SyncEnabled": true
}
```

### ✅ Nuevos Archivos
- `.env.example` - Plantilla de variables de entorno
- `scripts/configure-freshdesk-env.ps1` - Script de configuración interactivo

---

## 🔍 Cómo Obtener tu API Key de Freshdesk

1. Inicia sesión en tu cuenta de Freshdesk
2. Ve a **Admin** (icono de engranaje)
3. En el menú izquierdo, busca **API**
4. Copia tu **API Key**
5. Si no tienes una, haz clic en **Generate** para crear una nueva

**URL directa:** `https://TU_DOMAIN.freshdesk.com/a/admin/api`

---

## ⚠️ Seguridad

### ❌ NO hagas esto:
```json
// NO guardar la API Key directamente en appsettings.json
"ApiKey": "xzy123abc456..."
```

### ✅ SÍ haz esto:
```json
// Usar variables de entorno
"ApiKey": "${FRESHDESK_API_KEY}"
```

### 📝 Agregar al .gitignore:
```
# Archivos de configuración con credenciales
.env
appsettings.*.local.json
```

---

## 🧪 Scripts de Prueba

Una vez configurado, puedes probar:

```powershell
# Ping a Freshdesk
.\scripts\test-freshdesk-simple.ps1

# Sincronizar companies
.\scripts\test-freshdesk-companies.ps1

# Sincronizar agentes
.\scripts\test-freshdesk-agents.ps1

# Sincronizar tickets
.\scripts\test-freshdesk-sync.ps1
```

---

## 📚 Referencias

- [Freshdesk API Documentation](https://developers.freshdesk.com/api/)
- [Freshdesk Authentication](https://developers.freshdesk.com/api/#authentication)
- `docs/FRESHDESK_INTEGRATION.md` - Documentación completa de la integración

---

## ✅ Checklist de Verificación

- [ ] API Key configurada en variable de entorno
- [ ] Domain configurado correctamente
- [ ] API reiniciada después de configurar variables
- [ ] Endpoint `/api/v1/freshdesk/ping` responde 200 OK
- [ ] Variable `Freshdesk__ApiKey` NO está en appsettings.json (solo en .env)

---

**¡Problema resuelto!** 🎉

Ahora el endpoint `/api/v1/freshdesk/ping` debería funcionar correctamente.

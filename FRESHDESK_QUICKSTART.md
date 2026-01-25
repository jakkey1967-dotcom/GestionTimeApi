# 🚀 Quick Start - Testing Freshdesk

## 📝 Resumen de 3 Pasos

### 1️⃣ Configura tus credenciales

Edita `appsettings.json`:

```json
"Freshdesk": {
  "Domain": "miempresa",
  "ApiKey": "xyzABC123456789"
}
```

### 2️⃣ Inicia la API

```bash
dotnet run --project GestionTime.Api.csproj
```

### 3️⃣ Ejecuta el test automático

```powershell
.\scripts\test-freshdesk.ps1
```

---

## ✅ ¿Qué se ha agregado?

### Nuevos archivos:

1. **`Controllers/FreshdeskController.cs`** 
   - Controlador con endpoints de testing
   - 100% seguro (solo lectura)

2. **`scripts/test-freshdesk.ps1`**
   - Script de pruebas automatizado
   - Verifica toda la integración

3. **`docs/FRESHDESK_TESTING.md`**
   - Guía completa de testing
   - Troubleshooting incluido

### Configuración actualizada:

- **`appsettings.json`** - Nueva sección `Freshdesk`
- **`GestionTime.Infrastructure.csproj`** - Versión corregida de `Microsoft.Extensions.Http`

---

## 🔍 Endpoints disponibles

Después de autenticarte en Swagger (`http://localhost:2501/swagger`):

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `/api/freshdesk/test-connection` | GET | 🧪 Verificar configuración |
| `/api/freshdesk/tickets/suggest` | GET | 🎫 Buscar tickets |
| `/api/freshdesk/tags/suggest` | GET | 🏷️ Buscar tags |
| `/api/freshdesk/tags/sync` | POST | 🔄 Sincronizar tags (Admin) |

---

## 📊 Ejemplo de uso

### Test rápido con PowerShell:

```powershell
# 1. Login
$login = Invoke-RestMethod -Uri "http://localhost:2501/api/auth/login" `
  -Method POST `
  -Body (@{email="admin@gestiontime.com"; password="Admin123"} | ConvertTo-Json) `
  -ContentType "application/json" `
  -SessionVariable session

# 2. Test conexión
Invoke-RestMethod -Uri "http://localhost:2501/api/freshdesk/test-connection" `
  -Method GET `
  -WebSession $session

# 3. Buscar tickets
Invoke-RestMethod -Uri "http://localhost:2501/api/freshdesk/tickets/suggest?limit=5" `
  -Method GET `
  -WebSession $session
```

---

## ⚠️ Importante

- ✅ **Todas las operaciones son de SOLO LECTURA**
- ✅ **No se modifica nada en Freshdesk**
- ✅ **No se afectan datos de producción**
- ✅ **Logs detallados en `logs/`**

---

## 🆘 Si algo falla

1. Revisa `appsettings.json` - Credenciales correctas
2. Verifica que la API esté corriendo
3. Revisa logs en `logs/gestiontime-api.log`
4. Ejecuta `.\scripts\test-freshdesk.ps1` para diagnóstico

---

## 📚 Documentación completa

Ver `docs/FRESHDESK_TESTING.md` para:
- Guía detallada paso a paso
- Solución de problemas comunes
- Ejemplos de respuestas
- Configuración de producción

---

**¡Todo listo para probar! 🎉**

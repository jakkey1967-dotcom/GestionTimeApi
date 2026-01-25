# ⚡ ACTUALIZACIÓN RÁPIDA - SERVICIO EXISTENTE EN RENDER

**Servicio**: gestiontime-api (ya en producción)  
**URL**: https://gestiontimeapi.onrender.com  
**Acción**: Actualizar con nuevas funcionalidades

---

## 🎯 NUEVAS FUNCIONALIDADES

1. ✅ **Sistema de tags** para partes de trabajo
2. ✅ **Enriquecimiento de tickets** con datos de cliente/técnico
3. ✅ **Endpoint de detalles** completos de ticket
4. ✅ **Mejoras de seguridad** en Freshdesk

---

## 📝 PASOS (5 MINUTOS)

### 1. VERIFICAR VARIABLES EN RENDER

**Dashboard → gestiontime-api → Environment**

**Asegurar que existen estas variables de Freshdesk**:

```bash
FRESHDESK__DOMAIN=alterasoftware
FRESHDESK__APIKEY=9i1AtT08nkY1BlBmjtLk
FRESHDESK__SYNCENABLED=true
FRESHDESK__SYNCINTERVALHOURS=24
```

Si no existen, **agregarlas** (copiar/pegar).

---

### 2. COMMIT Y PUSH

```bash
# Desde el directorio del proyecto
cd C:\GestionTime\GestionTimeApi

# Agregar todos los cambios
git add .

# Commit
git commit -m "feat: Sistema de tags, mejoras Freshdesk y config Render"

# Push a main
git push origin main
```

**Render detectará el push y comenzará el deploy automáticamente.**

---

### 3. MONITOREAR DEPLOY

**Dashboard → gestiontime-api → Logs**

Buscar estos mensajes:

```
✅ Build successful
✅ Deploy live
⏳ Aplicando migraciones pendientes...
   • 20260125110057_AddPartesTagsWithFreshdeskTags
✅ Migraciones aplicadas
✅ GestionTime API iniciada correctamente
```

**Tiempo estimado**: 5-8 minutos

---

### 4. VERIFICAR (RÁPIDO)

#### A) Health Check
```bash
curl https://gestiontimeapi.onrender.com/health
# Debe responder: Healthy
```

#### B) Crear parte con tags
```bash
# Login (ajustar email/password)
curl -X POST https://gestiontimeapi.onrender.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@admin.com","password":"Admin123!"}'

# Copiar el accessToken

# Crear parte con tags (reemplazar TOKEN)
curl -X POST https://gestiontimeapi.onrender.com/api/v1/partes \
  -H "Authorization: Bearer TOKEN_AQUI" \
  -H "Content-Type: application/json" \
  -d '{
    "fecha_trabajo": "2026-01-26",
    "hora_inicio": "09:00",
    "hora_fin": "11:00",
    "id_cliente": 1,
    "accion": "Test producción",
    "tags": ["produccion", "test"]
  }'
```

Si devuelve `{ "id": 123 }` → **✅ FUNCIONA**

---

## 🎉 ¡LISTO!

El servicio se actualizó con las nuevas funcionalidades.

---

## 🔍 SI ALGO SALE MAL

### Problema: "Cannot connect to database"
**Solución**: Verificar que `DATABASE_URL` sigue configurado en Environment

### Problema: "Migración no se aplicó"
**Logs mostrarán**:
```
❌ Error applying migration
```

**Solución**: Conectar a BD y aplicar manual (ver `docs/RENDER_UPDATE_GUIDE.md`)

### Problema: "CORS blocked desde frontend"
**Solución**: Verificar variables `CORS__ORIGINS__*` en Environment

---

## 📚 MÁS INFORMACIÓN

**Guía completa**: `docs/RENDER_UPDATE_GUIDE.md`  
**Troubleshooting**: `docs/RENDER_DEPLOY_GUIDE.md` (sección 5)  
**Cambios realizados**: `docs/BACKEND_CHANGES_2026-01-25.md`

---

*Actualización: 25 de Enero de 2026*

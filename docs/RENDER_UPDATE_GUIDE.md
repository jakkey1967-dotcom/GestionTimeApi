# 🔄 GUÍA DE ACTUALIZACIÓN - DEPLOY EXISTENTE EN RENDER

**Fecha**: 25 de Enero de 2026  
**Tipo**: Actualización de servicio existente  
**Servicio**: gestiontime-api (ya en producción)

---

## ⚠️ IMPORTANTE

Esta guía es para **actualizar** el servicio que **YA ESTÁ en producción** en Render.

**NO crear un nuevo servicio** - Solo actualizar variables y código.

---

## 📋 SITUACIÓN ACTUAL

✅ **Ya existe**:
- Web Service: `gestiontime-api`
- PostgreSQL: `gestiontime-db`
- URL: `https://gestiontimeapi.onrender.com`

🆕 **Nuevas funcionalidades agregadas**:
- Sistema de tags para partes de trabajo
- Enriquecimiento de tickets con cliente/técnico
- Endpoint de detalles completos de ticket
- Mejoras de seguridad en Freshdesk

---

## 🔧 PASOS PARA ACTUALIZAR

### 1️⃣ VERIFICAR VARIABLES DE ENTORNO ACTUALES

**En Render Dashboard**:
1. Ir a: https://dashboard.render.com
2. Seleccionar: `gestiontime-api`
3. Click en: **Environment**

**Verificar que existen** (NO borrar):
```bash
✅ ASPNETCORE_ENVIRONMENT=Production
✅ DATABASE_URL=postgresql://...
✅ JWT_secret_key=...
✅ DB_SCHEMA=pss_dvnx
✅ APP__BASEURL=https://gestiontimeapi.onrender.com
```

---

### 2️⃣ AGREGAR NUEVAS VARIABLES (SI NO EXISTEN)

**Variables para Freshdesk** (verificar/agregar):

```bash
FRESHDESK__DOMAIN=alterasoftware
FRESHDESK__APIKEY=9i1AtT08nkY1BlBmjtLk
FRESHDESK__SYNCENABLED=true
FRESHDESK__SYNCINTERVALHOURS=24
```

**Variables para CORS** (verificar/agregar):

```bash
CORS__ORIGINS__0=https://gestiontime.vercel.app
CORS__ORIGINS__1=https://gestiontime.tdkportal.com
CORS__ORIGINS__2=https://gestiontimeapi.onrender.com
```

**Variables de Email** (si no están):

```bash
EMAIL__SMTPHOST=smtp.ionos.es
EMAIL__SMTPPORT=587
EMAIL__SMTPUSER=envio_noreplica@tdkportal.com
EMAIL__SMTPPASSWORD=A4gS9uV2bC5e
EMAIL__FROM=envio_noreplica@tdkportal.com
EMAIL__FROMNAME=GestionTime
```

**Variable opcional** (para habilitar endpoint manual de sync):

```bash
FRESHDESK_TAGS_SYNC_API_ENABLED=true
```

---

### 3️⃣ COMMIT Y PUSH DE NUEVOS CAMBIOS

```bash
# 1. Verificar cambios
git status

# 2. Agregar todos los archivos nuevos/modificados
git add .

# 3. Commit con mensaje descriptivo
git commit -m "feat: Sistema de tags, mejoras Freshdesk y config Render"

# 4. Push a main
git push origin main
```

**Render detectará el push automáticamente** y comenzará el deploy.

---

### 4️⃣ MONITOREAR EL DEPLOY

**En Render Dashboard** → `gestiontime-api`:

1. **Events**: Ver el nuevo deploy iniciándose
2. **Logs**: Monitorear logs en tiempo real
3. Esperar mensaje: `✅ Build successful`
4. Esperar mensaje: `✅ Deploy live`

**Logs esperados**:

```
[INFO] 🔍 Verificando base de datos...
[INFO] ✅ Base de datos 'pss_dvnx' existe
[INFO] ✅ Schema 'pss_dvnx' existe
[INFO] ⏳ Aplicando migraciones pendientes...
[INFO]    • 20260125110057_AddPartesTagsWithFreshdeskTags
[INFO] ✅ Migraciones aplicadas
[INFO] ✅ GestionTime API iniciada correctamente
```

---

### 5️⃣ VERIFICAR NUEVAS FUNCIONALIDADES

#### A) **Health Check**

```bash
curl https://gestiontimeapi.onrender.com/health
```

**Respuesta esperada**: `Healthy`

---

#### B) **Nuevas Tablas en BD**

La migración debería haber creado:
- ✅ `parte_tags` (tabla nueva)
- ✅ `freshdesk_tags` (puede ya existir, se reutiliza)

**Para verificar** (si tienes acceso a la BD):

```sql
-- Conectar a la BD de Render
SELECT table_name FROM information_schema.tables 
WHERE table_schema = 'pss_dvnx' 
  AND table_name IN ('parte_tags', 'freshdesk_tags');
```

---

#### C) **Sistema de Tags Funcionando**

```bash
# 1. Login
TOKEN=$(curl -X POST https://gestiontimeapi.onrender.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@admin.com","password":"Admin123!"}' \
  | jq -r '.accessToken')

# 2. Crear parte con tags
curl -X POST https://gestiontimeapi.onrender.com/api/v1/partes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fecha_trabajo": "2026-01-26",
    "hora_inicio": "09:00",
    "hora_fin": "11:00",
    "id_cliente": 1,
    "accion": "Test producción - Sistema de tags",
    "tags": ["produccion", "render", "test"]
  }'

# Respuesta esperada: { "id": 123 }
```

---

#### D) **Endpoint de Detalles de Ticket**

```bash
# Obtener detalles de un ticket
curl https://gestiontimeapi.onrender.com/api/v1/freshdesk/tickets/55950/details \
  -H "Authorization: Bearer $TOKEN"
```

**Respuesta esperada**: JSON con `requester`, `company`, `conversations`

---

#### E) **Tickets Enriquecidos con Cliente/Técnico**

```bash
# Buscar tickets
curl https://gestiontimeapi.onrender.com/api/v1/freshdesk/tickets/suggest-filtered?limit=5 \
  -H "Authorization: Bearer $TOKEN"
```

**Verificar que incluye**:
- `clientName`
- `technicianName`
- `tags`

---

### 6️⃣ VERIFICAR SWAGGER

Abrir: https://gestiontimeapi.onrender.com/swagger

**Nuevos endpoints visibles**:
- ✅ `PUT /api/v1/partes/{id}/tags`
- ✅ `GET /api/v1/tags/suggest`
- ✅ `GET /api/v1/freshdesk/tickets/suggest-filtered`
- ✅ `GET /api/v1/freshdesk/tickets/{ticketId}/details`

---

## 🔍 TROUBLESHOOTING

### ❌ Problema: "Migraciones no se aplican"

**Solución**:

```bash
# Opción 1: Forzar aplicación desde Render Shell (si está habilitado)
dotnet ef database update --project GestionTime.Infrastructure

# Opción 2: Conectar a BD y aplicar SQL manual
# Ver: docs/RENDER_DEPLOY_GUIDE.md sección "Aplicar Migraciones"
```

---

### ❌ Problema: "Error 500 al crear parte con tags"

**Causa**: Tabla `parte_tags` no existe

**Verificar en logs**:
```
⏳ Aplicando migraciones pendientes...
✅ Migraciones aplicadas
```

Si no aparece, la migración no se aplicó.

---

### ❌ Problema: "CORS blocked"

**Solución**: Verificar que las variables `CORS__ORIGINS__*` están configuradas

**Agregar** (si falta):
```bash
CORS__ORIGINS__0=https://gestiontime.vercel.app
CORS__ORIGINS__1=https://gestiontime.tdkportal.com
```

Luego: **Restart** el servicio en Render.

---

### ❌ Problema: "Freshdesk tags no se sincronizan"

**Verificar variables**:
```bash
FRESHDESK__DOMAIN=alterasoftware
FRESHDESK__APIKEY=9i1AtT08nkY1BlBmjtLk
FRESHDESK__SYNCENABLED=true
```

**Ver logs del Background Service**:
```
[INFO] 🔄 Iniciando sincronización de tags de Freshdesk...
[INFO] ✅ Sincronización completada: 125 tags
```

---

## 📊 CHECKLIST DE ACTUALIZACIÓN

### Pre-Deploy
- [x] ✅ Código con nuevas funcionalidades
- [x] ✅ Migraciones creadas
- [x] ✅ Dockerfile actualizado
- [x] ✅ Health check agregado
- [ ] ⏳ Commit de cambios
- [ ] ⏳ Push a main

### Durante Deploy
- [ ] ⏳ Monitorear logs en Render
- [ ] ⏳ Verificar que no hay errores
- [ ] ⏳ Esperar "Deploy live"

### Post-Deploy
- [ ] ⏳ Health check responde
- [ ] ⏳ Migraciones aplicadas
- [ ] ⏳ Nuevos endpoints funcionan
- [ ] ⏳ Sistema de tags operativo
- [ ] ⏳ Freshdesk enriquecimiento funciona
- [ ] ⏳ Frontend conecta correctamente

---

## 🎯 ROLLBACK (SI ALGO SALE MAL)

**En Render Dashboard**:

1. Ir a: **Deploys** (en el menú lateral)
2. Buscar el deploy anterior (exitoso)
3. Click en: **Redeploy**

Esto revertirá al código anterior sin las nuevas funcionalidades.

---

## 📞 SOPORTE

**Si hay problemas**:

1. **Ver logs completos**: Dashboard → Logs
2. **Revisar variables**: Dashboard → Environment
3. **Estado de BD**: Dashboard → PostgreSQL → Metrics
4. **Documentación**: `docs/RENDER_DEPLOY_GUIDE.md`
5. **Cambios realizados**: `docs/BACKEND_CHANGES_2026-01-25.md`

---

## ✅ CONFIRMACIÓN DE ÉXITO

El deploy es exitoso si:

- [x] ✅ Health check: `Healthy`
- [x] ✅ Swagger carga correctamente
- [x] ✅ Login funciona
- [x] ✅ Se puede crear parte con tags
- [x] ✅ Tags se guardan en BD
- [x] ✅ Tickets de Freshdesk muestran `clientName` y `technicianName`
- [x] ✅ Endpoint `/tickets/{id}/details` funciona
- [x] ✅ Frontend conecta sin errores CORS

---

**¡Listo para actualizar!** 🚀

**Siguiente paso**: `git commit && git push origin main`

---

*Última actualización: 25 de Enero de 2026*  
*GestionTime API - Actualización de Deploy Existente*

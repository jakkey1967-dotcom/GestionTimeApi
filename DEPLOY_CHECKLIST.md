# ✅ CHECKLIST DE DEPLOY EN RENDER

## PRE-DEPLOY

### 1. Configuración Local

- [ ] Código compilado sin errores: `dotnet build -c Release`
- [ ] Migraciones creadas y verificadas
- [ ] Tests pasando (si existen)
- [ ] Dockerfile actualizado y funcional
- [ ] `.gitignore` configurado correctamente
- [ ] Cambios commiteados en Git
- [ ] Push a branch `main`

### 2. Archivos Verificados

- [ ] `Dockerfile` existe en raíz del proyecto
- [ ] `appsettings.Production.json` sin secretos hardcodeados
- [ ] `Program.cs` tiene endpoint `/health`
- [ ] `GestionTime.sln` en raíz del proyecto

### 3. Variables de Entorno Preparadas

- [ ] `JWT_secret_key` generada (64+ caracteres)
- [ ] `DATABASE_URL` obtenida de Render PostgreSQL
- [ ] `FRESHDESK__APIKEY` verificada
- [ ] CORS origins listados
- [ ] Email SMTP configurado

---

## CONFIGURACIÓN EN RENDER

### 4. Crear Servicio Web

- [ ] Login en https://dashboard.render.com
- [ ] Click en **New → Web Service**
- [ ] Conectar repositorio: `jakkey1967-dotcom/GestionTimeApi`
- [ ] Branch: `main`
- [ ] Runtime: **Docker**
- [ ] Region: **Frankfurt (EU Central)**

### 5. Configurar Base de Datos

Opción A: PostgreSQL de Render (Recomendado)
- [ ] New → PostgreSQL
- [ ] Name: `gestiontime-db`
- [ ] Region: **Frankfurt** (mismo que API)
- [ ] Plan: **Free** o **Starter**
- [ ] Copiar **Internal Database URL**

Opción B: Base de Datos Externa
- [ ] Tener string de conexión lista
- [ ] Verificar acceso desde IPs de Render

### 6. Variables de Entorno

Ir a: Dashboard → Tu servicio → Environment

Copiar de `render.env.template` y configurar:

**CRÍTICAS (sin estas falla el deploy)**:
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `DATABASE_URL=postgresql://...`
- [ ] `JWT_secret_key=...`
- [ ] `DB_SCHEMA=pss_dvnx`

**IMPORTANTES**:
- [ ] `APP__BASEURL=https://gestiontimeapi.onrender.com`
- [ ] `CORS__ORIGINS__0=...`
- [ ] `CORS__ORIGINS__1=...`
- [ ] `EMAIL__*` (todas las variables de email)

**FRESHDESK**:
- [ ] `FRESHDESK__DOMAIN=alterasoftware`
- [ ] `FRESHDESK__APIKEY=...`
- [ ] `FRESHDESK__SYNCENABLED=true`
- [ ] `FRESHDESK__SYNCINTERVALHOURS=24`

### 7. Configuración del Servicio

**Settings**:
- [ ] Auto-Deploy: **Enabled** (deploy automático en push)
- [ ] Build Command: (vacío - usa Dockerfile)
- [ ] Start Command: (vacío - usa Dockerfile)

**Health & Alerts**:
- [ ] Health Check Path: `/health`
- [ ] Notificaciones por email: **Enabled**

### 8. Iniciar Deploy

- [ ] Click en **Manual Deploy → Deploy latest commit**
- [ ] Esperar que termine el build (5-10 min)
- [ ] Verificar logs durante el deploy

---

## POST-DEPLOY

### 9. Verificación Básica

- [ ] Servicio está **Live** en Dashboard
- [ ] Logs no muestran errores críticos
- [ ] Health check: `curl https://gestiontimeapi.onrender.com/health`
  - Respuesta esperada: `Healthy`
- [ ] Swagger accesible: `https://gestiontimeapi.onrender.com/swagger`

### 10. Verificación de Funcionalidades

**Auth**:
```bash
curl -X POST https://gestiontimeapi.onrender.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@admin.com","password":"Admin123!"}'
```
- [ ] Login exitoso
- [ ] Token recibido

**Base de Datos**:
- [ ] Migraciones aplicadas (ver logs)
- [ ] Tablas creadas correctamente
- [ ] Usuario admin existe

**Freshdesk**:
```bash
# Con el token del login anterior
curl https://gestiontimeapi.onrender.com/api/v1/freshdesk/tickets/suggest?limit=5 \
  -H "Authorization: Bearer <TOKEN>"
```
- [ ] Tickets se obtienen correctamente
- [ ] No hay errores 429 (rate limit)

**Partes con Tags**:
```bash
curl -X POST https://gestiontimeapi.onrender.com/api/v1/partes \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "fecha_trabajo": "2026-01-25",
    "hora_inicio": "09:00",
    "hora_fin": "11:00",
    "id_cliente": 1,
    "accion": "Test deploy Render",
    "tags": ["render", "deploy", "test"]
  }'
```
- [ ] Parte creado exitosamente
- [ ] Tags guardadas en `freshdesk_tags`
- [ ] Relación en `parte_tags` creada

### 11. Logs y Monitoreo

- [ ] Revisar logs en tiempo real (primeros 30 min)
- [ ] No hay warnings críticos
- [ ] Memoria y CPU dentro de límites
- [ ] Health checks pasando cada 30s

### 12. Configuración Frontend

- [ ] Actualizar URL de API en frontend a:
  ```
  https://gestiontimeapi.onrender.com
  ```
- [ ] Verificar CORS funciona desde frontend
- [ ] Login funciona desde UI
- [ ] Todas las funcionalidades operativas

---

## TROUBLESHOOTING

### Si el deploy falla:

1. **Verificar logs en Render**:
   - Dashboard → Logs
   - Buscar líneas con `ERR` o `FTL`

2. **Errores comunes**:

   **"Application failed to start"**:
   - [ ] Verificar `DATABASE_URL` está configurado
   - [ ] Verificar `JWT_secret_key` tiene 32+ chars
   - [ ] Ver logs completos del error

   **"Cannot connect to database"**:
   - [ ] Verificar formato de `DATABASE_URL`
   - [ ] Usar **Internal Database URL** (no External)
   - [ ] BD y API en misma región

   **"Port binding failed"**:
   - [ ] Dockerfile usa `ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}`
   - [ ] No hay puerto hardcodeado

3. **Reiniciar servicio**:
   - Dashboard → Manual Deploy → Clear build cache & deploy

4. **Contactar soporte**:
   - Render Support: https://render.com/support
   - GitHub Issues del proyecto

---

## MANTENIMIENTO

### Actualizaciones Futuras

- [ ] Push a `main` → Deploy automático
- [ ] Monitorear logs después de cada deploy
- [ ] Verificar health checks pasan

### Backup

- [ ] Render PostgreSQL tiene backups automáticos
- [ ] Verificar en: Dashboard → PostgreSQL → Backups
- [ ] Frecuencia: Diaria (plan Free) o configurable (paid)

### Monitoreo Continuo

- [ ] Configurar alertas por email
- [ ] Revisar métricas cada semana:
  - Uptime
  - Response time
  - Error rate
  - Uso de recursos

---

## ✅ DEPLOY COMPLETADO

Si todos los checkmarks están marcados, el deploy está completo y operativo.

**URL de la API**: https://gestiontimeapi.onrender.com

**Swagger**: https://gestiontimeapi.onrender.com/swagger

**Dashboard Render**: https://dashboard.render.com

**Documentación completa**: `docs/RENDER_DEPLOY_GUIDE.md`

---

*Última actualización: 25 de Enero de 2026*

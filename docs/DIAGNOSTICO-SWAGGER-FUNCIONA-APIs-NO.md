# 🔍 DIAGNÓSTICO: SWAGGER FUNCIONA PERO APIs NO RESPONDEN

**Fecha:** 2025-12-28  
**Deployment:** dep-d58iriruibrs73aom3h0  
**Estado:** 🚨 **ERROR 500 - FALLO INTERNO DEL SERVIDOR**

---

## 🚨 ERROR CONFIRMADO

### Error en Login Endpoint

**Request URL:**
```
https://gestiontimeapi.onrender.com/api/v1/auth/login
```

**Server Response:**
```
Code: 500
Error: response status is 500
Status: Undocumented
```

**Headers:**
```
alt-svc: h3=":443"; ma=86400
cf-cache-status: DYNAMIC
cf-ray: 9b51a218facfe54c-MAD
content-length: 0
date: Sun,28 Dec 2025 14:09:02 GMT
rndr-id: 2e25d609-1136-4c11
server: Cloudflare
x-render-origin-server: Kestrel
```

---

## 🔍 CAUSAS PROBABLES DEL ERROR 500

### 1️⃣ **Base de Datos No Conectada** (80% probable)

**Síntomas:**
- Error 500 en todos los endpoints que requieren BD
- Health check puede funcionar (no requiere BD)
- Swagger funciona (solo UI estática)

**Causa:**
- `DATABASE_URL` no configurada correctamente
- Contraseña de PostgreSQL incorrecta
- Migraciones no aplicadas

**Verificación en Runtime Logs:**
```
[ERR] Npgsql.PostgresException: password authentication failed
[ERR] No se puede conectar a la base de datos
[ERR] Error durante las migraciones
```

---

### 2️⃣ **Usuario No Existe en Base de Datos** (10% probable)

**Síntomas:**
- Error 500 en login
- Otros endpoints pueden funcionar

**Causa:**
- El seed no se ejecutó correctamente
- Usuario no existe en la base de datos

**Verificación en Runtime Logs:**
```
[INF] Seed completado
[INF] - Usuarios: 0  ← PROBLEMA
```

---

### 3️⃣ **Error en el Código de Autenticación** (10% probable)

**Síntomas:**
- Error 500 solo en endpoints de autenticación
- Otros endpoints funcionan

**Causa:**
- Error en `AuthController`
- Error en `IAuthService.LoginAsync()`
- Problema con JWT o hashing de contraseñas

---

## 🎯 SOLUCIÓN INMEDIATA

### ⚠️ NECESITO RUNTIME LOGS PARA DIAGNOSTICAR

**Por favor sigue estos pasos:**

### 1️⃣ Accede a Runtime Logs

**URL:**
```
https://dashboard.render.com/web/srv-d58hr8juibrs73ao1o50/logs
```

**Importante:**
- En el dropdown superior, selecciona **"Runtime"** (NO "Deploy")
- Busca líneas con `[ERR]` o `[WRN]`

### 2️⃣ Copia los logs

**Necesito las últimas 30-50 líneas, especialmente:**
- Líneas que empiecen con `[ERR]`
- Líneas que empiecen con `[WRN]`
- Líneas que mencionen "database", "connection", "migration", "seed"

### 3️⃣ Ejemplo de lo que busco:

**Si el problema es la base de datos:**
```
[ERR] Npgsql.PostgresException: password authentication failed for user "gestiontime"
[ERR] Unable to connect to database
```

**Si el problema es el seed:**
```
[INF] Iniciando seed de datos...
[ERR] Error al crear usuarios
[INF] Seed completado: 0 usuarios creados
```

**Si el problema es JWT:**
```
[ERR] System.ArgumentNullException: Value cannot be null. (Parameter 'key')
[ERR] JWT_SECRET_KEY not configured
```

---

## 📋 VARIABLES DE ENTORNO A VERIFICAR

**En Render Dashboard → Environment, debes tener:**

```
DATABASE_URL=postgresql://gestiontime:BvCDRFguh9SljJJUZOzGpdvpxgf18qnI@dpg-d57tobm3jp1c73b6i4ug-a.frankfurt-postgres.render.com:5432/pss_dvnx?sslmode=require

JWT_SECRET_KEY=v7ZpQ9mL3H2kN8xR1aT6yW4cE0sB5dU9jF2hK7nP3qL8rM1tX6zA4gS9uV2bC5e

ASPNETCORE_ENVIRONMENT=Production

SMTP_HOST=smtp.ionos.es
SMTP_PORT=587
SMTP_USER=envio_noreplica@tdkportal.com
SMTP_PASSWORD=Nimda2008@2020
SMTP_FROM=envio_noreplica@tdkportal.com
```

---

## 🎯 ESTADO ACTUAL

- ✅ Deployment exitoso
- ✅ Swagger funcionando
- ✅ Health check funciona (`Healthy`)
- ❌ **Login endpoint falla con error 500**
- ⏳ **Esperando Runtime Logs para diagnóstico preciso**

---

## 🚀 PRÓXIMOS PASOS

1. ✅ **Acceder a Runtime Logs** (link arriba)
2. ✅ **Copiar líneas con [ERR]**
3. ✅ **Compartir los logs aquí**
4. ⏳ **Aplicar la solución específica**

---

**Última actualización:** 2025-12-28 15:45  
**Estado:** 🚨 Error 500 confirmado - Necesitamos Runtime Logs para diagnóstico exacto

---

## 📞 ACCESO RÁPIDO A LOGS

**Dashboard del Servicio:**
https://dashboard.render.com/web/srv-d58hr8juibrs73ao1o50

**Logs en vivo:**
https://dashboard.render.com/web/srv-d58hr8juibrs73ao1o50/logs

**⚠️ IMPORTANTE:** Selecciona **"Runtime"** en el dropdown de logs (no "Deploy")

postgresql://gestiontime:BvCDRFguh9SljJJUZOzGpdvpxgf18qnI@dpg-d57tobm3jp1c73b6i4ug-a.frankfurt-postgres.render.com:5432/pss_dvnx?sslmode=require
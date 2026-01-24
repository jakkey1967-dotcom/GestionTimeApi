# ? CHECKLIST DE CONFIGURACIÓN - RENDER.COM

## ?? PASOS PARA CONFIGURAR VARIABLES DE ENTORNO

### Paso 1: Preparar las variables

- [ ] Abrir el archivo `.env.render.template`
- [ ] Revisar todas las variables
- [ ] Decidir si generar nuevas claves JWT (recomendado)

### Paso 2: Generar claves seguras (OPCIONAL pero RECOMENDADO)

```bash
# Generar nueva JWT_SECRET_KEY
openssl rand -base64 64
```

**O en Windows PowerShell:**
```powershell
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

- [ ] Copiar la clave generada
- [ ] Reemplazar el valor de `JWT_SECRET_KEY` en `.env.render.template`

### Paso 3: Configurar en Render

#### Método A: Importar desde archivo (RÁPIDO)

1. [ ] Ir a https://dashboard.render.com
2. [ ] Seleccionar tu servicio: **gestiontime-api**
3. [ ] Ir a la pestaña **Environment**
4. [ ] Hacer clic en **"Add from .env"**
5. [ ] Copiar TODO el contenido de `.env.render.template`
6. [ ] Pegar en el cuadro de texto de Render
7. [ ] Hacer clic en **"Save Changes"**

#### Método B: Agregar manualmente (LENTO)

1. [ ] Ir a https://dashboard.render.com
2. [ ] Seleccionar tu servicio: **gestiontime-api**
3. [ ] Ir a la pestaña **Environment**
4. [ ] Hacer clic en **"Add Environment Variable"**

Agregar las siguientes variables:

- [ ] `JWT_SECRET_KEY` = `v7ZpQ9mL3H2kN8xR1aT6yW4cE0sB5dU9jF2hK7nP3qL8rM1tX6zA4gS9uV2bC5e`
- [ ] `DATABASE_URL` = `postgresql://gestiontime:...`
- [ ] `SMTP_HOST` = `smtp.ionos.es`
- [ ] `SMTP_PORT` = `587`
- [ ] `SMTP_USER` = `envio_noreplica@tdkportal.com`
- [ ] `SMTP_PASSWORD` = `Nimda2008@2020`
- [ ] `SMTP_FROM` = `envio_noreplica@tdkportal.com`
- [ ] `ASPNETCORE_ENVIRONMENT` = `Production`

### Paso 4: Limpiar variables innecesarias (OPCIONAL)

Si tienes variables que NO aparecen en la lista de arriba:

- [ ] Revisar si `SECURITY_KEY` está configurada
  - Si SÍ y no la necesitas ? Eliminarla
  - Si SÍ y es la clave JWT ? Renombrarla a `JWT_SECRET_KEY`
  - Si NO existe ? No hacer nada

- [ ] Revisar si hay otras variables viejas o duplicadas

### Paso 5: Hacer deployment

- [ ] Hacer clic en **"Manual Deploy"** ? **"Deploy latest commit"**
- [ ] Esperar a que termine el deployment (puede tardar 5-10 minutos)

### Paso 6: Verificar deployment

#### En los logs de Render:

- [ ] Abrir la pestaña **"Logs"** del servicio
- [ ] Buscar el mensaje: `Iniciando GestionTime API...`
- [ ] Verificar: `JWT configuration loaded`
- [ ] Verificar: `Database connection established`
- [ ] Verificar: `Seed completado`
- [ ] Verificar: `GestionTime API iniciada correctamente en puerto 8080`

#### Si hay errores:

**Error: "JWT Key not found"**
- [ ] Verificar que `JWT_SECRET_KEY` está configurada
- [ ] Verificar que NO está vacía
- [ ] Hacer re-deploy

**Error: "Database connection failed"**
- [ ] Verificar que `DATABASE_URL` es correcta
- [ ] Copiar la URL desde el PostgreSQL de Render
- [ ] Asegurarse de que termina con `?sslmode=require`
- [ ] Hacer re-deploy

**Error: "SMTP authentication failed"**
- [ ] Verificar `SMTP_USER` y `SMTP_PASSWORD`
- [ ] Confirmar que las credenciales son correctas
- [ ] Hacer re-deploy

### Paso 7: Probar la aplicación

#### Test 1: Endpoint de salud
- [ ] Ir a `https://tu-servicio.onrender.com/health`
- [ ] Debe responder: `Healthy`

#### Test 2: Swagger
- [ ] Ir a `https://tu-servicio.onrender.com/swagger`
- [ ] Debe mostrar la documentación de la API

#### Test 3: Login
- [ ] Intentar hacer login con usuario: `admin@gestiontime.com`
- [ ] Password: `Admin123`
- [ ] Debe devolver un token JWT

#### Test 4: Registro y Email
- [ ] Registrar un nuevo usuario
- [ ] Verificar que se envía el email de activación
- [ ] Revisar los logs para confirmar el envío

---

## ?? RESUMEN DE VARIABLES CONFIGURADAS

Después de completar todos los pasos, deberías tener estas variables en Render:

```
? JWT_SECRET_KEY          •••••••••••••••• (oculto)
? DATABASE_URL            •••••••••••••••• (oculto)
? SMTP_HOST               smtp.ionos.es
? SMTP_PORT               587
? SMTP_USER               envio_noreplica@tdkportal.com
? SMTP_PASSWORD           •••••••••••••••• (oculto)
? SMTP_FROM               envio_noreplica@tdkportal.com
? ASPNETCORE_ENVIRONMENT  Production
```

**Variables automáticas de Render (NO configurar):**
- `PORT` - Asignado automáticamente por Render
- `RENDER` - Siempre es "true"
- `RENDER_SERVICE_ID` - ID del servicio
- `RENDER_SERVICE_NAME` - Nombre del servicio

---

## ?? SEGURIDAD POST-CONFIGURACIÓN

- [ ] Verificar que `.gitignore` incluye archivos `.env*`
- [ ] Verificar que `.gitignore` incluye archivos `*SECURE*`
- [ ] NO commitear archivos con credenciales a Git
- [ ] Guardar una copia de seguridad de las variables en un lugar seguro (password manager)
- [ ] Documentar cuándo se generaron las claves (para rotación futura)

---

## ?? TAREAS DE MANTENIMIENTO

### Cada 90 días:

- [ ] Generar nueva `JWT_SECRET_KEY`
- [ ] Actualizar en Render Environment
- [ ] Hacer re-deploy
- [ ] Informar a los usuarios (deberán hacer login nuevamente)

### Cada 180 días:

- [ ] Cambiar password de PostgreSQL
- [ ] Actualizar `DATABASE_URL` en Render
- [ ] Hacer re-deploy

### Después de incidente de seguridad:

- [ ] Rotar TODAS las credenciales inmediatamente
- [ ] Revisar logs de acceso
- [ ] Notificar a usuarios afectados

---

## ?? DOCUMENTOS DE REFERENCIA

1. **SECURITY-RENDER-CONFIG.md** - Resumen ejecutivo de seguridad
2. **RENDER-ENVIRONMENT-GUIDE.md** - Guía detallada con troubleshooting
3. **.env.render.template** - Plantilla para importar a Render
4. **RENDER-CONFIG-VERIFIED.md** - Configuración verificada (si existe)

---

## ? CONFIRMACIÓN FINAL

Una vez completados todos los pasos:

- [ ] Todas las variables están configuradas en Render
- [ ] El deployment fue exitoso (sin errores en logs)
- [ ] La aplicación responde en `/health`
- [ ] Swagger funciona correctamente
- [ ] El login funciona
- [ ] Los emails se envían correctamente
- [ ] NO hay credenciales en el código fuente
- [ ] Los archivos `.env*` están en `.gitignore`

**¡CONFIGURACIÓN COMPLETADA! ??**

---

**Fecha de configuración:** _______________  
**Configurado por:** _______________  
**Próxima revisión:** _______________ (90 días después)

# 🌐 Variables de Entorno para Render

## ✅ URL de Deploy Correcta

**URL del servicio:** `https://gestiontimeapi.onrender.com`

## 📋 Variables de Entorno Requeridas

Configura estas variables en **Render Dashboard → Settings → Environment**:

### 🔐 Base de Datos y Autenticación

```env
DATABASE_URL=<auto-configurada-por-render>
DB_SCHEMA=pss_dvnx
JWT_SECRET_KEY=v7ZpQ9mL3H2kN8xR1aT6yW4cE0sB5dU9jF2hK7nP3qL8rM1tX6zA4gS9uV2bC5e
ASPNETCORE_ENVIRONMENT=Production
```

### 📧 Configuración SMTP (Email)

```env
SMTP_HOST=smtp.ionos.es
SMTP_PORT=587
SMTP_USER=envio_noreplica@tdkportal.com
SMTP_PASSWORD=A4gS9uV2bC5e
SMTP_FROM=envio_noreplica@tdkportal.com
```

### 🌍 URLs de la Aplicación

```env
APP_BASE_URL=https://gestiontimeapi.onrender.com
```

## 🔍 Verificación de Variables

Una vez configuradas, verifica que:

1. **DATABASE_URL** se configura automáticamente al conectar la BD PostgreSQL
2. **APP_BASE_URL** usa **HTTPS** (sin guión: `gestiontimeapi`)
3. **SMTP_PASSWORD** es la contraseña correcta: `A4gS9uV2bC5e`
4. **JWT_SECRET_KEY** tiene al menos 32 caracteres

## 🚀 Después de Configurar

1. **Guarda** las variables de entorno
2. **Manual Deploy** → Deploy latest commit
3. **Espera 3-5 minutos** para que complete el build
4. **Verifica**:
   ```sh
   curl https://gestiontimeapi.onrender.com/health
   ```

## ✅ Respuesta Esperada del Health Check

```json
{
  "status": "OK",
  "timestamp": "2025-01-01T12:00:00Z",
  "service": "GestionTime API",
  "version": "1.0.0",
  "environment": "Production",
  "database": "connected"
}
```

## 🔗 Enlaces Útiles

- **API Base:** https://gestiontimeapi.onrender.com
- **Swagger UI:** https://gestiontimeapi.onrender.com/swagger
- **Health Check:** https://gestiontimeapi.onrender.com/health
- **Render Dashboard:** https://dashboard.render.com

## ⚠️ Notas Importantes

### Plan Gratuito de Render

- ✅ Gratuito
- ⏸️ Se suspende después de 15 minutos sin uso
- ⏱️ Primera petición tarda ~30-60 segundos en despertar
- 🚀 Peticiones siguientes son rápidas

### HTTPS

- ✅ Render proporciona HTTPS automáticamente
- ✅ Certificado SSL/TLS gratuito
- ✅ El contenedor escucha HTTP, Render maneja HTTPS
- ✅ Todas las URLs públicas deben usar `https://`

### Emails de Activación

Los enlaces de activación se generarán como:
```
https://gestiontimeapi.onrender.com/api/v1/auth/activate/{token}
```

Asegúrate de que `APP_BASE_URL` esté configurada correctamente.

---

**Última actualización:** 01 Enero 2025  
**Versión de la API:** 1.0.0  
**Estado:** ✅ Configuración verificada

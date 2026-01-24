# ?? GUÍA DE VARIABLES DE ENTORNO - RENDER.COM

## ?? RESUMEN RÁPIDO

### ¿Qué es `SECURITY_KEY`?

La variable `SECURITY_KEY` que ves en tu panel de Render **NO está siendo utilizada actualmente** por tu aplicación ASP.NET Core. Es posible que:

1. Sea una configuración de prueba antigua
2. La hayas agregado para otro propósito futuro
3. Sea una clave genérica para otro servicio

### ? Variables que SÍ usa tu aplicación:

| Variable | Dónde se usa | Propósito |
|----------|--------------|-----------|
| `JWT_SECRET_KEY` | `Program.cs` línea 56 | Firmar tokens de autenticación JWT |
| `DATABASE_URL` | `Program.cs` línea 107 | Conexión a PostgreSQL |
| `SMTP_HOST` | `SmtpEmailService.cs` | Servidor de correo |
| `SMTP_PORT` | `SmtpEmailService.cs` | Puerto SMTP |
| `SMTP_USER` | `SmtpEmailService.cs` | Usuario email |
| `SMTP_PASSWORD` | `SmtpEmailService.cs` | Contraseña email |
| `SMTP_FROM` | `SmtpEmailService.cs` | Remitente de emails |
| `PORT` | `Program.cs` línea 18 | Puerto donde escucha la app (Render lo asigna automáticamente) |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core | Entorno de ejecución |

---

## ?? CÓMO CONFIGURAR EN RENDER

### Opción 1: Agregar manualmente (una por una)

1. Ve a **Dashboard de Render** ? Tu Servicio ? **Environment**
2. Haz clic en **"Add Environment Variable"**
3. Agrega cada variable con su valor

### Opción 2: Importar desde archivo (RECOMENDADO)

1. Ve a **Dashboard de Render** ? Tu Servicio ? **Environment**
2. Haz clic en **"Add from .env"**
3. Copia y pega el contenido del archivo `.env.render.template`
4. Ajusta los valores si es necesario
5. Haz clic en **"Save Changes"**

---

## ?? GENERAR CLAVES SEGURAS

### Generar JWT_SECRET_KEY (64 caracteres)

```bash
openssl rand -base64 64
```

**Salida ejemplo:**
```
3kR9mN2pL7vX5tC8wQ1aH6jF4eS9bU3dK7nM2rP5gL8xZ4vT6yC1qW9hE2oJ5sA3
```

### Generar SECURITY_KEY (32 caracteres) - Si la necesitas

```bash
openssl rand -base64 32
```

**Salida ejemplo:**
```
8vK3nM7pQ2rL5tC9xW1aH4jF6eS
```

### En Windows (PowerShell sin openssl)

```powershell
# Generar JWT_SECRET_KEY
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))

# Generar SECURITY_KEY
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

---

## ?? CONFIGURACIÓN COMPLETA PARA RENDER

### Variables OBLIGATORIAS:

```env
# Autenticación JWT
JWT_SECRET_KEY=tu-clave-generada-con-openssl-rand-base64-64

# Base de datos (Render te proporciona esta URL al crear el PostgreSQL)
DATABASE_URL=postgresql://user:password@host:port/database?sslmode=require

# Email SMTP
SMTP_HOST=smtp.ionos.es
SMTP_PORT=587
SMTP_USER=envio_noreplica@tdkportal.com
SMTP_PASSWORD=tu-contraseña-smtp
SMTP_FROM=envio_noreplica@tdkportal.com

# Entorno ASP.NET
ASPNETCORE_ENVIRONMENT=Production
```

### Variables OPCIONALES:

```env
# Solo si necesitas una clave de seguridad adicional para otro propósito
SECURITY_KEY=tu-clave-adicional-si-la-necesitas
```

### Variables AUTOMÁTICAS (Render las proporciona):

```env
PORT                    # Render asigna automáticamente
RENDER=true            # Indica que se ejecuta en Render
RENDER_SERVICE_ID       # ID del servicio
RENDER_SERVICE_NAME     # Nombre del servicio
RENDER_INSTANCE_ID      # ID de la instancia
```

---

## ?? SEGURIDAD Y MEJORES PRÁCTICAS

### ? HACER:

- ? Usar variables de entorno para todas las credenciales
- ? Generar claves largas y aleatorias (openssl)
- ? Rotar credenciales cada 90 días
- ? Usar conexiones SSL/TLS (sslmode=require para PostgreSQL)
- ? Verificar que `.gitignore` excluye archivos sensibles
- ? Limitar acceso al panel de Render a personas autorizadas

### ? NO HACER:

- ? Commitear archivos `.env` o `*SECURE*` a Git
- ? Hardcodear credenciales en el código
- ? Usar claves débiles como "123456" o "password"
- ? Compartir claves por email o chat
- ? Reutilizar la misma clave en múltiples servicios
- ? Dejar credenciales por defecto en producción

---

## ?? VERIFICAR CONFIGURACIÓN

### 1. Revisar que las variables están configuradas

En el panel de Render ? Environment deberías ver:

```
? JWT_SECRET_KEY          (valor oculto con •••••••)
? DATABASE_URL            (valor oculto con •••••••)
? SMTP_HOST               smtp.ionos.es
? SMTP_PORT               587
? SMTP_USER               envio_noreplica@tdkportal.com
? SMTP_PASSWORD           (valor oculto con •••••••)
? SMTP_FROM               envio_noreplica@tdkportal.com
? ASPNETCORE_ENVIRONMENT  Production
```

### 2. Verificar en los logs de deployment

Después de hacer el deployment, revisa los logs de Render:

```
? JWT configuration loaded successfully
? Database connection established
? SMTP configuration loaded
```

### 3. Probar la aplicación

- Intentar login ? debe funcionar (JWT)
- Registrar usuario ? debe enviar email de activación (SMTP)
- Crear/leer datos ? debe funcionar (Database)

---

## ?? SOLUCIÓN DE PROBLEMAS

### Error: "JWT Key not found"

**Causa:** `JWT_SECRET_KEY` no está configurada o tiene valor vacío

**Solución:**
1. Ir a Render ? Environment
2. Agregar `JWT_SECRET_KEY` con un valor generado
3. Hacer re-deploy del servicio

### Error: "Database connection failed"

**Causa:** `DATABASE_URL` incorrecta o base de datos no accesible

**Solución:**
1. Verificar que el PostgreSQL de Render está activo
2. Copiar la **External Database URL** desde Render PostgreSQL
3. Actualizar `DATABASE_URL` en Environment
4. Asegurarse de que termina con `?sslmode=require`

### Error: "SMTP authentication failed"

**Causa:** Credenciales SMTP incorrectas

**Solución:**
1. Verificar `SMTP_USER` y `SMTP_PASSWORD`
2. Confirmar que el servidor SMTP permite conexiones desde Render
3. Verificar que el puerto (587) es el correcto para TLS

---

## ?? REFERENCIAS

- [Render Environment Variables](https://render.com/docs/environment-variables)
- [ASP.NET Core Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)
- [PostgreSQL Connection Strings](https://www.postgresql.org/docs/current/libpq-connect.html#LIBPQ-CONNSTRING)

---

## ?? CHANGELOG DE SEGURIDAD

### 2025-01-XX - Configuración inicial
- ? JWT_SECRET_KEY movida a variables de entorno
- ? DATABASE_URL movida a variables de entorno
- ? SMTP credentials movidas a variables de entorno
- ? Archivos sensibles añadidos a .gitignore
- ? Generadas claves seguras para producción

---

**Última actualización:** 2025-01-15  
**Mantenedor:** Equipo de Desarrollo GestionTime  
**Entorno:** Render.com Frankfurt Region

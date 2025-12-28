# ?? CONFIGURACIÓN SEGURA PARA RENDER.COM

## ?? INICIO RÁPIDO

### Opción 1: Importar desde archivo (RECOMENDADO)

1. Ir a **Dashboard de Render** ? Tu Servicio ? **Environment**
2. Hacer clic en **"Add from .env"**
3. Copiar y pegar el contenido del archivo `.env.render.template`
4. Hacer clic en **"Save Changes"**
5. Hacer re-deploy del servicio

### Opción 2: Agregar manualmente

1. Ir al **Dashboard de Render** ? Tu Servicio ? **Environment**
2. Hacer clic en **"Add Environment Variable"**
3. Agregar las siguientes variables una por una:

---

## ?? VARIABLES DE ENTORNO REQUERIDAS

### ?? Autenticación JWT

```
JWT_SECRET_KEY=v7ZpQ9mL3H2kN8xR1aT6yW4cE0sB5dU9jF2hK7nP3qL8rM1tX6zA4gS9uV2bC5e
```

**?? IMPORTANTE:** Esta clave debe ser única y secreta. Genera una nueva:

```bash
openssl rand -base64 64
```

### ??? Base de Datos PostgreSQL

```
DATABASE_URL=postgresql://gestiontime:BvCDRFguh9SljJJUZOzGpdvpxgf18qnI@dpg-d57tobm3jp1c73b6i4ug-a.frankfurt-postgres.render.com:5432/pss_dvnx?sslmode=require
```

**Nota:** Obtener esta URL desde el panel de PostgreSQL de Render (External Database URL)

### ?? Configuración de Email (SMTP)

```
SMTP_HOST=smtp.ionos.es
SMTP_PORT=587
SMTP_USER=envio_noreplica@tdkportal.com
SMTP_PASSWORD=Nimda2008@2020
SMTP_FROM=envio_noreplica@tdkportal.com
```

### ?? Configuración ASP.NET Core

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:$PORT
```

---

## ? SOBRE LA VARIABLE `SECURITY_KEY`

Si ves una variable llamada `SECURITY_KEY` en tu panel de Render con valor `tu-clave-secreta-unica-aqui`:

**?? ESTA VARIABLE NO ESTÁ SIENDO USADA POR LA APLICACIÓN**

Tu aplicación actual solo utiliza las variables listadas arriba. Puedes:

- **Opción 1:** Eliminarla (si no la necesitas para otro propósito)
- **Opción 2:** Mantenerla (no afecta el funcionamiento de la app)
- **Opción 3:** Renombrarla a `JWT_SECRET_KEY` si la creaste con ese propósito

Para más información, consulta: `RENDER-ENVIRONMENT-GUIDE.md`

---

## ? PROBLEMAS DE SEGURIDAD RESUELTOS

### ? Antes (INSEGURO):
- JWT Key hardcodeada en appsettings.json
- Passwords de BD y email expuestos
- Credenciales visibles en código fuente

### ? Después (SEGURO):
- Variables de entorno en Render
- Credenciales protegidas
- Código fuente sin información sensible
- Claves generadas de forma segura

---

## ??? RECOMENDACIONES DE SEGURIDAD

### 1. Generar claves seguras

```bash
# Generar JWT_SECRET_KEY (64 caracteres)
openssl rand -base64 64

# En Windows PowerShell (sin openssl)
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

### 2. Rotar credenciales regularmente

- Cambiar `JWT_SECRET_KEY` cada 90 días
- Cambiar contraseñas de base de datos cada 90 días
- Cambiar contraseñas SMTP según política de seguridad

### 3. Verificar archivos protegidos

El archivo `.gitignore` debe incluir:

```gitignore
*.env
.env*
*secrets*
*SECURE*
render-env-variables-SECURE.txt
```

### 4. Nunca commitear archivos sensibles

? **NO COMMITEAR:**
- Archivos que terminen en `*SECURE*`
- Archivos `.env` o `.env.*`
- Archivos con contraseñas o claves

---

## ?? ARCHIVOS DE REFERENCIA

- `.env.render.template` - Plantilla completa para importar a Render
- `RENDER-ENVIRONMENT-GUIDE.md` - Guía completa de configuración y troubleshooting
- `render-env-variables-SECURE.txt` - Copia de seguridad de variables (NO commitear)

---

## ?? DEPLOYMENT

1. Configurar todas las variables de entorno en Render
2. Hacer push a la rama `main` o hacer manual deploy
3. Verificar en los logs que la aplicación inicia correctamente
4. Probar login y registro de usuarios

---

## ?? VERIFICACIÓN POST-DEPLOYMENT

### Verificar en logs de Render:

```
? Iniciando GestionTime API...
? CORS configurado
? JWT configuration loaded
? Database connection established
? Seed completado
? GestionTime API iniciada correctamente en puerto 8080
```

### Probar funcionalidades:

- Login con usuario existente
- Registro de nuevo usuario (debe enviar email)
- Crear un parte de trabajo
- Consultar datos

---

**Última actualización:** 2025-01-15  
**Entorno:** Render.com (Frankfurt Region)  
**Versión de aplicación:** GestionTime API v1.0
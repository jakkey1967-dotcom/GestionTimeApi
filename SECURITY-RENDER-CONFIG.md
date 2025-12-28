# ?? CONFIGURACIÓN SEGURA PARA RENDER.COM

## ?? IMPORTANTE: CONFIGURAR VARIABLES DE ENTORNO

### 1. Ir al Dashboard de Render ? Tu Servicio ? Environment

### 2. Agregar las siguientes Variables de Entorno:

```
JWT_SECRET_KEY=v7ZpQ9mL3H2kN8xR1aT6yW4cE0sB5dU9jF2hK7nP3qL8rM1tX6zA4gS9uV2bC5e

DATABASE_URL=postgresql://gestiontime:BvCDRFguh9SljJJUZOzGpdvpxgf18qnI@dpg-d57tobm3jp1c73b6i4ug-a.frankfurt-postgres.render.com:5432/pss_dvnx?sslmode=require

SMTP_HOST=smtp.ionos.es
SMTP_PORT=587
SMTP_USER=envio_noreplica@tdkportal.com
SMTP_PASSWORD=Nimda2008@2020
SMTP_FROM=envio_noreplica@tdkportal.com
```

## ?? PROBLEMAS DE SEGURIDAD RESUELTOS:

### ? Antes (INSEGURO):
- JWT Key hardcodeada en appsettings.json
- Passwords de BD y email expuestos
- Credenciales visibles en código fuente

### ? Después (SEGURO):
- Variables de entorno en Render
- Credenciales protegidas
- Código fuente sin información sensible

## ?? RECOMENDACIONES ADICIONALES:

### 1. Generar nueva JWT Key más segura:
```bash
# Generar una nueva clave de 64 caracteres
openssl rand -base64 64
```

### 2. Rotar credenciales regularmente

### 3. Verificar que .gitignore excluye archivos sensibles

### 4. Nunca commitear archivos que terminen en *SECURE*

## ?? NOTA SOBRE GestionTime2024SecureKey789XYZ:

Si esta clave está siendo usada en algún lugar, también debe ser movida a variables de entorno.

## ?? DEPLOYMENT:

Una vez configuradas las variables de entorno en Render, el deployment será seguro.
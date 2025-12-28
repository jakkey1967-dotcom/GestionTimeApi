# ?? CONFIGURACIÓN DE SEGURIDAD PARA REPOSITORIO PRIVADO

## ?? IMPORTANTE: REPOSITORIO PRIVADO OBLIGATORIO

### 1. Hacer Repositorio Privado en GitHub
1. Ve a: https://github.com/jakkey1967-dotcom/GestionTimeApi/settings
2. **Danger Zone** ? **Change repository visibility**
3. **Make private** ? Confirma la acción
4. ? Tu código ya NO será público

### 2. Variables de Entorno con Validación de Seguridad

#### Variables OBLIGATORIAS (con validación de origen):
```
# Core Security
ASPNETCORE_ENVIRONMENT=Production
DEPLOYMENT_SOURCE=AUTHORIZED_RENDER_ONLY
SECURITY_KEY=tu-clave-secreta-unica-aqui

# Database
ConnectionStrings__Default=Host=dpg-d57tobm3jp1c73b6i4ug-a.frankfurt-postgres.render.com;Port=5432;Database=pss_dvnx;Username=gestiontime;Password=BvCDRFguh9SljJJUZOzGpdvxgf18qnI;SslMode=Require

# JWT
Jwt__Key=v7ZpQ9mL3H2kN8xR1aT6yW4cE0sB5dU9jF2hK7nP3qL8rM1tX6zA4gS9uV2bC5e
Jwt__Issuer=GestionTime
Jwt__Audience=GestionTime.Web
Jwt__AccessMinutes=15
Jwt__RefreshDays=14
```

#### Variables OPCIONALES (Email):
```
Email__SmtpHost=smtp.ionos.es
Email__SmtpPort=587
Email__SmtpUser=envio_noreplica@tdkportal.com
Email__SmtpPassword=Nimda2008@2020
Email__From=envio_noreplica@tdkportal.com
Email__FromName=GestionTime
```

### 3. Configuración Solo para Render Autorizado
```
Name: gestiontime-api-private
Environment: Docker
Region: Frankfurt (mismo que tu BD)
Branch: master
Instance Type: Starter (Free) o Basic ($7/mes)
Auto-Deploy: SOLO desde tu cuenta autorizada
```

### 4. Acceso Restringido
- ? Solo TÚ puedes ver el código
- ? Solo TÚ puedes hacer deployments
- ? Render accede con permisos limitados
- ? Nadie más puede clonar o ver el repositorio

## ??? MEDIDAS DE PROTECCIÓN ADICIONALES

### A. Restricción de Deployment por IP (Opcional)
En Render.com puedes configurar:
- **Access Control** ? **Allowed IPs**
- Agrega solo TUS IPs autorizadas

### B. Variables de Entorno Secretas
Todas las contraseñas y claves están:
- ?? Encriptadas en Render
- ?? No visibles en logs
- ?? Solo accesibles durante runtime

### C. Autenticación de 2 Factores
Habilita 2FA en:
- ? GitHub (para proteger el repositorio)
- ? Render.com (para proteger deployments)

## ?? PASOS INMEDIATOS

### 1. Hacer Repositorio Privado AHORA
```
1. GitHub.com ? Tu repositorio ? Settings
2. Scroll hasta "Danger Zone"
3. "Change repository visibility" ? "Make private"
4. Confirmar con tu contraseña
```

### 2. Verificar Acceso en Render
```
1. Render Dashboard ? Web Services
2. Solo TU cuenta debe tener acceso
3. Verificar que nadie más está en el equipo
```

### 3. Cambiar Claves Sensibles
```
# Genera nuevas claves para mayor seguridad
Jwt__Key=NUEVA-CLAVE-SECRETA-AQUI
SECURITY_KEY=CLAVE-UNICA-PARA-TU-DEPLOYMENT
```

## ?? ADVERTENCIAS DE SEGURIDAD

? **NUNCA hagas público un repositorio con:**
- Connection strings de base de datos
- Claves JWT
- Contraseñas de email
- Cualquier credencial

? **SIEMPRE mantén privado cualquier código que contenga:**
- Lógica de negocio sensible  
- Configuraciones de producción
- Credenciales o tokens
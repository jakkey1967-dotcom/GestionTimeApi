# ?? Guía de Deployment en Render.com

## ?? Pasos para deploar en Render.com

### 1?? Crear Web Service
1. Ve a https://render.com/dashboard
2. Click **"New +"** ? **"Web Service"**
3. Conecta tu repositorio: `https://github.com/jakkey1967-dotcom/GestionTimeApi.git`

### 2?? Configuración Básica
```
Name: gestiontime-api
Environment: Docker
Region: Frankfurt (mismo que tu BD)
Branch: master
Instance Type: Starter (Free) o Basic ($7/mes)
```

### 3?? Variables de Entorno (CRÍTICO)

Agrega estas variables en **Environment Variables**:

| Variable | Valor |
|----------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__Default` | `Host=dpg-d57tobm3jp1c73b6i4ug-a.frankfurt-postgres.render.com;Port=5432;Database=pss_dvnx;Username=gestiontime;Password=BvCDRFguh9SljJJUZOzGpdvpxgf18qnI;SslMode=Require` |
| `Jwt__Key` | `v7ZpQ9mL3H2kN8xR1aT6yW4cE0sB5dU9jF2hK7nP3qL8rM1tX6zA4gS9uV2bC5e` |

### 4?? Variables Opcionales (Email)
| Variable | Valor |
|----------|--------|
| `Email__SmtpHost` | `smtp.ionos.es` |
| `Email__SmtpPort` | `587` |
| `Email__SmtpUser` | `envio_noreplica@tdkportal.com` |
| `Email__SmtpPassword` | `Nimda2008@2020` |

### 5?? Deploy
1. Click **"Create Web Service"**
2. Render automáticamente:
   - Clona tu repositorio
   - Construye la imagen Docker
   - Deploya la aplicación
   - Te da una URL: `https://gestiontime-api-xxxx.onrender.com`

## ?? Verificación del Deploy

### Health Check
```bash
curl https://tu-api-url.onrender.com/health
```

### Swagger UI
```
https://tu-api-url.onrender.com/swagger
```

### Test Login
```bash
curl -X POST https://tu-api-url.onrender.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@gestiontime.local","password":"admin123"}'
```

## ?? URLs Finales

Una vez deployado tendrás:
- **API Base:** `https://gestiontime-api-xxxx.onrender.com`
- **Swagger:** `https://gestiontime-api-xxxx.onrender.com/swagger`  
- **Health:** `https://gestiontime-api-xxxx.onrender.com/health`

## ?? Configuración para Clientes

Tus clientes desktop deberán usar:
```csharp
// En ApiClient.cs
BaseUrl = "https://gestiontime-api-xxxx.onrender.com"
```

## ? Nota sobre Free Tier

Si usas el plan gratuito de Render:
- La aplicación se "duerme" tras 15 min de inactividad
- Primer request después del "sueño" tarda ~30 segundos
- Para producción real, considera el plan Basic ($7/mes)

## ?? Auto Deploy

Cada vez que hagas `git push` a master:
- Render automáticamente redeploya
- Puedes ver logs en tiempo real
- Zero downtime deployments
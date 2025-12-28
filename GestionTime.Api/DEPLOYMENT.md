# Configuración para deployment en diferentes plataformas

## ?? Render.com

### Build Command:
```bash
dotnet publish -c Release -o out
```

### Start Command:
```bash
cd out && dotnet GestionTime.Api.dll
```

### Environment Variables:
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://0.0.0.0:$PORT`
- `ConnectionStrings__Default=tu-connection-string-aqui`

## ?? Azure App Service

### Configuración:
1. Runtime: .NET 8
2. OS: Linux
3. Pricing: Basic B1 o superior

### Variables de entorno:
```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default=tu-azure-sql-connection
Jwt__Key=tu-clave-jwt-segura
```

## ?? Docker Hub / Registry

### Construir y subir imagen:
```bash
docker build -t tu-usuario/gestiontime-api:latest .
docker push tu-usuario/gestiontime-api:latest
```

### Ejecutar en producción:
```bash
docker run -d \
  --name gestiontime-api \
  -p 80:8080 \
  -e ConnectionStrings__Default="Host=tu-bd;..." \
  -e ASPNETCORE_ENVIRONMENT=Production \
  tu-usuario/gestiontime-api:latest
```

## ?? Railway.app

### railway.toml:
```toml
[build]
builder = "nixpacks"

[deploy]
healthcheckPath = "/health"
healthcheckTimeout = 300
restartPolicyType = "ON_FAILURE"
```

## ?? Variables de entorno requeridas

### Obligatorias:
- `ConnectionStrings__Default`
- `Jwt__Key`

### Opcionales:
- `ASPNETCORE_ENVIRONMENT` (default: Production)
- `Jwt__AccessMinutes` (default: 15)
- `Jwt__RefreshDays` (default: 14)
- `Email__SmtpHost`
- `Email__SmtpUser`
- `Email__SmtpPassword`

## ?? Health Check

El endpoint `/health` devuelve:
- 200 OK si todo funciona
- 503 Service Unavailable si hay problemas

Usar para configurar health checks en tu plataforma de deployment.
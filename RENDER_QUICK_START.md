# 🚀 Quick Deploy to Render

## ⚡ 5-Minute Setup

### 1. Crear Cuenta Render
- Ve a [render.com/register](https://render.com/register)
- Regístrate con GitHub

### 2. Crear PostgreSQL Database
1. **Dashboard → New → PostgreSQL**
2. Configuración:
   - Name: `gestiontime-db`
   - Region: `Frankfurt` (EU)
   - Plan: `Free`
3. **Create Database** → Espera 2-3 min
4. **Copia Internal Database URL**

### 3. Deploy API desde GitHub
1. **Push código a GitHub:**
   ```bash
   git add .
   git commit -m "deploy: render config"
   git push origin main
   ```

2. **Render Dashboard → New → Blueprint**
3. Conecta repositorio GitHub
4. Render detecta `render.yaml`
5. **Apply** → Espera 5-10 min

### 4. Configurar DATABASE_URL
1. Dashboard → `gestiontime-api` → **Environment**
2. **Add Environment Variable:**
   - Key: `DATABASE_URL`
   - Value: (pegar Internal Database URL de paso 2)
3. **Save** → Redeploy automático

### 5. Testing
```powershell
.\test-render-deploy.ps1 -BaseUrl "https://gestiontime-api.onrender.com"
```

## ✅ Checklist

- [ ] PostgreSQL creada y "Available"
- [ ] Código pusheado a GitHub
- [ ] Web Service desplegado desde Blueprint
- [ ] `DATABASE_URL` configurada
- [ ] Health check OK (`/health` returns 200)
- [ ] Login JWT funciona
- [ ] Tests pasan (12/12)

## 📋 Variables de Entorno Requeridas

| Variable | Valor | Nota |
|----------|-------|------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Auto-configurado |
| `ASPNETCORE_URLS` | `http://0.0.0.0:$PORT` | Auto-configurado |
| `DATABASE_URL` | `postgresql://...` | **Configurar manualmente** |
| `JWT_SECRET_KEY` | (generado) | Auto-generado |
| `JWT_ISSUER` | `GestionTimeApi` | Auto-configurado |
| `JWT_AUDIENCE` | `GestionTimeClient` | Auto-configurado |
| `JWT_EXPIRES_HOURS` | `12` | Auto-configurado |

## 🔗 URLs

- **API:** `https://gestiontime-api.onrender.com`
- **Health:** `https://gestiontime-api.onrender.com/health`
- **Swagger:** `https://gestiontime-api.onrender.com/swagger`
- **Dashboard:** https://dashboard.render.com

## ⚠️ Troubleshooting

### Error: 502 Bad Gateway
→ Verificar logs en Dashboard → Logs  
→ Verificar `DATABASE_URL` configurada correctamente

### Error: Database connection timeout
→ Verificar que PostgreSQL está "Available"  
→ Verificar `sslmode=require` en connection string

### Error: JWT token invalid
→ Regenerar `JWT_SECRET_KEY` en Environment Variables  
→ Hacer nuevo login para obtener token fresco

## 📚 Documentación Completa

- [Guía Completa de Deploy](docs/RENDER_DEPLOY_GUIDE.md)
- [Testing](docs/TESTING_INFORMES_V2.md)
- [API Docs](docs/API_INFORMES_V2.md)
- [Desktop Implementation](docs/DESKTOP_INFORMES_V2_IMPLEMENTATION.md)

## 💰 Costos

**Free Tier:**
- Web Service: $0/mes (spin down tras 15min inactividad)
- PostgreSQL: $0/mes (256MB RAM, 1GB storage)
- Total: **$0/mes** ✅

**Upgrade ($14/mes):**
- Web Service: $7/mes (24/7 uptime)
- PostgreSQL: $7/mes (1GB RAM, 10GB storage)
- Total: **$14/mes** (recomendado para producción)

## 🎯 Next Steps

1. ✅ **Deploy completado**
2. 📱 Actualizar GestionTime Desktop con URL de producción
3. 📊 Setup monitoring en Render Dashboard
4. 🔒 Configurar CORS con frontend real
5. 💾 Configurar backups automáticos PostgreSQL

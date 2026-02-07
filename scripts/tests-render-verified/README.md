# 🧪 Tests Verificados de Endpoints en Render

Esta carpeta contiene **tests completos y verificados** de los endpoints de la API en producción (Render).

## 📋 Propósito

- **Verificar** que los endpoints funcionan correctamente en producción
- **Documentar** el comportamiento esperado de cada módulo
- **Detectar** problemas antes de integrar con GestionTime Desktop
- **Reutilizar** como base para tests de integración

---

## 🎯 Tests Disponibles

### ✅ **Clientes** (`test-clientes-render.ps1`)

**Endpoints probados:**
- `GET /api/v1/clientes?page=1&pageSize=10` - Listar con paginación
- `GET /api/v1/clientes?search=Global` - Búsqueda por término
- `POST /api/v1/clientes` - Crear cliente
- `GET /api/v1/clientes/:id` - Obtener por ID
- `PUT /api/v1/clientes/:id` - Actualización completa
- `PATCH /api/v1/clientes/:id/nota` - Actualización parcial
- `DELETE /api/v1/clientes/:id` - Eliminar

**Características:**
- ✅ CRUD completo
- ✅ Validación de paginación
- ✅ Búsqueda por texto
- ✅ Verificación de eliminación (404)
- ✅ Estadísticas finales

**Estado:** ✅ **VERIFICADO** - Todos los endpoints funcionan correctamente

---

## 🚀 Cómo Ejecutar

### 1. Test Individual
```powershell
.\scripts\tests-render-verified\test-clientes-render.ps1
```

### 2. Todos los Tests
```powershell
Get-ChildItem .\scripts\tests-render-verified\*.ps1 | ForEach-Object { 
    Write-Host "`n🧪 Ejecutando: $($_.Name)" -ForegroundColor Cyan
    & $_.FullName 
}
```

---

## 📦 Próximos Tests a Crear

### 🔄 En Progreso
- `test-grupos-render.ps1` - CRUD de Grupos
- `test-tipos-render.ps1` - CRUD de Tipos
- `test-partes-render.ps1` - CRUD de Partes de Trabajo
- `test-tags-render.ps1` - Tags (ya existe, mover aquí cuando esté verificado)

### 📋 Planificados
- `test-users-render.ps1` - Gestión de Usuarios (Admin)
- `test-presence-render.ps1` - Presencia (check-in/check-out)
- `test-freshdesk-render.ps1` - Integración Freshdesk
- `test-auth-render.ps1` - Autenticación completa

---

## 🔑 Credenciales de Test

**Usuario de prueba:**
```
Email: psantos@global-retail.com
Password: 12345678
Role: Admin
```

---

## ✅ Criterios de Verificación

Para que un test se considere **VERIFICADO** debe:

1. ✅ **Health Check** - Confirmar que el servicio está activo
2. 🔐 **Login** - Autenticación exitosa
3. 📊 **CRUD Completo** - Crear, Leer, Actualizar, Eliminar
4. 🔍 **Búsqueda/Filtros** - Si aplica al endpoint
5. ⚠️ **Manejo de Errores** - Verificar 404, 400, etc.
6. 🧹 **Limpieza** - Eliminar datos de prueba creados
7. 📈 **Estadísticas** - Verificar estado final

---

## 📝 Convención de Nombres

```
test-{modulo}-render.ps1
```

**Ejemplos:**
- `test-clientes-render.ps1` ✅
- `test-grupos-render.ps1`
- `test-partes-render.ps1`
- `test-auth-render.ps1`

---

## 🆘 Solución de Problemas

### Error: "Servicio no responde"
```powershell
# Verificar estado en Render Dashboard
# https://dashboard.render.com/
```

### Error: "Login failed"
```powershell
# Verificar credenciales en la BD de producción
# O resetear password con: .\scripts\reset-password.ps1
```

### Error: "Tabla no existe"
```powershell
# Ejecutar migraciones en Render:
# Dashboard → PostgreSQL → Shell
# Ejecutar SQL de migraciones manualmente
```

---

## 📚 Recursos

- **Dashboard Render:** https://dashboard.render.com/
- **API Base URL:** https://gestiontimeapi.onrender.com
- **Swagger:** https://gestiontimeapi.onrender.com/swagger
- **Health Check:** https://gestiontimeapi.onrender.com/health

---

**Última actualización:** 2026-02-06  
**Mantenido por:** Equipo de Desarrollo GestionTime

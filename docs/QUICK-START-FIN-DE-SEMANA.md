# 📋 QUICK START - Sistema de Presencia (Fin de Semana)

## ⚡ RESUMEN ULTRA RÁPIDO

### **Estado Actual:**
- ✅ **Frontend (Desktop):** COMPLETO - Ventana de usuarios funciona
- ❌ **Backend (API):** PENDIENTE - Falta campo `last_seen_at` en BD

### **Resultado:**
- La ventana de usuarios se abre ✅
- Pero todos aparecen como "Offline" ⚠️ (porque falta `last_seen_at`)

---

## 🎯 QUÉ HACER EL FIN DE SEMANA (3 Pasos)

### **PASO 1: Backup (5 minutos)**

1. Abrir **Render Dashboard** → Tu PostgreSQL database
2. Click en **"Web Shell"** (o usar cualquier cliente SQL)
3. Ejecutar:

```sh
psql $DATABASE_URL
```

4. Copiar y pegar **TODO** el contenido de:
```
C:\GestionTime\GestionTimeApi\docs\SQL-Backup-Render-Interno.sql
```

5. Verificar que dice: **"✅ BACKUP COMPLETADO"**

---

### **PASO 2: Migración SQL (5 minutos)**

En el mismo Web Shell, copiar y pegar **TODO** el contenido de:
```
C:\GestionTime\GestionTimeApi\docs\SQL-Migration-AddLastSeenAt.sql
```

Verificar que dice: **"✅ MIGRACIÓN COMPLETADA"**

---

### **PASO 3: Actualizar Código Backend (20 minutos)**

Abrir archivo de instrucciones:
```
C:\GestionTime\GestionTimeApi\docs\IMPLEMENTAR-PRESENCIA-BACKEND.md
```

Seguir las instrucciones para actualizar:
1. `User.cs` (agregar propiedad `LastSeenAt`)
2. `AdminUsersController.cs` (incluir `LastSeenAt` + endpoint ping)
3. Desplegar backend

---

## ✅ VERIFICACIÓN RÁPIDA

Después de implementar, probar:

```powershell
# En PowerShell:
cd C:\GestionTime\GestionTimeDesktop
.\Scripts\Test-AdminUsersEndpoint.ps1
```

Debe devolver usuarios con `lastSeenAt` incluido.

---

## 🚨 SI ALGO SALE MAL

```sql
-- En Web Shell de Render:
psql $DATABASE_URL

SET search_path TO pss_dvnx;
DROP INDEX IF EXISTS pss_dvnx.idx_users_last_seen_at;
ALTER TABLE pss_dvnx.users DROP COLUMN IF EXISTS last_seen_at;
\q
```

---

## 📚 DOCUMENTACIÓN COMPLETA

Si necesitas más detalles, abrir:
```
C:\GestionTime\GestionTimeApi\docs\RESUMEN-SISTEMA-PRESENCIA-PENDIENTE.md
```

---

**Tiempo Total:** ~1 hora  
**Riesgo:** Bajo (migración segura)  
**Beneficio:** Sistema de usuarios online en tiempo real

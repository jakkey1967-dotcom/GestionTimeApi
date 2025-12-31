# 🛡️ Proceso Seguro de Migración Multi-Tenant

## ⚠️ **IMPORTANTE: Antes de Aplicar Multi-Tenancy**

### **Checklist de Seguridad**

- [ ] **NUNCA** aplicar esto en cliente en producción sin backup
- [ ] **SIEMPRE** crear backup completo antes de migrar
- [ ] **VERIFICAR** que no hay usuarios activos durante migración
- [ ] **PROBAR** en cliente nuevo primero
- [ ] **DOCUMENTAR** el proceso de rollback

---

## 🎯 **Escenarios**

### **✅ Escenario 1: Cliente NUEVO (Seguro)**

Cliente que NO está en producción. Puedes aplicar multi-tenancy directamente.

**Pasos:**
1. Crear servicio nuevo en Render
2. Configurar `DB_SCHEMA=nombre_cliente`
3. Deploy
4. Crear usuario admin

**Herramientas:**
```powershell
# Ver herramientas disponibles
dotnet run -- help
```

---

### **⚠️ Escenario 2: Cliente EXISTENTE en Producción (Requiere Backup)**

Cliente con datos reales. **REQUIERE PROCESO COMPLETO DE MIGRACIÓN**.

**Pasos Obligatorios:**

#### **PASO 1: BACKUP COMPLETO** ⚠️
```powershell
# Exportar TODO el schema actual
dotnet run -- backup-client pss_dvnx
```

Esto crea:
- `backups/pss_dvnx_YYYYMMDD_HHMMSS/schema.sql` - Estructura
- `backups/pss_dvnx_YYYYMMDD_HHMMSS/data.sql` - Datos
- `backups/pss_dvnx_YYYYMMDD_HHMMSS/manifest.json` - Metadatos

#### **PASO 2: Verificar Backup**
```powershell
# Verificar que el backup es válido
dotnet run -- verify-backup backups/pss_dvnx_YYYYMMDD_HHMMSS
```

#### **PASO 3: Crear Schema Nuevo**
```powershell
# Crear nuevo schema sin tocar el antiguo
dotnet run -- create-schema pss_dvnx_new
```

#### **PASO 4: Copiar Datos**
```powershell
# Copiar todos los datos al nuevo schema
dotnet run -- migrate-data gestiontime pss_dvnx_new
```

#### **PASO 5: Verificar Integridad**
```powershell
# Comparar que todo se copió correctamente
dotnet run -- compare-schemas gestiontime pss_dvnx_new
```

#### **PASO 6: Actualizar Configuración**
```powershell
# Actualizar variable de entorno en Render
DB_SCHEMA=pss_dvnx_new
```

#### **PASO 7: Probar**
- Login con usuarios existentes
- Verificar datos
- Probar funcionalidades críticas

#### **PASO 8: Limpiar Schema Antiguo (OPCIONAL)**
```powershell
# Solo después de confirmar que todo funciona
dotnet run -- drop-schema gestiontime
```

---

## 🔧 **Herramientas de Migración Segura**

### **1. Backup Completo**

```powershell
dotnet run -- backup-client <schema_name>
```

**Qué hace:**
- Exporta estructura de todas las tablas
- Exporta TODOS los datos (usuarios, roles, partes, etc.)
- Genera manifest con checksums
- Comprime en archivo ZIP

**Ejemplo:**
```powershell
dotnet run -- backup-client pss_dvnx
# Genera: backups/pss_dvnx_20250101_143022.zip
```

---

### **2. Restaurar Backup**

```powershell
dotnet run -- restore-backup <backup_file> <target_schema>
```

**Qué hace:**
- Crea schema de destino
- Restaura estructura
- Restaura datos
- Verifica integridad

**Ejemplo:**
```powershell
dotnet run -- restore-backup backups/pss_dvnx_20250101_143022.zip pss_dvnx_restored
```

---

### **3. Migrar Datos Entre Schemas**

```powershell
dotnet run -- migrate-data <source_schema> <target_schema>
```

**Qué hace:**
- Crea schema destino si no existe
- Copia estructura de tablas
- Copia TODOS los datos
- Mantiene relaciones (foreign keys)
- Preserva índices

**Ejemplo:**
```powershell
dotnet run -- migrate-data gestiontime pss_dvnx_new
```

---

### **4. Comparar Schemas**

```powershell
dotnet run -- compare-schemas <schema1> <schema2>
```

**Qué hace:**
- Compara número de tablas
- Compara número de registros por tabla
- Compara checksums de datos
- Genera reporte de diferencias

**Ejemplo:**
```powershell
dotnet run -- compare-schemas gestiontime pss_dvnx_new

# Output:
# ✅ users: 156 registros (igual)
# ✅ roles: 3 registros (igual)
# ⚠️ partes: 1024 vs 1023 registros (DIFERENCIA)
```

---

### **5. Verificar Integridad**

```powershell
dotnet run -- verify-integrity <schema_name>
```

**Qué hace:**
- Verifica foreign keys
- Verifica usuarios sin roles
- Verifica partes huérfanos
- Verifica datos corruptos

**Ejemplo:**
```powershell
dotnet run -- verify-integrity pss_dvnx
```

---

## 📋 **Plan de Migración Paso a Paso**

### **Para Cliente EN PRODUCCIÓN**

```powershell
# ========================================
# DÍA 1: PREPARACIÓN
# ========================================

# 1. Backup completo (OBLIGATORIO)
dotnet run -- backup-client pss_dvnx
# Guardar archivo: backups/pss_dvnx_20250101_143022.zip

# 2. Verificar backup
dotnet run -- verify-backup backups/pss_dvnx_20250101_143022.zip
# ✅ Verificación exitosa

# 3. Informar a usuarios (mantenimiento programado)
# ⚠️ AVISAR: "Mantenimiento 2 horas, domingo 3 AM"


# ========================================
# DÍA 2: MIGRACIÓN (HORARIO DE BAJA ACTIVIDAD)
# ========================================

# 4. Poner aplicación en modo mantenimiento
# (Opcional: página "En mantenimiento")

# 5. Crear nuevo schema
dotnet run -- create-schema pss_dvnx_new

# 6. Migrar datos
dotnet run -- migrate-data gestiontime pss_dvnx_new
# ⏱️ Esto puede tardar según cantidad de datos

# 7. Comparar schemas
dotnet run -- compare-schemas gestiontime pss_dvnx_new
# ✅ Todo igual

# 8. Verificar integridad
dotnet run -- verify-integrity pss_dvnx_new
# ✅ Sin errores

# 9. Actualizar Render
# Settings → Environment → DB_SCHEMA=pss_dvnx_new → Save
# Esperar redeploy (5 min)

# 10. Probar login con usuario real
curl -X POST https://gestiontime-api.onrender.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"usuario@test.com","password":"password123"}'
# ✅ Login exitoso

# 11. Verificar datos críticos
dotnet run -- check-render
# ✅ Schema: pss_dvnx_new (10 tablas, X registros)

# 12. Quitar modo mantenimiento

# 13. Monitorear 24 horas


# ========================================
# DÍA 3: LIMPIEZA (SOLO SI TODO OK)
# ========================================

# 14. Después de 24-48 horas sin problemas
dotnet run -- drop-schema gestiontime
# ⚠️ CUIDADO: Esto elimina el schema antiguo

# 15. Eliminar backups antiguos (después de 30 días)
```

---

## 🚨 **Plan de Rollback**

Si algo sale mal durante la migración:

```powershell
# ========================================
# ROLLBACK DE EMERGENCIA
# ========================================

# 1. Cambiar variable de entorno a schema anterior
# Render → Settings → DB_SCHEMA=gestiontime → Save

# 2. Esperar redeploy (5 min)

# 3. Verificar que funciona
curl -X POST https://gestiontime-api.onrender.com/api/v1/auth/login ...

# 4. Si el schema anterior está dañado, restaurar backup
dotnet run -- restore-backup backups/pss_dvnx_20250101_143022.zip gestiontime

# ✅ Sistema restaurado al estado anterior
```

---

## 📊 **Checklist de Verificación Post-Migración**

Después de migrar, verificar:

- [ ] Login funciona con usuarios existentes
- [ ] Roles asignados correctamente
- [ ] Partes de trabajo visibles
- [ ] Crear nuevo parte funciona
- [ ] Editar parte funciona
- [ ] Catálogos (clientes, grupos, tipos) visibles
- [ ] Refresh token funciona
- [ ] Logout funciona
- [ ] No hay errores en logs de Render

---

## 💾 **Estrategia de Backups Continua**

### **Backups Automáticos Recomendados:**

1. **Diario** (mantener 7 días)
```powershell
# Agregar a tarea programada
dotnet run -- backup-client pss_dvnx
```

2. **Semanal** (mantener 4 semanas)
3. **Mensual** (mantener 12 meses)
4. **Antes de cada deploy importante**

---

## 🎯 **Resumen: ¿Cuándo es Seguro?**

### ✅ **SEGURO (Sin backup necesario):**
- Cliente nuevo sin datos
- Entorno de desarrollo/pruebas
- Schema vacío

### ⚠️ **REQUIERE BACKUP (Obligatorio):**
- Cliente en producción
- Datos reales de usuarios
- Cualquier schema con data > 0 registros

### 🚫 **NUNCA hacer:**
- Migrar producción sin backup
- Eliminar schema antiguo antes de verificar nuevo
- Aplicar cambios en horario laboral
- Migrar sin plan de rollback

---

## 📞 **Contacto de Emergencia**

Si algo sale mal durante migración:

1. ⚠️ **NO ENTRAR EN PÁNICO**
2. 📝 Documentar el error exacto
3. 🔄 Ejecutar rollback
4. 📊 Revisar logs de Render
5. 💾 Restaurar desde backup si necesario

---

## 🎓 **Práctica Recomendada**

**ANTES de migrar cliente real:**

1. Practicar con cliente de prueba
2. Simular migración completa
3. Practicar rollback
4. Cronometrar proceso
5. Identificar puntos críticos

**Solo después de dominar el proceso**, aplicar en producción.

---

## 🔒 **Principio de Oro**

> **"Si no tienes backup, no tienes producción"**

**NUNCA** migrar un cliente en producción sin:
- ✅ Backup completo verificado
- ✅ Plan de rollback probado
- ✅ Ventana de mantenimiento programada
- ✅ Usuario informado
- ✅ Monitoreo post-migración

---

## 📚 **Comandos Rápidos de Referencia**

```powershell
# Backup
dotnet run -- backup-client <schema>

# Restaurar
dotnet run -- restore-backup <file> <schema>

# Migrar
dotnet run -- migrate-data <source> <target>

# Comparar
dotnet run -- compare-schemas <schema1> <schema2>

# Verificar
dotnet run -- verify-integrity <schema>

# Limpiar
dotnet run -- drop-schema <schema>

# Estado actual
dotnet run -- check-render
```

---

## ✅ **Conclusión**

**Multi-tenancy es seguro SOLO si:**
1. Aplicas proceso completo de backup
2. Pruebas en entorno no productivo primero
3. Tienes plan de rollback
4. Verificas integridad post-migración
5. Monitoreas el sistema después

**Para clientes nuevos:** Aplica directamente ✅  
**Para clientes en producción:** Sigue proceso completo ⚠️

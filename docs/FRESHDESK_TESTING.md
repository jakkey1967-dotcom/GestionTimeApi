# 🧪 Guía de Testing - Integración Freshdesk

Esta guía te muestra cómo testear la integración de Freshdesk de forma segura, sin afectar datos de producción.

---

## 📋 Pre-requisitos

1. ✅ **API Key de Freshdesk configurada** en `appsettings.json`
2. ✅ **API corriendo** en `http://localhost:2501`
3. ✅ **Usuario autenticado** (para usar los endpoints protegidos)

---

## 🔧 Paso 1: Configurar Freshdesk

Edita `appsettings.json` y configura tus credenciales de Freshdesk:

```json
"Freshdesk": {
  "Domain": "tu-dominio",
  "ApiKey": "tu-api-key-aqui"
}
```

### Cómo obtener tus credenciales:

**Domain:**
- Si tu URL de Freshdesk es: `https://miempresa.freshdesk.com`
- Tu Domain es: `miempresa`

**API Key:**
1. Inicia sesión en Freshdesk
2. Ve a **Perfil → Settings** (icono de engranaje)
3. En "Your API key", copia la clave
4. Pégala en `ApiKey`

---

## 🚀 Paso 2: Iniciar la API

```bash
# Asegúrate de estar en el directorio del proyecto
dotnet run --project GestionTime.Api.csproj
```

La API iniciará en:
- HTTP: `http://localhost:2501`
- HTTPS: `https://localhost:2502`

---

## 🧪 Paso 3: Ejecutar el Script de Testing Automático

El script `test-freshdesk.ps1` realiza todas las pruebas de forma segura y automática:

```powershell
# Desde PowerShell, en el directorio del proyecto
.\scripts\test-freshdesk.ps1
```

### ¿Qué hace el script?

1. ✅ Verifica que la configuración de Freshdesk está completa
2. ✅ Verifica que la API está corriendo
3. ✅ Se autentica con usuario admin
4. ✅ Prueba la conexión con Freshdesk
5. ✅ Busca tickets (solo lectura)
6. ✅ Busca tags (solo lectura)
7. 📊 Muestra un resumen completo

**🔒 100% Seguro:** Todas las operaciones son de solo lectura, no modifica nada.

---

## 🔍 Paso 4: Testing Manual con Swagger

Si prefieres testear manualmente, usa Swagger UI:

1. Abre tu navegador: `http://localhost:2501/swagger`
2. Autentícate:
   - Endpoint: `POST /api/auth/login`
   - Body:
     ```json
     {
       "email": "admin@gestiontime.com",
       "password": "Admin123"
     }
     ```
   - La cookie se guarda automáticamente

3. Prueba los endpoints de Freshdesk:

### 🔬 Test de Conexión
```
GET /api/freshdesk/test-connection
```
- Verifica que la API Key y Domain son correctos
- Busca tu email en Freshdesk como agente
- Muestra tu Agent ID si te encuentra

**Respuesta exitosa:**
```json
{
  "success": true,
  "message": "✅ Conexión a Freshdesk exitosa",
  "agentId": 123456789,
  "email": "tu-email@empresa.com",
  "timestamp": "2025-01-24T14:30:00Z"
}
```

### 🎫 Búsqueda de Tickets
```
GET /api/freshdesk/tickets/suggest?term=error&scope=mine_or_unassigned&limit=10
```

**Parámetros:**
- `term` (opcional): Texto a buscar en tickets
- `scope`: 
  - `mine` - Solo mis tickets
  - `unassigned` - Solo tickets sin asignar
  - `mine_or_unassigned` - Mis tickets o sin asignar (por defecto)
  - `all` - Todos los tickets
- `limit`: Número máximo de resultados (máx: 50)

**Respuesta:**
```json
{
  "success": true,
  "count": 5,
  "tickets": [
    {
      "id": 12345,
      "subject": "Error en login",
      "status": 2,
      "priority": 1,
      "created_at": "2025-01-20T10:00:00Z"
    }
  ]
}
```

### 🏷️ Búsqueda de Tags
```
GET /api/freshdesk/tags/suggest?term=bug&limit=10
```

**Parámetros:**
- `term` (opcional): Texto a buscar en tags
- `limit`: Número máximo de resultados (máx: 50)

**Respuesta:**
```json
{
  "success": true,
  "count": 3,
  "tags": ["bug", "bugfix", "debug"]
}
```

---

## 🔄 Paso 5: Sincronizar Tags (Opcional)

**⚠️ Solo para usuarios Admin**

Este endpoint sincroniza tags desde Freshdesk a la base de datos local:

```
POST /api/freshdesk/tags/sync
```

**Respuesta:**
```json
{
  "success": true,
  "message": "✅ Sincronización completada. 45 tags actualizados",
  "syncedCount": 45,
  "timestamp": "2025-01-24T14:35:00Z"
}
```

---

## 📊 Verificar Logs

Los logs se guardan en `logs/` y muestran toda la actividad de Freshdesk:

```bash
# Ver logs en tiempo real
Get-Content -Path "logs/gestiontime-api.log" -Wait -Tail 50
```

Busca líneas como:
```
[INF] 🧪 Probando conexión a Freshdesk...
[INF] Usuario actual: admin@gestiontime.com
[INF] Buscando agente Freshdesk por email: admin@gestiontime.com
[INF] Freshdesk agents autocomplete completado en 245ms, encontrados: 1
[INF] AgentId para admin@gestiontime.com resuelto y cacheado: 123456789
```

---

## ❌ Solución de Problemas Comunes

### Error: "API Key inválida"
```json
{
  "success": false,
  "message": "❌ API Key inválida. Verifica tu configuración de Freshdesk",
  "error": "Unauthorized"
}
```
**Solución:** Verifica que la API Key en `appsettings.json` sea correcta.

---

### Error: "Domain incorrecto"
```json
{
  "success": false,
  "message": "❌ Domain incorrecto. Verifica el dominio de Freshdesk",
  "error": "Not Found"
}
```
**Solución:** Verifica que el Domain en `appsettings.json` sea correcto (sin `https://` ni `.freshdesk.com`).

---

### Advertencia: "No se encontró agente"
```json
{
  "success": true,
  "message": "⚠️ Conexión exitosa pero no se encontró agente con este email",
  "email": "tu-email@empresa.com",
  "suggestion": "Verifica que el email del usuario exista como agente en Freshdesk"
}
```
**Solución:** 
- Verifica que tu email esté registrado como agente en Freshdesk
- O prueba con un email que sí exista como agente

---

### No hay tags en búsqueda
```json
{
  "success": true,
  "count": 0,
  "tags": []
}
```
**Solución:** Ejecuta la sincronización de tags:
```
POST /api/freshdesk/tags/sync
```

---

## 🔒 Seguridad

Todos los endpoints de testing son **100% seguros**:

✅ **Solo lectura** - No modifican datos en Freshdesk
✅ **Autenticación requerida** - Solo usuarios autenticados pueden acceder
✅ **Rate limiting** - Respeta los límites de Freshdesk API
✅ **Logs detallados** - Todo queda registrado para auditoría
✅ **Timeout configurado** - No cuelga la aplicación
✅ **Manejo de errores** - Errores informativos sin exponer datos sensibles

---

## 📈 Métricas de Performance

El sistema registra automáticamente:

- ⏱️ **Latencia de API** - Tiempo de respuesta de Freshdesk
- 📊 **Cache hits** - Cuántas veces se usa el caché de agentes
- 🔄 **Reintentos** - Reintentos automáticos en caso de rate limiting
- 📝 **Errores** - Todos los errores con contexto completo

Ejemplo de log:
```
[INF] Freshdesk agents autocomplete completado en 245ms, encontrados: 1
[INF] Freshdesk tickets search completado en 1230ms, encontrados: 15/150
```

---

## ✅ Checklist de Testing

Usa este checklist para verificar que todo funciona:

- [ ] Configuración de Freshdesk completa en `appsettings.json`
- [ ] API corriendo en localhost
- [ ] Script automático ejecutado sin errores
- [ ] Test de conexión exitoso
- [ ] Búsqueda de tickets funciona
- [ ] Búsqueda de tags funciona
- [ ] Logs muestran actividad correcta
- [ ] No hay errores en consola
- [ ] Swagger UI muestra los endpoints
- [ ] Autenticación funciona correctamente

---

## 🎯 Próximos Pasos

Una vez que todo funcione correctamente:

1. ✅ Configurar en **producción** (Render.com):
   - Agregar `Freshdesk__Domain` como variable de entorno
   - Agregar `Freshdesk__ApiKey` como variable de entorno

2. ✅ Integrar en tu frontend:
   - Usar `/api/freshdesk/tickets/suggest` para autocompletar tickets
   - Usar `/api/freshdesk/tags/suggest` para autocompletar tags

3. ✅ Monitorear en producción:
   - Revisar logs regularmente
   - Configurar alertas para errores de Freshdesk
   - Monitorear rate limiting

---

## 📞 Soporte

Si encuentras algún problema:

1. Revisa los logs en `logs/gestiontime-api.log`
2. Verifica que la configuración sea correcta
3. Ejecuta el script de testing para diagnóstico automático
4. Revisa los endpoints en Swagger UI

---

**¡Listo! Ahora puedes testear la integración de Freshdesk de forma segura. 🎉**

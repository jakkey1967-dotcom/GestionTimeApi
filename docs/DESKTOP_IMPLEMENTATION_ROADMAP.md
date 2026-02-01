# 🗺️ Roadmap de Implementación: GestionTime Desktop (WPF)

## 📋 Objetivo

Implementar la aplicación Desktop de GestionTime siguiendo un orden lógico que **minimice riesgos** y asegure que cada módulo funcione antes de pasar al siguiente.

---

## 🎯 Principios de Implementación

1. ✅ **Incremental:** Construir de lo simple a lo complejo
2. ✅ **Validación continua:** Probar cada módulo antes de avanzar
3. ✅ **Sin romper:** No tocar backend, solo consumir APIs existentes
4. ✅ **Reutilización:** Compartir DTOs y servicios entre módulos
5. ✅ **Testing:** Verificar cada funcionalidad antes de continuar

---

## 📊 Fases de Implementación

### **Leyenda de Prioridades:**
- 🔴 **CRÍTICO** - Sin esto, la app no funciona
- 🟡 **IMPORTANTE** - Funcionalidad core del negocio
- 🟢 **OPCIONAL** - Mejoras y optimizaciones

---

## 🚀 FASE 0: Prerequisitos y Configuración Base

**Prioridad:** 🔴 **CRÍTICO**  
**Tiempo estimado:** 2-3 horas  
**Objetivo:** Configurar infraestructura básica del proyecto WPF

### ✅ Tareas:

- [ ] **1. Crear proyecto WPF** (.NET 8)
  ```bash
  dotnet new wpf -n GestionTime.Desktop
  ```

- [ ] **2. Instalar NuGet packages:**
  ```xml
  <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
  <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
  <PackageReference Include="System.Text.Json" Version="8.0.0" />
  ```

- [ ] **3. Configurar Dependency Injection** en `App.xaml.cs`
- [ ] **4. Configurar HttpClient** con BaseAddress
- [ ] **5. Crear estructura de carpetas:**
  ```
  /Models/Api/
  /Services/Api/
  /ViewModels/
  /Views/
  /Converters/
  /Resources/
  ```

### 📝 Test de Verificación:
```csharp
// Verificar que DI funciona
var services = new ServiceCollection();
services.AddHttpClient("GestionTimeApi", client => {
    client.BaseAddress = new Uri("https://localhost:2502");
});
var provider = services.BuildServiceProvider();
var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("GestionTimeApi");
```

### ⚠️ Advertencias:
- ⚠️ No avanzar si DI no funciona
- ⚠️ Configurar correctamente el certificado SSL para desarrollo

---

## 🔐 FASE 1: Autenticación (Login/Logout)

**Prioridad:** 🔴 **CRÍTICO**  
**Tiempo estimado:** 4-6 horas  
**Objetivo:** Implementar login, almacenamiento de token y logout

### ✅ Tareas:

#### 1.1 Models y DTOs
- [ ] `LoginRequest.cs`
- [ ] `LoginResponse.cs`
- [ ] `RefreshTokenRequest.cs`
- [ ] `RefreshTokenResponse.cs`

#### 1.2 Services
- [ ] `AuthApiService.cs` - Llamadas al backend
- [ ] `TokenStorageService.cs` - Guardar/recuperar token de forma segura
- [ ] `AuthStateService.cs` - Estado global de autenticación

#### 1.3 ViewModels
- [ ] `LoginViewModel.cs` - Lógica de login
- [ ] `MainViewModel.cs` - Estado global de la app

#### 1.4 Views
- [ ] `LoginWindow.xaml` - Pantalla de login
- [ ] `MainWindow.xaml` - Shell principal (vacío por ahora)

### 📝 Test de Verificación:
```powershell
# Ejecutar desde Desktop
# 1. Abrir LoginWindow
# 2. Ingresar: psantos@global-retail.com / 12345678
# 3. Verificar que retorna token
# 4. Verificar que abre MainWindow
# 5. Hacer logout → Debe cerrar sesión y volver a login
```

### ⚠️ Advertencias:
- ⚠️ **NO hardcodear** el token en código
- ⚠️ Usar `SecureString` o `ProtectedData` para almacenar
- ⚠️ Implementar auto-refresh de token (expire en 15 min)

### 📚 Documentos de referencia:
- Ver `docs/BACKEND_API_CHANGES_FOR_DESKTOP.md`

---

## 🏠 FASE 2: Dashboard y Navegación

**Prioridad:** 🔴 **CRÍTICO**  
**Tiempo estimado:** 3-4 horas  
**Objetivo:** Estructura principal con menú de navegación

### ✅ Tareas:

- [ ] **2.1 MainWindow estructura:**
  - Sidebar con menú
  - ContentControl para cargar vistas dinámicamente
  - Header con usuario logueado
  - Footer con estado

- [ ] **2.2 Navegación:**
  - `NavigationService.cs` - Cambiar entre vistas
  - Comandos para cada opción del menú

- [ ] **2.3 Menú básico:**
  ```
  📊 Dashboard
  📝 Partes de Trabajo
  🏢 Clientes
  📁 Grupos
  🏷️ Tipos
  🎫 Freshdesk
  👥 Usuarios (solo ADMIN)
  👥 Presencia (solo ADMIN)
  ```

### 📝 Test de Verificación:
- Click en cada opción del menú → Debe cambiar vista
- Verificar que muestra usuario logueado en header
- Logout → Debe cerrar MainWindow y abrir LoginWindow

### ⚠️ Advertencias:
- ⚠️ No crear todas las vistas aún, solo placeholders
- ⚠️ Asegurar que el menú se ajusta según el rol (USER vs ADMIN)

---

## 📁 FASE 3: Catálogos Básicos (Grupos, Tipos, Clientes)

**Prioridad:** 🟡 **IMPORTANTE**  
**Tiempo estimado:** 6-8 horas  

### **Orden de implementación:**

### 3.1 Grupos (MÁS SIMPLE) - 2 horas
- [ ] Implementar según `docs/GRUPOS_TIPOS_DESKTOP_IMPLEMENTATION.md`
- [ ] Models: `GrupoDto`, `GrupoCreateRequest`, `GrupoUpdateRequest`
- [ ] Service: `GruposApiService`
- [ ] ViewModel: `GruposManagementViewModel`
- [ ] View: `GruposManagementWindow.xaml`

**Test:** Ejecutar `scripts/test-grupos-crud.ps1` → Debe pasar ✅

### 3.2 Tipos (IDÉNTICO A GRUPOS) - 1 hora
- [ ] Copiar estructura de Grupos
- [ ] Buscar/Reemplazar "Grupo" → "Tipo"
- [ ] Implementar según `docs/GRUPOS_TIPOS_DESKTOP_IMPLEMENTATION.md`

**Test:** Ejecutar `scripts/test-tipos-crud.ps1` → Debe pasar ✅

### 3.3 Clientes (MÁS COMPLEJO, CON PAGINACIÓN) - 3-4 horas
- [ ] Implementar según `docs/CLIENTES_DESKTOP_IMPLEMENTATION.md`
- [ ] Models: `ClienteDto`, `ClientePagedResult`, etc.
- [ ] Service: `ClientesApiService` con paginación
- [ ] ViewModel: `ClientesManagementViewModel` con filtros
- [ ] View: `ClientesManagementWindow.xaml`

**Test:** Ejecutar `scripts/test-clientes-crud-completo.ps1` → Debe pasar ✅

### ⚠️ Advertencias:
- ⚠️ **ORDEN IMPORTANTE:** Grupos → Tipos → Clientes (de simple a complejo)
- ⚠️ Clientes tiene paginación, los otros NO
- ⚠️ Probar cada uno antes de avanzar al siguiente

---

## 📝 FASE 4: Partes de Trabajo (Core Business)

**Prioridad:** 🔴 **CRÍTICO**  
**Tiempo estimado:** 8-10 horas  
**Objetivo:** CRUD completo de partes + Timer + Tags

### ✅ Tareas:

#### 4.1 Listar Partes (2 horas)
- [ ] Models: `ParteDto`, `PartePagedResult`
- [ ] Service: `PartesApiService`
- [ ] ViewModel: `PartesListViewModel`
- [ ] View: `PartesListView.xaml` con DataGrid

#### 4.2 Crear/Editar Parte (3 horas)
- [ ] Models: `CreateParteRequest`, `UpdateParteRequest`
- [ ] ViewModel: `ParteFormViewModel`
- [ ] View: `ParteFormWindow.xaml`
- [ ] Validaciones: Fecha, Cliente, Grupo, Tipo requeridos

#### 4.3 Timer Control (2 horas)
- [ ] Botones: Iniciar/Pausar/Detener
- [ ] Actualización en tiempo real
- [ ] Calcular duración automáticamente

#### 4.4 Tags Integration (2 horas)
- [ ] Selector de tags con checkbox
- [ ] Crear nuevo tag desde el formulario
- [ ] Mostrar tags asignados en la lista

### 📝 Test de Verificación:
```
1. Crear parte nueva → OK
2. Iniciar timer → OK
3. Pausar timer → OK
4. Detener y guardar → OK
5. Asignar tags → OK
6. Editar parte existente → OK
7. Eliminar parte → OK
```

### ⚠️ Advertencias:
- ⚠️ Timer debe actualizar UI cada segundo (usar `DispatcherTimer`)
- ⚠️ No permitir crear parte sin Cliente, Grupo y Tipo
- ⚠️ El backend retorna tags con IDs, mapear correctamente

### 📚 Documentos de referencia:
- `docs/PARTE_TAGS_IMPLEMENTATION.md`
- `Controllers/PartesDeTrabajoController.cs`

---

## 🎫 FASE 5: Integración con Freshdesk

**Prioridad:** 🟡 **IMPORTANTE**  
**Tiempo estimado:** 4-5 horas  
**Objetivo:** Buscar tickets y asociarlos a partes

### ✅ Tareas:

#### 5.1 Búsqueda de Tickets (2 horas)
- [ ] Models: `FreshdeskTicketSuggestDto`
- [ ] Service: `FreshdeskApiService`
- [ ] ViewModel: `FreshdeskSearchViewModel`
- [ ] View: `FreshdeskSearchWindow.xaml` con búsqueda + lista

#### 5.2 Asociar Ticket a Parte (2 horas)
- [ ] Botón "Buscar Ticket" en formulario de parte
- [ ] Seleccionar ticket de la lista
- [ ] Guardar `ticket_freshdesk_id` en el parte
- [ ] Mostrar info del ticket en la UI

### 📝 Test de Verificación:
```powershell
scripts/test-freshdesk-search-from-view.ps1
```

### ⚠️ Advertencias:
- ⚠️ Freshdesk puede estar lento, agregar loading indicator
- ⚠️ Manejar errores 401 (API key inválida)
- ⚠️ Búsqueda debe ser **asíncrona**

### 📚 Documentos de referencia:
- `docs/FRESHDESK_DESKTOP_INTEGRATION.md`
- `docs/FRESHDESK_TICKET_SEARCH_FROM_VIEW.md`

---

## 👥 FASE 6: Gestión de Usuarios y Presencia (Solo ADMIN)

**Prioridad:** 🟡 **IMPORTANTE**  
**Tiempo estimado:** 5-6 horas  

### ✅ Tareas:

#### 6.1 Gestión de Usuarios (3 horas)
- [ ] Implementar según `docs/USERS_MANAGEMENT_DESKTOP_IMPLEMENTATION.md`
- [ ] CRUD completo de usuarios
- [ ] Asignar roles
- [ ] Habilitar/Deshabilitar usuarios

**Test:** Ejecutar `scripts/test-users-management.ps1` → Debe pasar ✅

#### 6.2 Presencia en Tiempo Real (3 horas)
- [ ] Implementar según `docs/PRESENCIA_DESKTOP_IMPLEMENTATION.md`
- [ ] Ver usuarios online
- [ ] KICK usuarios remotamente
- [ ] Auto-refresh cada 30s

**Test:** Ejecutar `scripts/test-presence-complete.ps1` → Debe pasar ✅

### ⚠️ Advertencias:
- ⚠️ **Solo mostrar estas opciones si el rol es ADMIN**
- ⚠️ KICK debe pedir confirmación antes de ejecutar
- ⚠️ Auto-refresh debe detenerse al cerrar la ventana

---

## 📊 FASE 7: Dashboard y Reportes (OPCIONAL)

**Prioridad:** 🟢 **OPCIONAL**  
**Tiempo estimado:** 6-8 horas  
**Objetivo:** Estadísticas y visualización de datos

### ✅ Tareas:

- [ ] **7.1 Dashboard inicial:**
  - Total de horas trabajadas (hoy/semana/mes)
  - Partes abiertos
  - Clientes más trabajados
  - Tags más usados

- [ ] **7.2 Reportes:**
  - Reporte por cliente
  - Reporte por usuario
  - Reporte por fecha
  - Export a Excel/CSV

- [ ] **7.3 Gráficos:**
  - Horas por día (gráfico de líneas)
  - Clientes por horas (gráfico de barras)
  - Tags más usados (gráfico de torta)

### 📝 Test de Verificación:
- Navegar al Dashboard → Debe mostrar datos correctos
- Filtrar por fechas → Debe actualizar gráficos
- Exportar a Excel → Debe descargar archivo

---

## 📋 Checklist General de Implementación

### ✅ Por cada módulo:
- [ ] Models/DTOs creados
- [ ] API Service implementado
- [ ] ViewModel con MVVM Toolkit
- [ ] View XAML diseñada
- [ ] Registrado en DI container
- [ ] Agregado al menú principal
- [ ] Tests manuales ejecutados
- [ ] Manejo de errores implementado

### ✅ Validaciones globales:
- [ ] Todas las vistas manejan errores de red
- [ ] Loading indicators en operaciones largas
- [ ] Confirmaciones antes de eliminar
- [ ] Validación de campos requeridos
- [ ] Mensajes de error claros
- [ ] Token auto-refresh funcionando

---

## 🚨 Warnings Importantes

### ⚠️ NO HACER:
- ❌ Modificar el backend durante la implementación de Desktop
- ❌ Hardcodear URLs o tokens
- ❌ Implementar múltiples módulos en paralelo sin probar
- ❌ Crear todas las vistas al mismo tiempo
- ❌ Ignorar errores de compilación

### ✅ SÍ HACER:
- ✅ Seguir el orden de fases estrictamente
- ✅ Probar cada módulo antes de avanzar
- ✅ Ejecutar scripts de test del backend para verificar
- ✅ Usar try-catch en todas las llamadas al API
- ✅ Agregar logs para debugging

---

## 📚 Documentos de Referencia

### Por Módulo:
1. **Auth:** `docs/BACKEND_API_CHANGES_FOR_DESKTOP.md`
2. **Grupos/Tipos:** `docs/GRUPOS_TIPOS_DESKTOP_IMPLEMENTATION.md`
3. **Clientes:** `docs/CLIENTES_DESKTOP_IMPLEMENTATION.md`
4. **Partes:** `docs/PARTE_TAGS_IMPLEMENTATION.md`
5. **Freshdesk:** `docs/FRESHDESK_DESKTOP_INTEGRATION.md`
6. **Usuarios:** `docs/USERS_MANAGEMENT_DESKTOP_IMPLEMENTATION.md`
7. **Presencia:** `docs/PRESENCIA_DESKTOP_IMPLEMENTATION.md`

### Tests de Backend:
```powershell
scripts/test-grupos-crud.ps1
scripts/test-tipos-crud.ps1
scripts/test-clientes-crud-completo.ps1
scripts/test-users-management.ps1
scripts/test-presence-complete.ps1
scripts/test-freshdesk-search-from-view.ps1
```

---

## 🎯 Tiempo Total Estimado

| Fase | Tiempo | Prioridad |
|------|--------|-----------|
| 0. Prerequisitos | 2-3 horas | 🔴 CRÍTICO |
| 1. Autenticación | 4-6 horas | 🔴 CRÍTICO |
| 2. Dashboard | 3-4 horas | 🔴 CRÍTICO |
| 3. Catálogos | 6-8 horas | 🟡 IMPORTANTE |
| 4. Partes de Trabajo | 8-10 horas | 🔴 CRÍTICO |
| 5. Freshdesk | 4-5 horas | 🟡 IMPORTANTE |
| 6. Usuarios/Presencia | 5-6 horas | 🟡 IMPORTANTE |
| 7. Reportes | 6-8 horas | 🟢 OPCIONAL |
| **TOTAL MVP** | **32-42 horas** | |
| **TOTAL COMPLETO** | **38-50 horas** | |

---

## 🚀 Próximos Pasos

1. ✅ Leer este documento completo
2. ✅ Iniciar por **FASE 0** (Prerequisitos)
3. ✅ **NO saltar fases**
4. ✅ Ejecutar tests del backend para cada módulo
5. ✅ Documentar problemas encontrados
6. ✅ Pedir ayuda si algo no funciona

---

**Fecha:** 2026-02-01  
**Estado:** ✅ **LISTO PARA INICIAR**  
**Autor:** GitHub Copilot + Francisco Santos  
**Versión:** 1.0

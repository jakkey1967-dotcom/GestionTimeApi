# 🔧 Solución: Error CS0006 - No se encontró el archivo de metadatos

## ❌ Error

```
error CS0006: No se encontró el archivo de metadatos 'GestionTime.Domain.dll'
error CS0006: No se encontró el archivo de metadatos 'GestionTime.Infrastructure.dll'
```

## 🔍 Causas Comunes

1. **Archivos de compilación corruptos** (bin/obj)
2. **Cache de NuGet desactualizado**
3. **Referencias de proyectos desactualizadas**
4. **Visual Studio no sincronizado con cambios recientes**
5. **Compilación incremental con errores previos**

---

## ✅ Solución Rápida (Opción 1)

### Paso 1: Ejecutar Script Automático

```powershell
.\scripts\quick-fix-build.ps1
```

Este script hace:
1. ✅ Limpia `bin` y `obj`
2. ✅ Limpia cache de NuGet
3. ✅ Restaura paquetes
4. ✅ Compila sin compilación incremental

**Tiempo estimado:** ~1-2 minutos

---

## 🔧 Solución Completa (Opción 2)

Si el script rápido no funciona:

```powershell
.\scripts\fix-build-error.ps1
```

Este script hace una limpieza más profunda y te guía paso a paso.

---

## 🛠️ Solución Manual (Opción 3)

### Paso 1: Cerrar Visual Studio
```
Cierra completamente Visual Studio si está abierto
```

### Paso 2: Limpiar Directorios
```powershell
# Eliminar todos los bin/obj
Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force
```

### Paso 3: Limpiar Cache de NuGet
```powershell
dotnet nuget locals all --clear
```

### Paso 4: Restaurar Paquetes
```powershell
dotnet restore --force --no-cache
```

### Paso 5: Compilar sin Incremental
```powershell
dotnet build --no-incremental
```

### Paso 6: Verificar
```powershell
dotnet run --project GestionTime.Api
```

---

## 🔍 Diagnóstico Adicional

### Ver Estado de Proyectos
```powershell
# Listar todos los .csproj
Get-ChildItem -Recurse -Filter *.csproj | Select-Object Name, Directory
```

### Verificar Referencias
```powershell
# Ver referencias del proyecto API
dotnet list GestionTime.Api/GestionTime.Api.csproj reference
```

**Deberías ver:**
```
Referencia de proyecto
  ..\GestionTime.Domain\GestionTime.Domain.csproj
  ..\GestionTime.Infrastructure\GestionTime.Infrastructure.csproj
```

---

## 📁 Estructura de Proyectos Esperada

```
GestionTimeApi/
├── GestionTime.Domain/
│   └── GestionTime.Domain.csproj
├── GestionTime.Infrastructure/
│   ├── GestionTime.Infrastructure.csproj
│   └── Referencias: Domain
└── GestionTime.Api/
    ├── GestionTime.Api.csproj
    └── Referencias: Domain, Infrastructure
```

---

## ⚠️ Problemas Específicos

### Error: "No se puede encontrar GestionTime.Domain.dll"

**Solución:**
```powershell
# Compilar proyectos en orden
dotnet build GestionTime.Domain/GestionTime.Domain.csproj
dotnet build GestionTime.Infrastructure/GestionTime.Infrastructure.csproj
dotnet build GestionTime.Api/GestionTime.Api.csproj
```

### Error: "Referencias circulares"

**Diagnóstico:**
```powershell
# Ver gráfico de dependencias
dotnet list package --include-transitive
```

**Solución:**
- Revisar que no haya referencias cruzadas entre proyectos

### Error: "Versión de SDK incompatible"

**Verificar SDK:**
```powershell
dotnet --version
```

**Esperado:** 8.0.x o superior

**Actualizar SDK:**
```powershell
# Descargar desde: https://dotnet.microsoft.com/download
```

---

## 🔄 Si Nada Funciona

### Opción 1: Clonar Fresco
```powershell
# Hacer backup
Copy-Item -Path . -Destination ../GestionTimeApi_backup -Recurse

# Clonar de nuevo
cd ..
git clone https://github.com/jakkey1967-dotcom/GestionTimeApi.git GestionTimeApi_fresh
cd GestionTimeApi_fresh
dotnet restore
dotnet build
```

### Opción 2: Verificar Git
```powershell
# Ver cambios no commiteados
git status

# Ver diferencias
git diff

# Resetear cambios locales (¡CUIDADO!)
git reset --hard
git clean -fdx

# Restaurar y compilar
dotnet restore
dotnet build
```

---

## 📝 Logs de Diagnóstico

### Compilación Verbose
```powershell
dotnet build --verbosity detailed > build.log 2>&1
notepad build.log
```

### Ver Errores Específicos
```powershell
dotnet build 2>&1 | Select-String "error"
```

---

## ✅ Verificación Post-Solución

### 1. Compilación Exitosa
```powershell
dotnet build
```
**Esperado:** `Build succeeded. 0 Warning(s), 0 Error(s)`

### 2. Tests Funcionan
```powershell
dotnet test
```

### 3. API Arranca
```powershell
dotnet run --project GestionTime.Api
```
**Esperado:** `Now listening on: http://localhost:2501`

### 4. Swagger Accesible
```
Abrir: http://localhost:2501/swagger
```

---

## 🚀 Scripts Disponibles

| Script | Descripción | Uso |
|--------|-------------|-----|
| `quick-fix-build.ps1` | Limpieza rápida | Primer intento |
| `fix-build-error.ps1` | Limpieza completa guiada | Si quick-fix falla |
| `cleanup.ps1` | Limpieza profunda | Último recurso |

---

## 💡 Consejos para Prevenir

### 1. Limpieza Regular
```powershell
# Cada semana
dotnet clean
dotnet nuget locals all --clear
dotnet restore
```

### 2. Compilación Limpia
```powershell
# Antes de commit importante
dotnet build --no-incremental
```

### 3. Visual Studio
- Cerrar antes de ejecutar scripts
- Usar "Clean Solution" regularmente
- Reiniciar VS si se comporta raro

### 4. Git
```powershell
# Ignorar archivos de compilación
# (ya está en .gitignore)
bin/
obj/
*.user
```

---

## 📚 Referencias

- [.NET Build Errors](https://docs.microsoft.com/en-us/dotnet/core/tools/diagnostics)
- [NuGet Cache Management](https://docs.microsoft.com/en-us/nuget/consume-packages/managing-the-global-packages-and-cache-folders)

---

## ✅ Checklist de Solución

- [ ] Cerré Visual Studio
- [ ] Ejecuté `.\scripts\quick-fix-build.ps1`
- [ ] Si falló, ejecuté `.\scripts\fix-build-error.ps1`
- [ ] Verifiqué que compila: `dotnet build`
- [ ] Verifiqué que arranca: `dotnet run --project GestionTime.Api`
- [ ] Abrí Visual Studio y compilé (Ctrl+Shift+B)
- [ ] Todo funciona ✅

---

**¡Problema resuelto!** 🎉

Si el problema persiste después de todos estos pasos, por favor:
1. Copia el log de `dotnet build --verbosity detailed`
2. Revisa si hay otros errores en la consola
3. Verifica que todos los .csproj existen en sus carpetas

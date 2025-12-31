# 🔧 Fix: Carpeta wwwroot-pss_dvnx Visible en Visual Studio

## ⚠️ Problema Identificado

La carpeta `wwwroot-pss_dvnx` **existe físicamente** en el sistema de archivos pero **no aparecía en Visual Studio** porque no estaba incluida explícitamente en el archivo `GestionTime.Api.csproj`.

---

## ✅ Solución Aplicada

### Cambios en `GestionTime.Api.csproj`

Se agregó la siguiente entrada en el archivo del proyecto:

```xml
<!-- wwwroot específico del cliente PSS DVNX -->
<Content Include="wwwroot-pss_dvnx\**\*">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  <Link>wwwroot-pss_dvnx\%(RecursiveDir)%(FileName)%(Extension)</Link>
</Content>
```

**Explicación:**
- `Include="wwwroot-pss_dvnx\**\*"` → Incluye todos los archivos de esa carpeta
- `CopyToOutputDirectory>PreserveNewest` → Copia archivos al compilar
- `<Link>...` → Hace que aparezcan en el explorador de VS

---

## 🔄 Cómo Ver los Cambios

### Opción 1: Recargar Proyecto (Recomendado)

1. **En Visual Studio:**
   - Clic derecho en el proyecto `GestionTime.Api`
   - Seleccionar **"Unload Project"** (Descargar proyecto)
   - Esperar unos segundos
   - Clic derecho nuevamente
   - Seleccionar **"Reload Project"** (Recargar proyecto)

2. **Resultado esperado:**
   ```
   GestionTimeApi/
   ├── wwwroot/
   │   └── images/
   │       └── LogoOscuro.png
   └── wwwroot-pss_dvnx/           ← ✅ AHORA VISIBLE
       └── images/
           ├── .gitkeep
           ├── pss_dvnx_logo.png
           └── pss_dvnx_logo.png.png
   ```

### Opción 2: Reiniciar Visual Studio

Si recargar el proyecto no funciona:
- Cerrar Visual Studio completamente
- Volver a abrir la solución

---

## 📊 Verificación

### Desde PowerShell (ya verificado)

```powershell
# Confirmar que la carpeta existe físicamente
Test-Path C:\GestionTime\GestionTimeApi\wwwroot-pss_dvnx
# Resultado: True ✅

# Ver archivos en Git
git ls-files wwwroot-pss_dvnx
# Resultado:
# wwwroot-pss_dvnx/images/.gitkeep
# wwwroot-pss_dvnx/images/pss_dvnx_logo.png
# wwwroot-pss_dvnx/images/pss_dvnx_logo.png.png
```

### Desde Visual Studio (después de recargar)

**Deberías ver:**
```
Solution Explorer
└── GestionTime.Api
    ├── wwwroot
    │   └── images
    │       └── LogoOscuro.png
    └── wwwroot-pss_dvnx          ← ✅ Ahora visible
        └── images
            ├── .gitkeep
            ├── pss_dvnx_logo.png
            └── pss_dvnx_logo.png.png
```

---

## 🎯 Para Agregar Nuevos Clientes

Cuando agregues una carpeta `wwwroot-{nuevoCliente}`, debes:

1. **Crear la carpeta físicamente:**
   ```bash
   mkdir wwwroot-cliente_abc
   mkdir wwwroot-cliente_abc/images
   ```

2. **Agregar al `.csproj`:**
   ```xml
   <!-- wwwroot específico del Cliente ABC -->
   <Content Include="wwwroot-cliente_abc\**\*">
     <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
     <Link>wwwroot-cliente_abc\%(RecursiveDir)%(FileName)%(Extension)</Link>
   </Content>
   ```

3. **Recargar proyecto en Visual Studio**

4. **Agregar al Git:**
   ```bash
   git add wwwroot-cliente_abc/
   git commit -m "feat: agregar recursos para Cliente ABC"
   ```

---

## 📝 Commit Realizado

```
be6ba69  fix: incluir carpeta wwwroot-pss_dvnx en archivo csproj para visibilidad en VS
```

**Estado actual:**
- ✅ 6 commits locales pendientes de push
- ✅ Compilación exitosa
- ✅ Carpeta incluida en proyecto

---

## ⚠️ Archivo Duplicado Detectado

**Pendiente de eliminar:**
```
wwwroot-pss_dvnx/images/pss_dvnx_logo.png.png  ← Duplicado innecesario
```

**Comando para eliminarlo:**
```bash
git rm wwwroot-pss_dvnx/images/pss_dvnx_logo.png.png
git commit -m "fix: eliminar logo duplicado con extension doble"
git push
```

---

## ✅ Resumen

**Antes:**
- ❌ Carpeta existía pero no era visible en VS
- ❌ No estaba en archivo `.csproj`

**Después:**
- ✅ Carpeta incluida explícitamente
- ✅ Visible después de recargar proyecto
- ✅ Se copia al compilar
- ✅ Trackeada por Git

---

**🎉 Recarga el proyecto en Visual Studio para ver los cambios!**

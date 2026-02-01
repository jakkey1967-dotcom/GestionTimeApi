# ==============================================================================
# SOLUCIÓN: Error CS0006 - No se encontró el archivo de metadatos
# ==============================================================================

Write-Host "🔧 Solucionando error CS0006..." -ForegroundColor Cyan
Write-Host ""

# Paso 1: Cerrar Visual Studio si está abierto
Write-Host "⚠️ IMPORTANTE: Cierra Visual Studio antes de continuar" -ForegroundColor Yellow
Write-Host "Presiona Enter cuando hayas cerrado Visual Studio..." -ForegroundColor Yellow
$null = Read-Host
Write-Host ""

# Paso 2: Limpiar todos los directorios bin y obj
Write-Host "🧹 Paso 1/5: Limpiando directorios bin y obj..." -ForegroundColor Cyan
Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | ForEach-Object {
    Write-Host "   Eliminando: $($_.FullName)" -ForegroundColor Gray
    Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "✅ Directorios bin/obj eliminados" -ForegroundColor Green
Write-Host ""

# Paso 3: Limpiar cache de NuGet
Write-Host "🧹 Paso 2/5: Limpiando cache de NuGet..." -ForegroundColor Cyan
dotnet nuget locals all --clear
Write-Host "✅ Cache de NuGet limpiado" -ForegroundColor Green
Write-Host ""

# Paso 4: Restaurar paquetes NuGet
Write-Host "📦 Paso 3/5: Restaurando paquetes NuGet..." -ForegroundColor Cyan
dotnet restore --force --no-cache
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Paquetes restaurados correctamente" -ForegroundColor Green
} else {
    Write-Host "❌ Error al restaurar paquetes" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Paso 5: Compilar la solución
Write-Host "🔨 Paso 4/5: Compilando la solución..." -ForegroundColor Cyan
dotnet build --no-incremental
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Compilación exitosa" -ForegroundColor Green
} else {
    Write-Host "❌ Error en la compilación" -ForegroundColor Red
    Write-Host ""
    Write-Host "📋 Si el error persiste, revisa los logs arriba" -ForegroundColor Yellow
    exit 1
}
Write-Host ""

# Paso 6: Verificar proyectos
Write-Host "🔍 Paso 5/5: Verificando proyectos..." -ForegroundColor Cyan

$projects = @(
    "GestionTime.Domain",
    "GestionTime.Infrastructure", 
    "GestionTime.Api"
)

foreach ($project in $projects) {
    $csproj = Get-ChildItem -Path . -Filter "$project.csproj" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($csproj) {
        Write-Host "   ✅ $project encontrado" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️ $project NO encontrado" -ForegroundColor Yellow
    }
}
Write-Host ""

# Paso 7: Ejecutar la API para verificar
Write-Host "🚀 ¿Deseas ejecutar la API para verificar? (S/N)" -ForegroundColor Cyan
$response = Read-Host
if ($response -eq "S" -or $response -eq "s") {
    Write-Host ""
    Write-Host "🚀 Ejecutando API..." -ForegroundColor Cyan
    dotnet run --project GestionTime.Api
} else {
    Write-Host ""
    Write-Host "✅ Solución aplicada exitosamente" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Próximos pasos:" -ForegroundColor Cyan
    Write-Host "   1. Abre Visual Studio" -ForegroundColor Gray
    Write-Host "   2. Abre la solución" -ForegroundColor Gray
    Write-Host "   3. Compila (Ctrl+Shift+B)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   O ejecuta: dotnet run --project GestionTime.Api" -ForegroundColor Gray
}

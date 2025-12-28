@echo off
REM Script de arranque para desarrollo local en Windows

echo ?? Iniciando GestionTime API en modo desarrollo...

REM Verificar si .NET está instalado
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ? .NET SDK no está instalado. Por favor, instala .NET 8.0 SDK
    pause
    exit /b 1
)

REM Restaurar paquetes
echo ?? Restaurando paquetes NuGet...
dotnet restore

REM Ejecutar migraciones
echo ?? Aplicando migraciones de base de datos...
dotnet ef database update --no-build

REM Compilar
echo ?? Compilando proyecto...
dotnet build --no-restore

REM Ejecutar la aplicación
echo ??  Iniciando aplicación...
echo ?? API estará disponible en: https://localhost:2501
echo ?? Swagger UI: https://localhost:2501/swagger
echo.

set ASPNETCORE_ENVIRONMENT=Development
dotnet run --no-build

pause
#!/bin/bash

# Script de arranque para desarrollo local
echo "?? Iniciando GestionTime API en modo desarrollo..."

# Verificar si .NET está instalado
if ! command -v dotnet &> /dev/null; then
    echo "? .NET SDK no está instalado. Por favor, instala .NET 8.0 SDK"
    exit 1
fi

# Verificar si PostgreSQL está ejecutándose (local)
if command -v pg_isready &> /dev/null; then
    if ! pg_isready -h localhost -p 5432 &> /dev/null; then
        echo "??  PostgreSQL no está ejecutándose en localhost:5432"
        echo "Iniciando con Docker Compose..."
        docker-compose up -d postgres
        sleep 10
    fi
fi

# Restaurar paquetes
echo "?? Restaurando paquetes NuGet..."
dotnet restore

# Ejecutar migraciones
echo "?? Aplicando migraciones de base de datos..."
dotnet ef database update --no-build

# Compilar
echo "?? Compilando proyecto..."
dotnet build --no-restore

# Ejecutar la aplicación
echo "??  Iniciando aplicación..."
echo "?? API estará disponible en: https://localhost:2501"
echo "?? Swagger UI: https://localhost:2501/swagger"
echo ""

export ASPNETCORE_ENVIRONMENT=Development
dotnet run --no-build
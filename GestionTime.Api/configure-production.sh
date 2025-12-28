#!/bin/bash

# ?? Script para configurar la app desktop con la API de producción
# Uso: ./configure-production.sh https://tu-api-url.onrender.com

if [ -z "$1" ]; then
    echo "? Error: Debes proporcionar la URL de la API"
    echo ""
    echo "Uso: $0 <API_URL>"
    echo "Ejemplo: $0 https://gestiontime-api-abc123.onrender.com"
    exit 1
fi

API_URL=$1

echo "?? Configurando app desktop para producción..."
echo "API URL: $API_URL"

# Buscar archivo ApiClient.cs en desktop
DESKTOP_PATH="../../../GestionTime.Desktop/Services/ApiClient.cs"

if [ ! -f "$DESKTOP_PATH" ]; then
    echo "? No se encontró $DESKTOP_PATH"
    echo "Ejecuta este script desde la carpeta de la API"
    exit 1
fi

# Hacer backup
cp "$DESKTOP_PATH" "$DESKTOP_PATH.backup"

# Actualizar URL
sed -i.bak "s|BaseUrl = \"[^\"]*\"|BaseUrl = \"$API_URL\"|g" "$DESKTOP_PATH"

echo "? Configuración actualizada"
echo "   Archivo: $DESKTOP_PATH"
echo "   Backup creado: $DESKTOP_PATH.backup"
echo ""
echo "?? Ahora recompila la app desktop:"
echo "   cd ../../../GestionTime.Desktop"
echo "   dotnet build"
echo "   dotnet run"
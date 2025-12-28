# Dockerfile optimizado para Render.com
# Build context: Raíz del repositorio (donde está GestionTime.sln)

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivo de solución
COPY GestionTime.sln ./

# Copiar archivos de proyecto para caché de capas
COPY GestionTime.Domain/*.csproj ./GestionTime.Domain/
COPY GestionTime.Application/*.csproj ./GestionTime.Application/
COPY GestionTime.Infrastructure/*.csproj ./GestionTime.Infrastructure/

# Buscar y copiar proyecto API desde cualquier ubicación
COPY GestionTime.Api/GestionTime.Api/*.csproj ./GestionTime.Api/ 2>/dev/null || \
     find . -name "Program.cs" -path "*/GestionTime.Api*" -exec dirname {} \; | head -1 | xargs -I {} cp {}/*.csproj ./GestionTime.Api/ || \
     echo "Proyecto API no encontrado en estructura esperada"

# Restaurar dependencias
RUN dotnet restore GestionTime.sln || \
    find . -name "*.csproj" -path "*Api*" | head -1 | xargs dotnet restore

# Copiar todo el código fuente
COPY . .

# Encontrar y publicar el proyecto API
RUN if [ -f "Program.cs" ]; then \
        echo "Publicando desde raíz con Program.cs" && \
        find . -name "*.csproj" -path "*Api*" | head -1 | xargs dotnet publish -c Release -o /app/publish; \
    elif [ -f "GestionTime.Api/GestionTime.Api/GestionTime.Api.csproj" ]; then \
        echo "Publicando desde GestionTime.Api/GestionTime.Api/" && \
        dotnet publish GestionTime.Api/GestionTime.Api/GestionTime.Api.csproj -c Release -o /app/publish; \
    else \
        echo "Buscando cualquier proyecto API..." && \
        find . -name "*.csproj" -exec grep -l "Microsoft.NET.Sdk.Web" {} \; | head -1 | xargs dotnet publish -c Release -o /app/publish; \
    fi

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Instalar curl para health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copiar aplicación publicada
COPY --from=build /app/publish .

# Crear directorio de logs
RUN mkdir -p /app/logs && chmod 755 /app/logs

# Variables de entorno para Render
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT

# Exponer puerto (Render usa la variable PORT)
EXPOSE $PORT

# Health check específico para Render
HEALTHCHECK --interval=30s --timeout=10s --start-period=20s --retries=3 \
    CMD curl -f http://localhost:$PORT/health || exit 1

# Buscar y ejecutar el DLL correcto
CMD find . -name "*.dll" -path "*Api*" | head -1 | xargs dotnet || \
    find . -name "GestionTime.Api.dll" | head -1 | xargs dotnet || \
    dotnet GestionTime.Api.dll
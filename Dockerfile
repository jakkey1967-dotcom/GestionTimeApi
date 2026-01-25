# Dockerfile para Render - Optimizado
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Copiar archivos de solucion y proyectos
COPY GestionTime.sln ./
COPY GestionTime.Api.csproj ./
COPY GestionTime.Domain/*.csproj ./GestionTime.Domain/
COPY GestionTime.Application/*.csproj ./GestionTime.Application/
COPY GestionTime.Infrastructure/*.csproj ./GestionTime.Infrastructure/

# Restaurar dependencias
RUN dotnet restore GestionTime.sln

# Copiar codigo fuente
COPY . .

# Limpiar solo directorios bin para evitar conflictos (mantener obj para assets)
RUN rm -rf bin GestionTime.Api/bin

# Compilar aplicacion sin --no-restore para regenerar assets si es necesario
RUN dotnet publish GestionTime.Api.csproj -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Instalar curl y crear directorio logs
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/* && \
    mkdir -p /app/logs && chmod 777 /app/logs

COPY --from=build /app/publish .

# Variables de entorno
ENV ASPNETCORE_ENVIRONMENT=Production

# Render usa la variable PORT dinamicamente
# Si PORT no existe, usar 8080 por defecto
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}

# Exponer puerto 8080 (Render lo mapea automaticamente)
EXPOSE 8080

ENTRYPOINT ["dotnet", "GestionTime.Api.dll"]

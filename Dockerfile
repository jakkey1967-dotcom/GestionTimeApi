# Dockerfile optimizado para Render.com (corrige problemas de tipos duplicados)
# Build context: Raíz del repositorio

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivo de solución
COPY GestionTime.sln ./

# Copiar archivos de proyecto para caché de capas
COPY GestionTime.Domain/*.csproj ./GestionTime.Domain/
COPY GestionTime.Application/*.csproj ./GestionTime.Application/
COPY GestionTime.Infrastructure/*.csproj ./GestionTime.Infrastructure/
COPY *.csproj ./

# Restaurar dependencias
RUN dotnet restore GestionTime.sln

# Copiar todo el código fuente
COPY . .

# Construir y publicar la aplicación (ignorando warnings CS0436)
RUN dotnet publish GestionTime.Api.csproj -c Release -o /app/publish \
    --no-restore \
    --verbosity quiet \
    -p:WarningsAsErrors="" \
    -p:TreatWarningsAsErrors=false \
    -nowarn:CS0436

# Runtime stage
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

# Exponer puerto
EXPOSE $PORT

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=20s --retries=3 \
    CMD curl -f http://localhost:$PORT/health || exit 1

# Ejecutar aplicación
ENTRYPOINT ["dotnet", "GestionTime.Api.dll"]
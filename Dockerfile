# Dockerfile para Render.com - BUILD TIMESTAMP FORCE REBUILD
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# FORCE REBUILD - Cache invalidation
ARG BUILDTIME_CACHE_BUST=1
RUN echo "Timestamp cache bust: $(date '+%Y%m%d_%H%M%S')" > /tmp/cache_bust_${BUILDTIME_CACHE_BUST}

# Establecer directorio de trabajo
WORKDIR /app

# Copiar archivos de proyecto uno por uno para mejor caching
COPY ["GestionTime.Api.csproj", "./"]
COPY ["GestionTime.sln", "./"]

# Copiar proyectos dependientes
COPY ["GestionTime.Domain/GestionTime.Domain.csproj", "GestionTime.Domain/"]
COPY ["GestionTime.Application/GestionTime.Application.csproj", "GestionTime.Application/"]  
COPY ["GestionTime.Infrastructure/GestionTime.Infrastructure.csproj", "GestionTime.Infrastructure/"]

# Restaurar dependencias
RUN dotnet restore "GestionTime.Api.csproj"

# Copiar código fuente
COPY . .

# Verificar que estamos compilando el proyecto correcto
RUN ls -la && echo "Building GestionTime.Api.csproj..."

# Publicar aplicación
RUN dotnet publish "GestionTime.Api.csproj" -c Release -o /app/publish --no-restore

# Runtime image  
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Instalar curl para health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copiar archivos publicados
COPY --from=build /app/publish .

# Verificar archivos publicados
RUN ls -la && echo "Published files ready"

# Variables de entorno
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT

# Ejecutar aplicación
ENTRYPOINT ["dotnet", "GestionTime.Api.dll"]
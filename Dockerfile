# Dockerfile optimizado para Render.com
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Cache busting para forzar rebuild
ARG BUILD_DATE
RUN echo "Build date: $BUILD_DATE" > /tmp/builddate.txt

# Copiar archivos de configuración del proyecto
COPY ["GestionTime.Api.csproj", "./"]
COPY ["GestionTime.Domain/GestionTime.Domain.csproj", "GestionTime.Domain/"]
COPY ["GestionTime.Application/GestionTime.Application.csproj", "GestionTime.Application/"]
COPY ["GestionTime.Infrastructure/GestionTime.Infrastructure.csproj", "GestionTime.Infrastructure/"]
COPY ["GestionTime.sln", "./"]

# Restaurar dependencias
RUN dotnet restore "GestionTime.Api.csproj"

# Copiar el resto del código fuente
COPY . .

# Cambiar al directorio del proyecto principal
WORKDIR "/src"

# Publicar aplicación
RUN dotnet publish "GestionTime.Api.csproj" -c Release -o /app/publish \
    --no-restore \
    --verbosity minimal \
    /p:TreatWarningsAsErrors=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Instalar curl para health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Crear directorio de logs con permisos
RUN mkdir -p /app/logs && chmod 755 /app/logs

# Copiar archivos publicados
COPY --from=build /app/publish .

# Variables de entorno para Render
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT

# Ejecutar aplicación
ENTRYPOINT ["dotnet", "GestionTime.Api.dll"]
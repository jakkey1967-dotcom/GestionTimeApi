# Dockerfile para Render.com - Estructura simplificada
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Establecer directorio de trabajo
WORKDIR /app

# Copiar archivos de proyecto
COPY *.csproj ./
COPY GestionTime.Domain/*.csproj GestionTime.Domain/
COPY GestionTime.Application/*.csproj GestionTime.Application/
COPY GestionTime.Infrastructure/*.csproj GestionTime.Infrastructure/
COPY GestionTime.sln ./

# Restaurar dependencias
RUN dotnet restore

# Copiar todo el código
COPY . ./

# Publicar aplicación
RUN dotnet publish -c Release -o out

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Instalar curl para health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copiar archivos publicados
COPY --from=build /app/out .

# Variables de entorno
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT

# Ejecutar aplicación  
CMD ["dotnet", "GestionTime.Api.dll"]
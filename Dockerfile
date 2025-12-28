# Dockerfile V2 CLEAN - Sin referencias a AddNpgSql
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Cache busting para forzar rebuild completo
RUN echo "REBUILD_$(date +%s)" > /tmp/rebuild_marker

# Copiar y restaurar dependencias
COPY *.csproj ./
COPY *.sln ./
COPY GestionTime.Domain/*.csproj ./GestionTime.Domain/
COPY GestionTime.Application/*.csproj ./GestionTime.Application/
COPY GestionTime.Infrastructure/*.csproj ./GestionTime.Infrastructure/

RUN dotnet restore

# Copiar código fuente
COPY . .

# CRÍTICO: Verificar que el código no tiene AddNpgSql
RUN echo "=== VERIFICANDO CÓDIGO ===" && \
    if grep -r "AddNpgSql" .; then \
        echo "ERROR: AddNpgSql encontrado en código!" && exit 1; \
    else \
        echo "? Código limpio sin AddNpgSql"; \
    fi

# Compilar aplicación
RUN dotnet publish -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Instalar curl y crear directorio logs
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/* && \
    mkdir -p /app/logs

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT

ENTRYPOINT ["dotnet", "GestionTime.Api.dll"]
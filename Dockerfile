# Dockerfile V2 CLEAN - Sin referencias a AddNpgSql
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

# Limpiar directorios bin y obj para evitar conflictos
RUN rm -rf bin obj GestionTime.Api/bin GestionTime.Api/obj

# Verificar que el codigo C# no tiene referencias problematicas
RUN echo "=== VERIFICANDO CODIGO ===" && \
    if grep -r "AddNpgSql" --include="*.cs" .; then \
        echo "ERROR: AddNpgSql encontrado en codigo C#!" && exit 1; \
    else \
        echo "OK: Codigo limpio sin AddNpgSql"; \
    fi

# Compilar aplicacion directamente sin clean
RUN dotnet publish GestionTime.Api.csproj -c Release -o /app/publish --no-restore

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
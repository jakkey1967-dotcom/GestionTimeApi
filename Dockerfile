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

# Limpiar solo directorios bin para evitar conflictos (mantener obj para assets)
RUN rm -rf bin GestionTime.Api/bin

# Verificar que el codigo C# no tiene referencias problematicas
# NOTA: Excluimos Migrations porque son auto-generadas y contienen NpgsqlExtensions
RUN echo "=== VERIFICANDO CODIGO ===" && \
    if find . -name "*.cs" -type f ! -path "*/Migrations/*" -exec grep -l "AddNpgSql" {} \; | grep -q .; then \
        echo "ERROR: AddNpgSql encontrado en codigo C#!" && exit 1; \
    else \
        echo "OK: Codigo limpio sin AddNpgSql"; \
    fi

# Compilar aplicacion sin --no-restore para regenerar assets si es necesario
RUN dotnet publish GestionTime.Api.csproj -c Release -o /app/publish

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
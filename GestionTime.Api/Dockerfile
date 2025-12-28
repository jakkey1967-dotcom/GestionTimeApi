# Dockerfile for GestionTime API
# Build context: Root of repository (C:\GestionTime\src)

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY GestionTime.sln ./

# Copy project files for layer caching
COPY GestionTime.Domain/GestionTime.Domain.csproj ./GestionTime.Domain/
COPY GestionTime.Application/GestionTime.Application.csproj ./GestionTime.Application/
COPY GestionTime.Infrastructure/GestionTime.Infrastructure.csproj ./GestionTime.Infrastructure/

# Check if API project is in subdirectory or root
COPY GestionTime.Api/GestionTime.Api/*.csproj ./GestionTime.Api/ 2>/dev/null || true
COPY *.csproj ./ 2>/dev/null || true

# Restore dependencies using solution file
RUN dotnet restore GestionTime.sln

# Copy all source code
COPY . .

# Build and publish the main API project (try both locations)
RUN if [ -f "GestionTime.Api/GestionTime.Api/GestionTime.Api.csproj" ]; then \
        echo "Building from GestionTime.Api/GestionTime.Api/" && \
        dotnet publish GestionTime.Api/GestionTime.Api/GestionTime.Api.csproj -c Release -o /app/publish --no-restore; \
    elif [ -f "Program.cs" ]; then \
        echo "Building from root with Program.cs" && \
        find . -name "*.csproj" -path "*/GestionTime.Api*" | head -1 | xargs dotnet publish -c Release -o /app/publish --no-restore; \
    else \
        echo "Building any API project found" && \
        find . -name "*.csproj" -exec grep -l "Microsoft.NET.Sdk.Web" {} \; | head -1 | xargs dotnet publish -c Release -o /app/publish --no-restore; \
    fi

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /app/logs && chmod 755 /app/logs

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production

# Use PORT environment variable (for Render.com)
EXPOSE $PORT

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=20s --retries=3 \
    CMD curl -f http://localhost:$PORT/health || exit 1

# Start the application  
ENTRYPOINT ["dotnet", "GestionTime.Api.dll"]
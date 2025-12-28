# Multi-project Dockerfile for GestionTime solution
# Build context should be from src/ folder (root of repository)

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all project files for better layer caching
COPY GestionTime.Domain/*.csproj ./GestionTime.Domain/
COPY GestionTime.Application/*.csproj ./GestionTime.Application/
COPY GestionTime.Infrastructure/*.csproj ./GestionTime.Infrastructure/
COPY GestionTime.Api/*.csproj ./GestionTime.Api/

# Restore dependencies
WORKDIR /src/GestionTime.Api
RUN dotnet restore

# Copy all source code
WORKDIR /src
COPY GestionTime.Domain/ ./GestionTime.Domain/
COPY GestionTime.Application/ ./GestionTime.Application/
COPY GestionTime.Infrastructure/ ./GestionTime.Infrastructure/
COPY GestionTime.Api/ ./GestionTime.Api/

# Build and publish
WORKDIR /src/GestionTime.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

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
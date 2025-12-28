# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first
COPY *.csproj ./
COPY ../GestionTime.Domain/*.csproj ../GestionTime.Domain/
COPY ../GestionTime.Application/*.csproj ../GestionTime.Application/
COPY ../GestionTime.Infrastructure/*.csproj ../GestionTime.Infrastructure/

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .
COPY ../GestionTime.Domain/ ../GestionTime.Domain/
COPY ../GestionTime.Application/ ../GestionTime.Application/
COPY ../GestionTime.Infrastructure/ ../GestionTime.Infrastructure/

# Build the application
RUN dotnet publish GestionTime.Api.csproj -c Release -o /app/publish

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

# Render uses the PORT environment variable
EXPOSE $PORT

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=20s --retries=3 \
    CMD curl -f http://localhost:$PORT/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "GestionTime.Api.dll"]
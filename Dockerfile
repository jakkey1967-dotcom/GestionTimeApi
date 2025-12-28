# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY GestionTime.Api.csproj .
COPY ../GestionTime.Domain/GestionTime.Domain.csproj ../GestionTime.Domain/
COPY ../GestionTime.Application/GestionTime.Application.csproj ../GestionTime.Application/
COPY ../GestionTime.Infrastructure/GestionTime.Infrastructure.csproj ../GestionTime.Infrastructure/

# Restore dependencies
RUN dotnet restore GestionTime.Api.csproj

# Copy all source code
COPY . .
COPY ../GestionTime.Domain/ ../GestionTime.Domain/
COPY ../GestionTime.Application/ ../GestionTime.Application/
COPY ../GestionTime.Infrastructure/ ../GestionTime.Infrastructure/

# Build the application
RUN dotnet publish GestionTime.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install required packages for PostgreSQL
RUN apt-get update && apt-get install -y \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "GestionTime.Api.dll"]
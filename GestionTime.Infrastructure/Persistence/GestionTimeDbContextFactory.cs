using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GestionTime.Infrastructure.Persistence;

/// <summary>
/// Factory para crear instancias de GestionTimeDbContext en tiempo de diseño (migraciones)
/// </summary>
public class GestionTimeDbContextFactory : IDesignTimeDbContextFactory<GestionTimeDbContext>
{
    public GestionTimeDbContext CreateDbContext(string[] args)
    {
        // Buscar appsettings.Development.json en el directorio correcto (GestionTimeApi/)
        var basePath = Path.Combine(Directory.GetCurrentDirectory());
        
        // Si estamos en GestionTime.Infrastructure, subir un nivel
        if (basePath.EndsWith("GestionTime.Infrastructure"))
        {
            basePath = Path.Combine(basePath, "..");
        }
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        // Obtener connection string y schema
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found");

        var schema = configuration["Database:Schema"] ?? "pss_dvnx";

        // Configurar DbContext
        var optionsBuilder = new DbContextOptionsBuilder<GestionTimeDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        // Crear schema config
        var schemaConfig = new DatabaseSchemaConfig { Schema = schema };

        return new GestionTimeDbContext(optionsBuilder.Options, schemaConfig);
    }
}

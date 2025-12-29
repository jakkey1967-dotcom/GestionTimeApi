using GestionTime.Api.Logging;
using GestionTime.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

// Configuración temprana de Serilog para capturar errores de arranque
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando GestionTime API...");

    var builder = WebApplication.CreateBuilder(args);

    // ?? Configurar URLs para Render.com (usa PORT de variable de entorno)
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

    // Configurar Serilog con archivos separados
    SerilogConfiguration.ConfigureSerilog(builder);

    // Controllers
    builder.Services.AddControllers();

    // Health checks (básico para compatibilidad con Docker)
    builder.Services.AddHealthChecks();

    // CORS (para navegador + cookies)
    var corsOrigins = builder.Configuration
        .GetSection("Cors:Origins")
        .Get<string[]>() ?? Array.Empty<string>();

    builder.Services.AddCors(opt =>
    {
        opt.AddPolicy("WebClient", p => p
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    });

    Log.Debug("CORS configurado para orígenes: {Origins}", string.Join(", ", corsOrigins));

    // JWT (leído desde cookie "access_token")
    var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
    var jwtAudience = builder.Configuration["Jwt:Audience"]!;
    var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? builder.Configuration["Jwt:Key"]!;

    builder.Services.AddSingleton<GestionTime.Api.Security.RefreshTokenService>();
    builder.Services.AddSingleton<GestionTime.Api.Security.JwtService>();

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            // Lee el JWT desde cookie HttpOnly
            opt.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    if (ctx.Request.Cookies.TryGetValue("access_token", out var token))
                        ctx.Token = token;

                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    // Configurar Identity Options
    builder.Services.Configure<Microsoft.AspNetCore.Identity.IdentityOptions>(options =>
    {
        // HABILITAR verificación de email (para testing del sistema de activación)
        options.SignIn.RequireConfirmedEmail = true;
        
        // Relajar requisitos de contraseña para desarrollo
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    });

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // ? DbContext con conversión de DATABASE_URL de Render
    var connectionString = GetConnectionString(builder.Configuration);
    Log.Information("Usando connection string (oculto por seguridad)");
    
    builder.Services.AddDbContext<GestionTimeDbContext>(opt =>
        opt.UseNpgsql(connectionString));

    // Memory Cache
    builder.Services.AddMemoryCache();

    // Servicios de email y verificación
    builder.Services.AddScoped<GestionTime.Api.Services.ResetTokenService>();
    builder.Services.AddScoped<GestionTime.Api.Services.EmailVerificationTokenService>();
    builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, GestionTime.Api.Services.SmtpEmailService>(); 

    var app = builder.Build();

    // Request logging middleware de Serilog
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondió {StatusCode} en {Elapsed:0.0000} ms";
    });

    // Seed
    Log.Information("Ejecutando seed de base de datos...");
    await GestionTime.Api.Startup.DbSeeder.SeedAsync(app.Services);
    Log.Information("Seed completado");

    // Health checks endpoint
    app.MapHealthChecks("/health");

    // ? ENDPOINT RAÍZ - Redirigir a Swagger
    app.MapGet("/", () => Results.Redirect("/swagger"))
        .ExcludeFromDescription();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        // En producción también habilitar Swagger para debugging
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "GestionTime API v1");
            c.RoutePrefix = "swagger";
        });
    }

    // No usar HTTPS redirect en Render (ellos manejan SSL)
    if (!app.Environment.IsProduction())
    {
        app.UseHttpsRedirection();
    }

    // Servir archivos estáticos (logos, imágenes)
    app.UseStaticFiles();

    app.UseCors("WebClient");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    Log.Information("GestionTime API iniciada correctamente en puerto {Port}", port);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación falló al iniciar");
    throw;
}
finally
{
    SerilogConfiguration.CloseAndFlush();
}

/// <summary>
/// Convierte DATABASE_URL de Render (postgresql://...) a formato Npgsql connection string
/// </summary>
static string GetConnectionString(IConfiguration configuration)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    
    // Si DATABASE_URL existe y es formato URL de Render
    if (!string.IsNullOrEmpty(databaseUrl) && databaseUrl.StartsWith("postgresql://"))
    {
        Log.Information("Detectado DATABASE_URL en formato Render, convirtiendo...");
        
        try
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            
            var connectionString = $"Host={uri.Host};" +
                                 $"Port={uri.Port};" +
                                 $"Database={uri.AbsolutePath.TrimStart('/')};" +
                                 $"Username={userInfo[0]};" +
                                 $"Password={userInfo[1]};" +
                                 $"SslMode=Require;";
            
            Log.Information("Connection string convertido exitosamente");
            return connectionString;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error convirtiendo DATABASE_URL, usando connection string de configuración");
        }
    }
    
    // Fallback: usar connection string de appsettings
    return configuration.GetConnectionString("Default") 
           ?? throw new InvalidOperationException("No se encontró connection string");
}


using GestionTime.Api.Logging;
using GestionTime.Api.Middleware;
using GestionTime.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using System.Text;
using Npgsql;

// ? FORZAR UTF-8 en consola para emojis y caracteres especiales
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// Si se invoca con argumentos "export-schema", ejecutar herramienta de exportación
if (args.Length > 0 && args[0] == "export-schema")
{
    var schemaArgs = args.Skip(1).ToArray();
    await GestionTime.Api.Tools.ExportSchema.Main(schemaArgs);
    return;
}

// Si se invoca con argumentos "import-schema", ejecutar herramienta de importación
if (args.Length > 0 && args[0] == "import-schema")
{
    var schemaArgs = args.Skip(1).ToArray();
    await GestionTime.Api.Tools.ImportSchema.Main(schemaArgs);
    return;
}

// Si se invoca con argumentos "sync-schema", sincronizar directamente entre BDs
if (args.Length > 0 && args[0] == "sync-schema")
{
    var schemaArgs = args.Skip(1).ToArray();
    await GestionTime.Api.Tools.SyncSchema.Main(schemaArgs);
    return;
}

// Si se invoca con argumentos "check-render", verificar estado de Render
if (args.Length > 0 && args[0] == "check-render")
{
    var schemaArgs = args.Skip(1).ToArray();
    await GestionTime.Api.Tools.CheckRender.Main(schemaArgs);
    return;
}

// Si se invoca con argumentos "clean-render", limpiar base de datos de Render
if (args.Length > 0 && args[0] == "clean-render")
{
    var schemaArgs = args.Skip(1).ToArray();
    await GestionTime.Api.Tools.CleanRender.Main(schemaArgs);
    return;
}

// Si se invoca con argumentos "deploy-render", deployment automatizado completo
if (args.Length > 0 && args[0] == "deploy-render")
{
    var schemaArgs = args.Skip(1).ToArray();
    await GestionTime.Api.Tools.DeployToRender.Main(schemaArgs);
    return;
}

// Si se invoca con argumentos "seed-admin", crear usuario admin en Render
if (args.Length > 0 && args[0] == "seed-admin")
{
    await GestionTime.Api.Tools.SeedAdminUser.Main(args.Skip(1).ToArray());
    return;
}

// Si se invoca con argumentos "execute-sql", ejecutar script SQL en Render
if (args.Length > 0 && args[0] == "execute-sql")
{
    await GestionTime.Api.Tools.ExecuteSql.Main(args.Skip(1).ToArray());
    return;
}

// Si se invoca con argumentos "verify-password", verificar hash BCrypt
if (args.Length > 0 && args[0] == "verify-password")
{
    await GestionTime.Api.Tools.VerifyPassword.Main(args.Skip(1).ToArray());
    return;
}

// Si se invoca con argumentos "reset-password", resetear contraseña de usuario
if (args.Length > 0 && args[0] == "reset-password")
{
    await GestionTime.Api.Tools.ResetUserPassword.Main(args.Skip(1).ToArray());
    return;
}

// Si se invoca con argumentos "backup-client", hacer backup completo
if (args.Length > 0 && args[0] == "backup-client")
{
    await GestionTime.Api.Tools.BackupClient.Main(args.Skip(1).ToArray());
    return;
}

// Configuración temprana de Serilog para capturar errores de arranque
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando GestionTime API...");

    var builder = WebApplication.CreateBuilder(args);

    // ?? Configurar URLs para Render.com (usa PORT de variable de entorno)
    var port = Environment.GetEnvironmentVariable("PORT") ?? "2501";
    var isRender = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT"));
    var isProduction = !builder.Environment.IsDevelopment();
    
    // En Render o Production, solo HTTP en contenedor (Render maneja HTTPS en proxy)
    if (isRender || isProduction)
    {
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        Log.Information("Configurado para producción/Render: HTTP={HttpPort} (HTTPS manejado por proxy)", port);
        
        // ? FORZAR HTTPS para URLs generadas (emails, redirects, etc.)
        builder.Services.Configure<Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionOptions>(options =>
        {
            options.HttpsPort = 443; // Puerto HTTPS estándar
        });
        
        // ? Configurar para que la app sepa que está detrás de un proxy HTTPS
        builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }
    else
    {
        // En desarrollo local, permitir HTTP y HTTPS
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}", $"https://0.0.0.0:2502");
        Log.Information("Configurado para desarrollo local: HTTP={HttpPort}, HTTPS={HttpsPort}", port, "2502");
    }

    // ? Configurar Serilog con consola Y archivo
    builder.Host.UseSerilog((context, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "GestionTime")
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");

        Log.Information("? Serilog configurado: Consola + Archivo (logs/log-.txt)");
    });

    // Controllers con filtro de validación personalizado
    builder.Services.AddControllers(options =>
    {
        // Agregar filtro global de logging de validación
        options.Filters.Add<GestionTime.Api.Filters.ValidationLoggingFilter>();
    })
    .AddJsonOptions(options =>
    {
        // ? Permitir case-insensitive en propiedades JSON (nombre = Nombre)
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Personalizar la respuesta automática de validación
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage ?? e.Exception?.Message ?? "Error de validación").ToArray()
                );

            // Determinar qué endpoint falló para dar ejemplo apropiado
            var routeData = context.HttpContext.GetRouteData();
            var controller = routeData.Values["controller"]?.ToString();
            var action = routeData.Values["action"]?.ToString();

            object? example = null;
            string? suggestion = null;

            if (controller == "Tipos")
            {
                if (action == "Create")
                {
                    example = new { Nombre = "Instalación", Descripcion = "Trabajos de instalación de equipos" };
                    suggestion = "Envía un JSON con las propiedades en PascalCase: Nombre (requerido, max 120 chars) y Descripcion (opcional, max 500 chars)";
                }
                else if (action == "Update")
                {
                    example = new { Nombre = "Instalación y Configuración", Descripcion = "Trabajos completos" };
                    suggestion = "PUT requiere el objeto completo. Envía todas las propiedades en PascalCase";
                }
            }
            else if (controller == "Grupos")
            {
                if (action == "Create")
                {
                    example = new { Nombre = "Soporte Premium", Descripcion = "Clientes premium 24/7" };
                    suggestion = "Envía un JSON con las propiedades en PascalCase: Nombre (requerido, max 120 chars) y Descripcion (opcional, max 500 chars)";
                }
                else if (action == "Update")
                {
                    example = new { Nombre = "Soporte VIP", Descripcion = "Clientes VIP prioritarios" };
                    suggestion = "PUT requiere el objeto completo. Envía todas las propiedades en PascalCase";
                }
            }

            var problemDetails = new GestionTime.Api.Filters.CustomValidationProblemDetails(errors, suggestion, example);

            return new BadRequestObjectResult(problemDetails);
        };
    });

    // Registrar el filtro de validación
    builder.Services.AddScoped<GestionTime.Api.Filters.ValidationLoggingFilter>();

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
                    // ? PRIORIDAD 1: Leer desde header Authorization: Bearer {token}
                    // Esto es lo que envía el Desktop y aplicaciones móviles
                    var authHeader = ctx.Request.Headers["Authorization"].ToString();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Token = authHeader.Substring("Bearer ".Length).Trim();
                        return Task.CompletedTask;
                    }

                    // ? PRIORIDAD 2: Leer desde cookie "access_token" (para navegadores web)
                    if (ctx.Request.Cookies.TryGetValue("access_token", out var cookieToken))
                    {
                        ctx.Token = cookieToken;
                        return Task.CompletedTask;
                    }

                    // ?? No se encontró token en ningún lugar
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    // Configurar Identity Options
    builder.Services.Configure<Microsoft.AspNetCore.Identity.IdentityOptions>(options =>
    {
        // ?? DESHABILITAR verificación de email temporalmente (mientras se soluciona SMTP)
        options.SignIn.RequireConfirmedEmail = false;
        
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
    
    // ? Obtener schema desde variable de entorno o configuración
    var dbSchema = Environment.GetEnvironmentVariable("DB_SCHEMA") 
                   ?? builder.Configuration["Database:Schema"] 
                   ?? "pss_dvnx";
    
    Log.Information("Schema de base de datos: {Schema}", dbSchema);
    
    // ? ASEGURAR QUE LA BD SEA SIEMPRE pss_dvnx Y CONFIGURAR SEARCH PATH
    var csBuilder = new NpgsqlConnectionStringBuilder(connectionString);
    if (csBuilder.Database != "pss_dvnx")
    {
        Log.Warning("??  Ajustando base de datos de '{OldDb}' a 'pss_dvnx'", csBuilder.Database);
        csBuilder.Database = "pss_dvnx";
    }
    
    // ? CONFIGURAR SEARCH PATH para que encuentre las tablas en el esquema correcto
    if (string.IsNullOrEmpty(csBuilder.SearchPath))
    {
        csBuilder.SearchPath = dbSchema;
        Log.Information("? Search Path configurado: {SearchPath}", dbSchema);
    }
    else
    {
        Log.Information("??  Search Path ya configurado: {SearchPath}", csBuilder.SearchPath);
    }
    
    connectionString = csBuilder.ToString();
    
    Log.Information("?? Base de datos: pss_dvnx | Schema: {Schema}", dbSchema);
    
    // ? CREAR BASE DE DATOS Y SCHEMA SI NO EXISTEN (solo en desarrollo local)
    if (builder.Environment.IsDevelopment() && connectionString.Contains("localhost"))
    {
        await EnsureDatabaseAndSchemaExistAsync(connectionString, dbSchema);
    }
    else
    {
        Log.Information("??  Saltando verificación de BD (conexión remota)");
    }
    
    builder.Services.AddDbContext<GestionTimeDbContext>((serviceProvider, opt) =>
    {
        opt.UseNpgsql(connectionString);
    });
    
    // Configurar el schema en el DbContext mediante DI
    builder.Services.AddSingleton(new DatabaseSchemaConfig { Schema = dbSchema });

    // Memory Cache
    builder.Services.AddMemoryCache();

    // Servicios de email y verificación
    builder.Services.AddScoped<GestionTime.Api.Services.ResetTokenService>();
    builder.Services.AddScoped<GestionTime.Api.Services.EmailVerificationTokenService>();
    builder.Services.AddScoped<GestionTime.Api.Services.IEmailService, GestionTime.Api.Services.SmtpEmailService>();
    
    
    // ? Servicio centralizado de configuración de clientes
    builder.Services.AddSingleton<GestionTime.Api.Services.ClientConfigurationService>();
    
    // ? CRUD Services para catálogos
    builder.Services.AddScoped<GestionTime.Api.Services.TipoService>();
    builder.Services.AddScoped<GestionTime.Api.Services.GrupoService>();
    
    // ? Freshdesk Integration
    builder.Services.Configure<GestionTime.Infrastructure.Services.Freshdesk.FreshdeskOptions>(
        builder.Configuration.GetSection(GestionTime.Infrastructure.Services.Freshdesk.FreshdeskOptions.SectionName));
    builder.Services.AddHttpClient<GestionTime.Infrastructure.Services.Freshdesk.FreshdeskClient>();
    builder.Services.AddScoped<GestionTime.Infrastructure.Services.Freshdesk.FreshdeskService>();
    builder.Services.AddScoped<GestionTime.Infrastructure.Services.Freshdesk.FreshdeskTicketHeaderSyncService>();
    builder.Services.AddScoped<GestionTime.Infrastructure.Services.Freshdesk.FreshdeskCompaniesSyncService>();
    builder.Services.AddScoped<GestionTime.Infrastructure.Services.Freshdesk.FreshdeskAgentMeSyncService>();
    builder.Services.AddScoped<GestionTime.Infrastructure.Services.Freshdesk.FreshdeskAgentsSyncService>();
    builder.Services.AddScoped<GestionTime.Api.Services.FreshdeskTicketSuggestService>();
    
    // ? Freshdesk Background Service - Sincronización automática de tags
    builder.Services.AddHostedService<GestionTime.Infrastructure.Services.Freshdesk.FreshdeskSyncBackgroundService>();
    
    // ? Data Protection - Persistir claves en PostgreSQL
    builder.Services.AddDataProtection()
        .PersistKeysToDbContext<GestionTimeDbContext>()
        .SetApplicationName("GestionTimeApi");

    var app = builder.Build();

    // ? Habilitar forwarded headers en producción (para HTTPS detrás de proxy)
    if (isRender || isProduction)
    {
        app.UseForwardedHeaders();
        Log.Information("? Forwarded headers habilitados (HTTPS via proxy)");
    }

    // ?? Verificar conexión a base de datos (SIN aplicar migraciones)
    try
    {
        Log.Information("?? Verificando conexión a base de datos...");
        
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GestionTimeDbContext>();
            
            var canConnect = await db.Database.CanConnectAsync();
            if (!canConnect)
            {
                Log.Error("? No se puede conectar a la base de datos");
                throw new Exception("No se puede conectar a la base de datos");
            }
            
            Log.Information("? Conexión a BD establecida");
            
            // ?? NO APLICAR MIGRACIONES AUTOMÁTICAMENTE
            // Si restauraste la BD, las tablas ya existen y no necesitas migraciones
            // Para aplicar migraciones manualmente usa: dotnet ef database update
            Log.Information("??  Migraciones deshabilitadas. Las tablas deben existir en la BD.");
            
            // Verificar que las tablas existen haciendo una query simple
            try
            {
                var userCount = await db.Users.CountAsync();
                Log.Information("? Base de datos operativa ({UserCount} usuarios)", userCount);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "? Error verificando tablas. ¿La BD está correctamente restaurada?");
                throw;
            }
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "? ERROR verificando base de datos");
        throw;
    }

    // ? Middleware de logging de request body (ANTES de Serilog)
    app.UseMiddleware<GestionTime.Api.Middleware.RequestBodyLoggingMiddleware>();

    // Request logging middleware de Serilog
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondió {StatusCode} en {Elapsed:0.0000} ms";
    });

    // ?? Seed automático con script SQL completo
    try
    {
        Log.Information("?? Ejecutando seed de base de datos...");
        await GestionTime.Api.Startup.DbSeeder.SeedAsync(app.Services);
        Log.Information("? Seed completado exitosamente");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "? Error durante el seed");
        
        // No fallar el arranque si es error de BD ya inicializada
        var message = ex.Message.ToLowerInvariant();
        var isDbAlreadySetup = message.Contains("already exists") || 
                               message.Contains("42p07") ||
                               message.Contains("usuario") ||
                               message.Contains("duplicate");
        
        if (isDbAlreadySetup)
        {
            Log.Warning("?? Base de datos ya inicializada. Continuando arranque...");
        }
        else
        {
            Log.Warning("?? Error en seed pero continuando arranke: {Message}", ex.Message);
        }
    }

    // ? Health checks endpoint con JSON detallado
    app.MapGet("/health", async (GestionTimeDbContext db, GestionTime.Api.Services.ClientConfigurationService clientConfig) =>
    {
        try
        {
            var canConnect = await db.Database.CanConnectAsync();
            var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
            
            // Usar servicio centralizado para obtener configuración del cliente
            var currentClient = clientConfig.GetCurrentClient();
            
            return Results.Ok(new
            {
                status = canConnect ? "OK" : "DEGRADED",
                timestamp = DateTime.UtcNow,
                service = "GestionTime API",
                version = "1.9.0",
                client = currentClient.Name,                    // ? Nombre descriptivo
                clientId = currentClient.Id,                    // ? ID técnico
                schema = clientConfig.GetDatabaseSchema(),      // ? Schema de BD
                environment = app.Environment.EnvironmentName,  // ? Development/Production
                uptime = $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s",
                database = canConnect ? "connected" : "disconnected",
                
                // ? Información adicional de configuración
                configuration = new
                {
                    jwtAccessMinutes = clientConfig.GetJwtConfig().AccessMinutes,
                    jwtRefreshDays = clientConfig.GetJwtConfig().RefreshDays,
                    emailConfirmationRequired = clientConfig.RequiresEmailConfirmation(),
                    selfRegistrationAllowed = clientConfig.AllowsSelfRegistration(),
                    passwordExpirationDays = clientConfig.GetPasswordExpirationDays(),
                    maxUsers = clientConfig.GetMaxUsersAllowed(),
                    maxStorageGB = clientConfig.GetMaxStorageGB(),
                    corsOriginsCount = clientConfig.GetCorsOrigins().Length
                }
            });
        }
        catch (Exception ex)
        {
            return Results.Json(new
            {
                status = "UNHEALTHY",
                timestamp = DateTime.UtcNow,
                service = "GestionTime API",
                database = "error",
                error = ex.Message
            }, statusCode: 503);
        }
    })
    .WithName("HealthCheck")
    .WithTags("Health")
    .Produces<object>(200)
    .Produces<object>(503)
    .ExcludeFromDescription();

    // ? ENDPOINT RAÍZ - Muestra información según el entorno
    app.MapGet("/", async (GestionTimeDbContext db, GestionTime.Api.Services.ClientConfigurationService clientConfig) =>
    {
        // En Development, mostrar diagnósticos completos
        if (app.Environment.IsDevelopment())
        {
            return await GetDiagnosticsPageAsync(db, app);
        }
        
        // Usar servicio centralizado para obtener logo
        var logoPath = clientConfig.GetLogoPath();
        
        // En Production, mostrar página simple y segura
        var html = $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>GestionTime API</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }}
        .container {{
            background: white;
            border-radius: 20px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            max-width: 600px;
            width: 100%;
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(135deg, #0B8C99 0%, #0A7A85 100%);
            color: white;
            padding: 40px;
            text-align: center;
        }}
        .logo-link {{
            display: inline-block;
            text-decoration: none;
        }}
        .logo {{
            max-width: 300px;
            height: auto;
            margin: 0 auto 15px auto;
            display: block;
        }}
        .header h1 {{
            font-size: 32px;
            font-weight: 600;
            margin-bottom: 10px;
        }}
        .header p {{
            opacity: 0.9;
            font-size: 16px;
        }}
        .content {{
            padding: 40px;
            text-align: center;
        }}
        .status {{
            display: inline-block;
            padding: 15px 30px;
            background: #d4edda;
            color: #155724;
            border-radius: 25px;
            font-size: 20px;
            font-weight: 600;
            margin-bottom: 30px;
        }}
        .links {{
            display: flex;
            gap: 15px;
            flex-wrap: wrap;
            justify-content: center;
        }}
        .link-button {{
            display: inline-block;
            padding: 12px 30px;
            background: linear-gradient(135deg, #0B8C99 0%, #0A7A85 100%);
            color: white;
            text-decoration: none;
            border-radius: 25px;
            font-weight: 600;
            transition: all 0.3s ease;
            box-shadow: 0 4px 15px rgba(0, 140, 153, 0.3);
        }}
        .link-button:hover {{
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(0, 140, 153, 0.4);
        }}
        .link-button.secondary {{
            background: linear-gradient(135deg, #6c757d 0%, #5a6268 100%);
            box-shadow: 0 4px 15px rgba(108, 117, 125, 0.3);
        }}
        .link-button.download {{
            background: linear-gradient(135deg, #28a745 0%, #218838 100%);
            box-shadow: 0 4px 15px rgba(40, 167, 69, 0.3);
        }}
        .link-button.download:hover {{
            box-shadow: 0 6px 20px rgba(40, 167, 69, 0.4);
        }}
        .icon {{
            display: inline-block;
            width: 18px;
            height: 18px;
            vertical-align: text-bottom;
            margin-right: 6px;
        }}
        .footer {{
            text-align: center;
            padding: 20px;
            background: #f8f9fa;
            color: #6c757d;
            font-size: 14px;
        }}
        @media (max-width: 600px) {{
            .logo {{
                max-width: 200px;
            }}
            .header h1 {{
                font-size: 24px;
            }}
            .content {{
                padding: 30px 20px;
            }}
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <a href=""https://gestiontimeapi.onrender.com"" target=""_blank"" rel=""noopener"" class=""logo-link"">
                <img src=""{logoPath}"" alt=""GestionTime"" class=""logo"" onerror=""this.src='/images/LogoOscuro.png'"" />
            </a>
            <h1>GestionTime API</h1>
            <p>Sistema de Gestión de Tiempo y Recursos</p>
        </div>
        <div class=""content"">
            <div class=""status"">
                <svg class=""icon"" viewBox=""0 0 24 24"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
                    <circle cx=""12"" cy=""12"" r=""10"" stroke=""#155724"" stroke-width=""2"" fill=""#d4edda""/>
                    <path d=""M7 12l3 3 7-7"" stroke=""#155724"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""/>
                </svg>
                API Online
            </div>
            <p style=""color: #6c757d; margin-bottom: 30px;"">La API está funcionando correctamente</p>
            <div class=""links"">
                <a href=""/swagger"" class=""link-button"">
                    <svg class=""icon"" viewBox=""0 0 24 24"" fill=""currentColor"" xmlns=""http://www.w3.org/2000/svg"">
                        <path d=""M4 19.5A2.5 2.5 0 0 1 6.5 17H20""/>
                        <path d=""M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"" stroke=""currentColor"" stroke-width=""2"" fill=""none""/>
                    </svg>
                    Documentación API
                </a>
                <a href=""/health"" class=""link-button secondary"">
                    <svg class=""icon"" viewBox=""0 0 24 24"" fill=""currentColor"" xmlns=""http://www.w3.org/2000/svg"">
                        <path d=""M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"" stroke=""currentColor"" stroke-width=""2"" fill=""none""/>
                    </svg>
                    Health Check
                </a>
                <a href=""https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/download/v1.9.0-beta/GestionTime-v1.9.3-beta.msi"" class=""link-button download"">
                    <svg class=""icon"" viewBox=""0 0 24 24"" fill=""currentColor"" xmlns=""http://www.w3.org/2000/svg"">
                        <path d=""M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"" stroke=""currentColor"" stroke-width=""2"" fill=""none""/>
                        <polyline points=""7 10 12 15 17 10"" stroke=""currentColor"" stroke-width=""2"" fill=""none""/>
                        <line x1=""12"" y1=""15"" x2=""12"" y2=""3"" stroke=""currentColor"" stroke-width=""2""/>
                    </svg>
                    Descargar App
                </a>
            </div>
        </div>
        <div class=""footer"">
            © 2025 GestionTime - Todos los derechos reservados<br>
            <small>Desarrollado por TDK Portal</small>
        </div>
    </div>
</body>
</html>";
        return Results.Content(html, "text/html; charset=utf-8");
    })
    .ExcludeFromDescription();

    // Swagger disponible en todos los entornos
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GestionTime API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "GestionTime API - Documentación";
        
        // ? Habilitar envío de cookies con credenciales
        c.ConfigObject.AdditionalItems["persistAuthorization"] = true;
        c.ConfigObject.AdditionalItems["withCredentials"] = true;
    });

    // HTTPS redirect solo en desarrollo local
    if (app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    
    // ? Servir archivos estáticos con prioridad por cliente
    var clientConfigService = app.Services.GetRequiredService<GestionTime.Api.Services.ClientConfigurationService>();
    
    Log.Information("+------------------------------------------------------+");
    Log.Information("¦     Configurando Archivos Estáticos Multi-Tenant     ¦");
    Log.Information("+------------------------------------------------------+");
    
    var currentClient = clientConfigService.GetCurrentClient();
    Log.Information("?? Cliente activo: {ClientId} ({ClientName})", currentClient.Id, currentClient.Name);
    Log.Information("???  Logo configurado: {Logo}", currentClient.Logo);
    Log.Information("?? Logo URL: {LogoPath}", clientConfigService.GetLogoPath());
    
    if (clientConfigService.HasClientSpecificWwwroot())
    {
        var clientWwwroot = clientConfigService.GetClientWwwrootPath();
        var clientWwwrootFullPath = Path.GetFullPath(clientWwwroot);
        
        // Verificar que el directorio existe
        if (Directory.Exists(clientWwwrootFullPath))
        {
            Log.Information("? 1?? Prioridad: {ClientPath}", clientWwwrootFullPath);
            
            // Verificar archivos en el directorio
            var imagesPath = Path.Combine(clientWwwrootFullPath, "images");
            if (Directory.Exists(imagesPath))
            {
                var files = Directory.GetFiles(imagesPath, "*.png");
                Log.Information("   ?? Archivos en images/: {Count} archivos", files.Length);
                foreach (var file in files.Take(5))
                {
                    var fileName = Path.GetFileName(file);
                    Log.Information("      • {FileName}", fileName);
                }
            }
            
            // Configurar middleware con prioridad alta
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(clientWwwrootFullPath),
                RequestPath = "",
                OnPrepareResponse = ctx =>
                {
                    Log.Information("? Sirviendo desde cliente: {Path}", ctx.File.Name);
                }
            });
        }
        else
        {
            Log.Warning("??  Carpeta cliente no encontrada: {Path}", clientWwwrootFullPath);
        }
        
        Log.Information("? 2?? Fallback: wwwroot (archivos comunes)");
    }
    else
    {
        Log.Information("??  No hay carpeta específica para el cliente, usando solo wwwroot común");
    }
    
    // SIEMPRE: Archivos comunes (fallback para todos los clientes)
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            Log.Information("?? Sirviendo desde común: {Path}", ctx.File.Name);
        }
    });
    
    Log.Information("------------------------------------------------------");

    // Pipeline de autenticación/autorización
    app.UseCors("WebClient");
    app.UseAuthentication();
    app.UseAuthorization();
    
    // ? Middleware de presencia (después de autenticación)
    app.UsePresenceTracking();

    app.MapControllers();
    
    // ? Health Check endpoint para Render
    app.MapHealthChecks("/health");

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
    
    // Si no hay DATABASE_URL, usar connection string de configuración
    if (string.IsNullOrEmpty(databaseUrl))
    {
        databaseUrl = configuration.GetConnectionString("Default");
    }
    
    // Si es null o vacío, lanzar error
    if (string.IsNullOrEmpty(databaseUrl))
    {
        throw new InvalidOperationException("No se encontró connection string");
    }
    
    // Si la cadena es formato URL de PostgreSQL, convertirla
    if (databaseUrl.StartsWith("postgresql://"))
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
            throw;
        }
    }
    
    // Si no es formato URL, devolverla tal cual (formato Npgsql estándar)
    return databaseUrl;
}

/// <summary>
/// Genera página de diagnósticos con información del sistema
/// </summary>
static async Task<IResult> GetDiagnosticsPageAsync(GestionTimeDbContext db, WebApplication app)
{
    var apiStatus = "<svg class='icon-inline' viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><circle cx='12' cy='12' r='10' stroke='#28a745' stroke-width='2' fill='none'/><path d='M7 12l3 3 7-7' stroke='#28a745' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'/></svg> Online";
    var apiStatusClass = "status-ok";
    
    var dbStatus = "<svg class='icon-inline' viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><circle cx='12' cy='12' r='10' stroke='#dc3545' stroke-width='2' fill='none'/><line x1='8' y1='8' x2='16' y2='16' stroke='#dc3545' stroke-width='2'/><line x1='16' y1='8' x2='8' y2='16' stroke='#dc3545' stroke-width='2'/></svg> Desconectado";
    var dbStatusClass = "status-error";
    var dbLatency = 0;
    var migrationsApplied = 0;
    var migrationsPending = 0;
    
    // Verificar Base de Datos CON latencia
    var sw = System.Diagnostics.Stopwatch.StartNew();
    try
    {
        await db.Database.CanConnectAsync();
        sw.Stop();
        dbLatency = (int)sw.ElapsedMilliseconds;
        
        var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        
        migrationsApplied = appliedMigrations.Count();
        migrationsPending = pendingMigrations.Count();
        
        dbStatus = "<svg class='icon-inline' viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><circle cx='12' cy='12' r='10' stroke='#28a745' stroke-width='2' fill='none'/><path d='M7 12l3 3 7-7' stroke='#28a745' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'/></svg> Conectado";
        dbStatusClass = "status-ok";
    }
    catch (Exception ex)
    {
        sw.Stop();
        dbLatency = (int)sw.ElapsedMilliseconds;
        dbStatus = $"<svg class='icon-inline' viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><circle cx='12' cy='12' r='10' stroke='#dc3545' stroke-width='2' fill='none'/><line x1='8' y1='8' x2='16' y2='16' stroke='#dc3545' stroke-width='2'/><line x1='16' y1='8' x2='8' y2='16' stroke='#dc3545' stroke-width='2'/></svg> Error: {ex.Message[..Math.Min(50, ex.Message.Length)]}...";
    }

    var process = System.Diagnostics.Process.GetCurrentProcess();
    var memoryUsedMB = (int)(process.WorkingSet64 / 1024 / 1024);
    var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
    var environment = app.Environment.EnvironmentName;
    var version = "1.0.0";
    
    var gcGen0 = GC.CollectionCount(0);
    var gcGen1 = GC.CollectionCount(1);
    var gcGen2 = GC.CollectionCount(2);

    var envBadge = environment == "Development" 
        ? "<span style=\"background: #ffc107; color: #000; padding: 5px 15px; border-radius: 15px; font-size: 12px; font-weight: 600; margin-left: 10px;\"><svg style='display:inline-block;width:14px;height:14px;vertical-align:middle;margin-right:4px;' viewBox='0 0 24 24' fill='currentColor' xmlns='http://www.w3.org/2000/svg'><path d='M22.7 19l-9.1-9.1c.9-2.3.4-5-1.5-6.9-2-2-5-2.4-7.4-1.3L9 6 6 9 1.6 4.7C.4 7.1.9 10.1 2.9 12.1c1.9 1.9 4.6 2.4 6.9 1.5l9.1 9.1c.4.4 1 .4 1.4 0l2.3-2.3c.5-.4.5-1.1.1-1.4z' stroke='currentColor' stroke-width='0.5' fill='currentColor'/></svg> DEV</span>"
        : "<span style=\"background: #28a745; color: #fff; padding: 5px 15px; border-radius: 15px; font-size: 12px; font-weight: 600; margin-left: 10px;\"><svg style='display:inline-block;width:14px;height:14px;vertical-align:middle;margin-right:4px;' viewBox='0 0 24 24' fill='currentColor' xmlns='http://www.w3.org/2000/svg'><path d='M5 3l3.057-3 11.943 12-11.943 12-3.057-3 9-9z' stroke='currentColor' stroke-width='1' fill='currentColor'/></svg> PROD</span>";

    var html = $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>GestionTime API - Diagnósticos del Sistema</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }}
        .container {{
            background: white;
            border-radius: 20px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            max-width: 900px;
            width: 100%;
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(135deg, #0B8C99 0%, #0A7A85 100%);
            color: white;
            padding: 40px;
            text-align: center;
        }}
        .logo {{
            max-width: 300px;
            height: auto;
            margin: 0 auto 15px auto;
            display: block;
        }}
        .header h1 {{
            font-size: 32px;
            font-weight: 600;
            margin-bottom: 10px;
        }}
        .header p {{
            opacity: 0.9;
            font-size: 16px;
        }}
        .content {{
            padding: 40px;
        }}
        .status-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 20px;
            margin-bottom: 30px;
        }}
        .status-card {{
            background: #f8f9fa;
            border-radius: 12px;
            padding: 25px;
            border-left: 4px solid #ddd;
            transition: transform 0.2s, box-shadow 0.2s;
        }}
        .status-card:hover {{
            transform: translateY(-5px);
            box-shadow: 0 10px 25px rgba(0,0,0,0.1);
        }}
        .status-card.status-ok {{
            border-left-color: #28a745;
        }}
        .status-card.status-error {{
            border-left-color: #dc3545;
        }}
        .status-card h3 {{
            font-size: 14px;
            color: #6c757d;
            text-transform: uppercase;
            letter-spacing: 1px;
            margin-bottom: 10px;
        }}
        .status-card .value {{
            font-size: 24px;
            font-weight: 600;
            color: #212529;
            margin-bottom: 5px;
        }}
        .status-card .detail {{
            font-size: 12px;
            color: #6c757d;
            margin-top: 8px;
        }}
        .info-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
            gap: 15px;
            margin-bottom: 30px;
        }}
        .info-item {{
            background: #e9ecef;
            padding: 15px 20px;
            border-radius: 8px;
        }}
        .info-item label {{
            font-size: 12px;
            color: #6c757d;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            display: block;
            margin-bottom: 5px;
        }}
        .info-item .value {{
            font-size: 16px;
            font-weight: 600;
            color: #212529;
        }}
        .badge {{
            display: inline-block;
            padding: 4px 10px;
            border-radius: 12px;
            font-size: 11px;
            font-weight: 600;
            margin-top: 5px;
        }}
        .badge-success {{
            background: #d4edda;
            color: #155724;
        }}
        .badge-warning {{
            background: #fff3cd;
            color: #856404;
        }}
        .footer {{
            text-align: center;
            padding: 20px;
            background: #f8f9fa;
            color: #6c757d;
            font-size: 14px;
        }}
        .links {{
            display: flex;
            gap: 15px;
            flex-wrap: wrap;
            justify-content: center;
        }}
        .link-button {{
            display: inline-block;
            padding: 12px 30px;
            background: linear-gradient(135deg, #0B8C99 0%, #0A7A85 100%);
            color: white;
            text-decoration: none;
            border-radius: 25px;
            font-weight: 600;
            transition: all 0.3s ease;
            box-shadow: 0 4px 15px rgba(11, 140, 153, 0.3);
        }}
        .link-button:hover {{
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(11, 140, 153, 0.4);
        }}
        .link-button.secondary {{
            background: linear-gradient(135deg, #6c757d 0%, #5a6268 100%);
            box-shadow: 0 4px 15px rgba(108, 117, 125, 0.3);
        }}
        .link-button.download {{
            background: linear-gradient(135deg, #28a745 0%, #218838 100%);
            box-shadow: 0 4px 15px rgba(40, 167, 69, 0.3);
        }}
        .link-button.download:hover {{
            box-shadow: 0 6px 20px rgba(40, 167, 69, 0.4);
        }}
        .icon {{
            display: inline-block;
            width: 18px;
            height: 18px;
            vertical-align: text-bottom;
            margin-right: 6px;
        }}
        .icon-inline {{
            display: inline-block;
            width: 20px;
            height: 20px;
            vertical-align: text-bottom;
            margin-right: 4px;
        }}
        @media (max-width: 600px) {{
            .logo {{ max-width: 200px; }}
            .header h1 {{ font-size: 24px; }}
            .content {{ padding: 30px 20px; }}
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <a href=""https://gestiontimeapi.onrender.com"" target=""_blank"" rel=""noopener"" style=""display:inline-block;text-decoration:none;"">
                <img src=""/images/LogoOscuro.png"" alt=""GestionTime"" class=""logo"" onerror=""this.style.display='none'"" />
            </a>
            <h1>GestionTime API {envBadge}</h1>
            <p>Sistema de Gestión de Tiempo y Recursos</p>
        </div>
        <div class=""content"">
            <div class=""status-grid"">
                <div class=""status-card {apiStatusClass}"">
                    <h3>Estado API</h3>
                    <div class=""value"">{apiStatus}</div>
                    <div class=""detail"">Respondiendo solicitudes</div>
                </div>
                <div class=""status-card {dbStatusClass}"">
                    <h3>Base de Datos</h3>
                    <div class=""value"">{dbStatus}</div>
                    <div class=""detail"">Latencia: {dbLatency}ms</div>
                    {(migrationsPending > 0 ? $@"<span class=""badge badge-warning""><svg style='display:inline-block;width:14px;height:14px;vertical-align:middle;margin-right:4px;' viewBox='0 0 24 24' fill='#856404' xmlns='http://www.w3.org/2000/svg'><path d='M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z'/></svg> {migrationsPending} pendiente(s)</span>" : $@"<span class=""badge badge-success""><svg style='display:inline-block;width:14px;height:14px;vertical-align:middle;margin-right:4px;' viewBox='0 0 24 24' fill='#155724' xmlns='http://www.w3.org/2000/svg'><circle cx='12' cy='12' r='10' fill='none' stroke='#155724' stroke-width='2'/><path d='M7 12l3 3 7-7' stroke='#155724' stroke-width='2' fill='none'/></svg> {migrationsApplied} aplicada(s)</span>")}
                </div>
            </div>
            <div class=""info-grid"">
                <div class=""info-item"">
                    <label>Versión</label>
                    <div class=""value"">v{version}</div>
                </div>
                <div class=""info-item"">
                    <label>Entorno</label>
                    <div class=""value"">{environment}</div>
                </div>
                <div class=""info-item"">
                    <label>Tiempo Activo</label>
                    <div class=""value"">{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m</div>
                </div>
                <div class=""info-item"">
                    <label>Memoria</label>
                    <div class=""value"">{memoryUsedMB} MB</div>
                </div>
                <div class=""info-item"">
                    <label>GC (Gen0/1/2)</label>
                    <div class=""value"">{gcGen0}/{gcGen1}/{gcGen2}</div>
                </div>
                <div class=""info-item"">
                    <label>Hora Server</label>
                    <div class=""value"">{DateTime.UtcNow:HH:mm:ss} UTC</div>
                </div>
            </div>
            <div class=""links"">
                <a href=""/swagger"" class=""link-button"">
                    <svg class=""icon"" viewBox=""0 0 24 24"" fill=""currentColor"" xmlns=""http://www.w3.org/2000/svg"">
                        <path d=""M4 19.5A2.5 2.5 0 0 1 6.5 17H20""/>
                        <path d=""M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"" stroke=""currentColor"" stroke-width=""2"" fill=""none""/>
                    </svg>
                    Documentación API
                </a>
                <a href=""/health"" class=""link-button secondary"">
                    <svg class=""icon"" viewBox=""0 0 24 24"" fill=""currentColor"" xmlns=""http://www.w3.org/2000/svg"">
                        <path d=""M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"" stroke=""currentColor"" stroke-width=""2"" fill=""none""/>
                    </svg>
                    Health Check
                </a>
                <a href=""https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/download/v1.9.0-beta/GestionTime-v1.9.3-beta.msi"" class=""link-button download"">
                    <svg class=""icon"" viewBox=""0 0 24 24"" fill=""currentColor"" xmlns=""http://www.w3.org/2000/svg"">
                        <path d=""M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"" stroke=""currentColor"" stroke-width=""2"" fill=""none""/>
                        <polyline points=""7 10 12 15 17 10"" stroke=""currentColor"" stroke-width=""2"" fill=""none""/>
                        <line x1=""12"" y1=""15"" x2=""12"" y2=""3"" stroke=""currentColor"" stroke-width=""2""/>
                    </svg>
                    Descargar App
                </a>
            </div>
        </div>
        <div class=""footer"">
            © {DateTime.Now.Year} GestionTime - Todos los derechos reservados<br>
            <small>Desarrollado por TDK Portal • Auto-refresh: 30s</small>
        </div>
    </div>
    <script>
        setTimeout(() => location.reload(), 30000);
    </script>
</body>
</html>";

    return Results.Content(html, "text/html; charset=utf-8");
}

/// <summary>
/// Asegura que la base de datos y el schema existan antes de continuar
/// </summary>
static async Task EnsureDatabaseAndSchemaExistAsync(string connectionString, string schema)
{
    try
    {
        Log.Information("?? Verificando base de datos pss_dvnx y schema {Schema}...", schema);
        
        // La BD ya debe ser pss_dvnx en este punto
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;
        
        if (databaseName != "pss_dvnx")
        {
            throw new InvalidOperationException($"Error de configuración: se esperaba 'pss_dvnx' pero se recibió '{databaseName}'");
        }
        
        // 1. Verificar/Crear la base de datos pss_dvnx
        var maintenanceDb = "postgres";
        var maintenanceConnString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = maintenanceDb
        }.ToString();
        
        await using (var conn = new NpgsqlConnection(maintenanceConnString))
        {
            await conn.OpenAsync();
            
            var checkDbCmd = new NpgsqlCommand(
                "SELECT 1 FROM pg_database WHERE datname = 'pss_dvnx'", 
                conn);
            
            var dbExists = await checkDbCmd.ExecuteScalarAsync() != null;
            
            if (!dbExists)
            {
                Log.Information("?? Creando base de datos 'pss_dvnx'...");
                var createDbCmd = new NpgsqlCommand("CREATE DATABASE pss_dvnx", conn);
                await createDbCmd.ExecuteNonQueryAsync();
                Log.Information("? Base de datos 'pss_dvnx' creada");
            }
            else
            {
                Log.Information("? Base de datos 'pss_dvnx' existe");
            }
        }
        
        // 2. Conectar a pss_dvnx y gestionar schemas
        await using (var conn = new NpgsqlConnection(connectionString))
        {
            await conn.OpenAsync();
            
            // 2.1 Verificar/Crear schema del cliente
            var checkSchemaCmd = new NpgsqlCommand(
                $"SELECT 1 FROM information_schema.schemata WHERE schema_name = '{schema}'", 
                conn);
            
            var schemaExists = await checkSchemaCmd.ExecuteScalarAsync() != null;
            
            if (!schemaExists)
            {
                Log.Information("?? Creando schema '{Schema}'...", schema);
                var createSchemaCmd = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", conn);
                await createSchemaCmd.ExecuteNonQueryAsync();
                Log.Information("? Schema '{Schema}' creado", schema);
            }
            else
            {
                Log.Information("? Schema '{Schema}' existe", schema);
            }
            
            // 2.2 Habilitar extensión pgcrypto
            try
            {
                var enablePgcryptoCmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS pgcrypto", conn);
                await enablePgcryptoCmd.ExecuteNonQueryAsync();
                Log.Information("? Extensión pgcrypto habilitada");
            }
            catch (Exception ex)
            {
                Log.Warning("??  pgcrypto: {Message}", ex.Message);
            }
            
            // 2.3 Listar schemas existentes
            var listSchemasCmd = new NpgsqlCommand(
                @"SELECT schema_name 
                  FROM information_schema.schemata 
                  WHERE schema_name NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
                  ORDER BY schema_name", 
                conn);
            
            var schemas = new List<string>();
            await using var reader = await listSchemasCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                schemas.Add(reader.GetString(0));
            }
            
            Log.Information("?? Schemas en pss_dvnx: {Schemas}", string.Join(", ", schemas));
        }
        
        Log.Information("? Verificación completada");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "? Error en verificación de BD/schema");
        throw;
    }
}

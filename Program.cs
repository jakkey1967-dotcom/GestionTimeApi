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

    // Seed con manejo robusto de errores
    try
    {
        Log.Information("🚀 Ejecutando seed de base de datos...");
        await GestionTime.Api.Startup.DbSeeder.SeedAsync(app.Services);
        Log.Information("✅ Seed completado exitosamente");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "❌ Error durante el seed");
        
        // No fallar el arranque si es error de BD ya inicializada
        var message = ex.Message.ToLowerInvariant();
        var isDbAlreadySetup = message.Contains("already exists") || 
                               message.Contains("42p07") ||
                               message.Contains("__efmigrationshistory");
        
        if (isDbAlreadySetup)
        {
            Log.Warning("⚠️ Base de datos ya inicializada. Continuando arranque...");
        }
        else
        {
            Log.Fatal(ex, "💥 Error crítico en seed - La aplicación no puede continuar");
            throw;
        }
    }

    // Health checks endpoint (público)
    app.MapHealthChecks("/health");

    // ✅ ENDPOINT RAÍZ PÚBLICO - Página simple de estado
    app.MapGet("/", () => 
    {
        var html = @"
<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>GestionTime API</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }
        .container {
            background: white;
            border-radius: 20px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            max-width: 600px;
            width: 100%;
            overflow: hidden;
        }
        .header {
            background: linear-gradient(135deg, #0B8C99 0%, #0A7A85 100%);
            color: white;
            padding: 40px;
            text-align: center;
        }
        .header h1 {
            font-size: 32px;
            font-weight: 600;
            margin-bottom: 10px;
        }
        .header p {
            opacity: 0.9;
            font-size: 16px;
        }
        .content {
            padding: 40px;
            text-align: center;
        }
        .status {
            display: inline-block;
            padding: 15px 30px;
            background: #d4edda;
            color: #155724;
            border-radius: 25px;
            font-size: 20px;
            font-weight: 600;
            margin-bottom: 30px;
        }
        .links {
            display: flex;
            gap: 15px;
            flex-wrap: wrap;
            justify-content: center;
        }
        .link-button {
            display: inline-block;
            padding: 12px 30px;
            background: linear-gradient(135deg, #0B8C99 0%, #0A7A85 100%);
            color: white;
            text-decoration: none;
            border-radius: 25px;
            font-weight: 600;
            transition: all 0.3s ease;
            box-shadow: 0 4px 15px rgba(11, 140, 153, 0.3);
        }
        .link-button:hover {
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(11, 140, 153, 0.4);
        }
        .link-button.secondary {
            background: linear-gradient(135deg, #6c757d 0%, #5a6268 100%);
            box-shadow: 0 4px 15px rgba(108, 117, 125, 0.3);
        }
        .footer {
            text-align: center;
            padding: 20px;
            background: #f8f9fa;
            color: #6c757d;
            font-size: 14px;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🚀 GestionTime API</h1>
            <p>Sistema de Gestión de Tiempo y Recursos</p>
        </div>
        <div class=""content"">
            <div class=""status"">✅ API Online</div>
            <p style=""color: #6c757d; margin-bottom: 30px;"">La API está funcionando correctamente</p>
            <div class=""links"">
                <a href=""/swagger"" class=""link-button"">📚 Documentación API</a>
                <a href=""/health"" class=""link-button secondary"">🏥 Health Check</a>
            </div>
        </div>
        <div class=""footer"">
            © 2025 GestionTime - Todos los derechos reservados<br>
            <small>Desarrollado por TDK Portal</small>
        </div>
    </div>
</body>
</html>";
        return Results.Content(html, "text/html");
    })
    .ExcludeFromDescription();

    // 🔒 ENDPOINT DE DIAGNÓSTICOS PROTEGIDO - Solo para administradores
    app.MapGet("/diagnostics", async (GestionTimeDbContext db) =>
    {
        var apiStatus = "✅ Online";
        var apiStatusClass = "status-ok";
        
        var dbStatus = "❌ Desconectado";
        var dbStatusClass = "status-error";
        var dbLatency = 0;
        var migrationsApplied = 0;
        var migrationsPending = 0;
        
        // ✅ Verificar Base de Datos CON latencia
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await db.Database.CanConnectAsync();
            sw.Stop();
            dbLatency = (int)sw.ElapsedMilliseconds;
            
            // Obtener información de migraciones
            var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            
            migrationsApplied = appliedMigrations.Count();
            migrationsPending = pendingMigrations.Count();
            
            dbStatus = "✅ Conectado";
            dbStatusClass = "status-ok";
        }
        catch (Exception ex)
        {
            sw.Stop();
            dbLatency = (int)sw.ElapsedMilliseconds;
            dbStatus = $"❌ Error: {ex.Message.Substring(0, Math.Min(50, ex.Message.Length))}...";
        }

        // ✅ Información del sistema
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var memoryUsedMB = (int)(process.WorkingSet64 / 1024 / 1024);
        var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
        var environment = app.Environment.EnvironmentName;
        var version = "1.0.0";
        
        // Información de Garbage Collector
        var gcGen0 = GC.CollectionCount(0);
        var gcGen1 = GC.CollectionCount(1);
        var gcGen2 = GC.CollectionCount(2);

        var html = $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>GestionTime API - Diagnósticos del Sistema</title>
    <style>
        * {{

            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
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
            background: linear-gradient(135deg, #dc3545 0%, #c82333 100%);
            color: white;
            padding: 40px;
            text-align: center;
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
        
        .warning-banner {{
            background: #fff3cd;
            color: #856404;
            padding: 15px;
            text-align: center;
            font-weight: 600;
            border-bottom: 2px solid #ffc107;
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
        
        .status-card.status-warning {{
            border-left-color: #ffc107;
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
        
        @media (max-width: 600px) {{
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
            <h1>🔒 Diagnósticos del Sistema</h1>
            <p>Información Confidencial - Solo Administradores</p>
        </div>
        
        <div class=""warning-banner"">
            ⚠️ ACCESO RESTRINGIDO: Esta página contiene información sensible del sistema
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
                    {(migrationsPending > 0 ? $@"<span class=""badge badge-warning"">⚠️ {migrationsPending} migración(es) pendiente(s)</span>" : $@"<span class=""badge badge-success"">✓ {migrationsApplied} migración(es) aplicada(s)</span>")}
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
                    <label>Memoria Usada</label>
                    <div class=""value"">{memoryUsedMB} MB</div>
                </div>
                
                <div class=""info-item"">
                    <label>GC Collections</label>
                    <div class=""value"">{gcGen0}/{gcGen1}/{gcGen2}</div>
                </div>
                
                <div class=""info-item"">
                    <label>Hora del Servidor</label>
                    <div class=""value"">{DateTime.UtcNow:HH:mm:ss} UTC</div>
                </div>
            </div>
        </div>
        
        <div class=""footer"">
            © 2025 GestionTime - Diagnósticos del Sistema<br>
            <small>Acceso: Solo Administradores • Auto-refresh: 30s</small>
        </div>
    </div>
    
    <script>
        // Auto-refresh cada 30 segundos
        setTimeout(() => location.reload(), 30000);
    </script>
</body>
</html>";

        return Results.Content(html, "text/html");
    })
    .RequireAuthorization(policy => policy.RequireRole("Admin"))
    .WithName("SystemDiagnostics")
    .ExcludeFromDescription();
    
    app.MapMethods("/", new[] { "HEAD" }, () => Results.Ok())
        .ExcludeFromDescription();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        // 🔒 En producción: Proteger Swagger con autenticación
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "GestionTime API v1");
            c.RoutePrefix = "swagger";
            c.DocumentTitle = "GestionTime API - Documentación";
        });
        
        // Middleware para proteger acceso a Swagger en producción
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                // Verificar si el usuario está autenticado
                if (!context.User.Identity?.IsAuthenticated ?? true)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Acceso denegado: Se requiere autenticación para acceder a la documentación de la API.");
                    return;
                }
                
                // Verificar si el usuario es Admin
                if (!context.User.IsInRole("Admin"))
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Acceso denegado: Solo los administradores pueden acceder a la documentación de la API.");
                    return;
                }
            }
            
            await next();
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


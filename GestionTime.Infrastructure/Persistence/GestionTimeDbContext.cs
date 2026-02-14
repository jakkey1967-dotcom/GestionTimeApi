using GestionTime.Domain.Auth;
using GestionTime.Domain.Work;
using GestionTime.Domain.Freshdesk;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GestionTime.Infrastructure.Persistence;

public sealed class GestionTimeDbContext : DbContext, IDataProtectionKeyContext
{
    private readonly string _schema;

    // Constructor para runtime (con DI completo)
    public GestionTimeDbContext(
        DbContextOptions<GestionTimeDbContext> options, 
        DatabaseSchemaConfig? schemaConfig = null) 
        : base(options)
    {
        _schema = schemaConfig?.Schema ?? "pss_dvnx";
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    // WORK
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Grupo> Grupos => Set<Grupo>();
    public DbSet<Tipo> Tipos => Set<Tipo>();
    public DbSet<ParteDeTrabajo> PartesDeTrabajo => Set<ParteDeTrabajo>();
    public DbSet<ParteTag> ParteTags => Set<ParteTag>();
    
    // ✅ Data Protection Keys
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    
    // ✅ Freshdesk
    public DbSet<FreshdeskAgentMap> FreshdeskAgentMaps => Set<FreshdeskAgentMap>();
    public DbSet<FreshdeskTag> FreshdeskTags => Set<FreshdeskTag>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Configuración básica
        base.OnConfiguring(optionsBuilder);
        
        // Configurar warnings para Entity Framework 8.0
        optionsBuilder.ConfigureWarnings(warnings =>
        {
            // Ignorar warnings comunes que no afectan funcionalidad
            warnings.Ignore(CoreEventId.NavigationBaseIncludeIgnored);
            warnings.Ignore(CoreEventId.NavigationBaseIncluded);
        });
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // PostgreSQL 9.4 => SERIAL (no IDENTITY)
        b.UseSerialColumns();

        // Configurar schema dinámico desde configuración
        b.HasDefaultSchema(_schema);

        // WORK: catálogos
        b.Entity<Cliente>(e =>
        {
            e.ToTable("cliente");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.IdPuntoop).HasColumnName("id_puntoop");
            e.Property(x => x.LocalNum).HasColumnName("local_num");
            e.Property(x => x.NombreComercial).HasColumnName("nombre_comercial");
            e.Property(x => x.Provincia).HasColumnName("provincia");
            e.Property(x => x.DataUpdate).HasColumnName("data_update").HasDefaultValueSql("now()");
            e.Property(x => x.DataHtml).HasColumnName("data_html");
            e.Property(x => x.Nota).HasColumnName("nota");
        });

        b.Entity<Grupo>(e =>
        {
            e.ToTable("grupo");
            e.HasKey(x => x.IdGrupo);

            e.Property(x => x.IdGrupo).HasColumnName("id_grupo").ValueGeneratedOnAdd();
            e.Property(x => x.Nombre).HasColumnName("nombre").IsRequired();
            e.Property(x => x.Descripcion).HasColumnName("descripcion");
        });

        b.Entity<Tipo>(e =>
        {
            e.ToTable("tipo");
            e.HasKey(x => x.IdTipo);

            e.Property(x => x.IdTipo).HasColumnName("id_tipo").ValueGeneratedOnAdd();
            e.Property(x => x.Nombre).HasColumnName("nombre").IsRequired();
            e.Property(x => x.Descripcion).HasColumnName("descripcion");
        });

        b.Entity<User>(e =>
        {
            e.ToTable("users");

            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();

            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();

            e.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            e.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(200);
            e.Property(x => x.Enabled).HasColumnName("enabled").HasDefaultValue(true);
            e.Property(x => x.EmailConfirmed).HasColumnName("email_confirmed").HasDefaultValue(false);
            
            // Control de expiración de contraseñas
            e.Property(x => x.PasswordChangedAt).HasColumnName("password_changed_at");
            e.Property(x => x.MustChangePassword).HasColumnName("must_change_password").HasDefaultValue(false);
            e.Property(x => x.PasswordExpirationDays).HasColumnName("password_expiration_days").HasDefaultValue(90);
            
            // Ignorar propiedades calculadas
            e.Ignore(x => x.IsPasswordExpired);
            e.Ignore(x => x.ShouldChangePassword);
            e.Ignore(x => x.DaysUntilPasswordExpires);
            
            // Relación 1:1 con UserProfile
            e.HasOne(x => x.Profile).WithOne(p => p.User)
             .HasForeignKey<UserProfile>(p => p.Id).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<UserProfile>(e =>
        {
            e.ToTable("user_profiles");

            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");

            // Datos personales
            e.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(100);
            e.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(100);
            e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20);
            e.Property(x => x.Mobile).HasColumnName("mobile").HasMaxLength(20);

            // Dirección
            e.Property(x => x.Address).HasColumnName("address").HasMaxLength(200);
            e.Property(x => x.City).HasColumnName("city").HasMaxLength(100);
            e.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(10);

            // Información laboral
            e.Property(x => x.Department).HasColumnName("department").HasMaxLength(100);
            e.Property(x => x.Position).HasColumnName("position").HasMaxLength(100);
            e.Property(x => x.EmployeeType).HasColumnName("employee_type").HasMaxLength(50);
            e.Property(x => x.HireDate).HasColumnName("hire_date").HasColumnType("date");

            // Otros
            e.Property(x => x.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(500);
            e.Property(x => x.Notes).HasColumnName("notes").HasColumnType("text");

            // Auditoría
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();

            // Ignorar propiedades calculadas
            e.Ignore(x => x.FullName);
            e.Ignore(x => x.IsTechnician);
        });

        b.Entity<Role>(e =>
        {
            e.ToTable("roles");

            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();

            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<UserRole>(e =>
        {
            e.ToTable("user_roles");

            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.RoleId).HasColumnName("role_id");

            e.HasKey(x => new { x.UserId, x.RoleId });

            e.HasOne(x => x.User).WithMany(u => u.UserRoles)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Role).WithMany(r => r.UserRoles)
             .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");

            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();

            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.TokenHash).IsUnique();

            e.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
            e.Property(x => x.RevokedAt).HasColumnName("revoked_at");

            e.HasOne(x => x.User).WithMany(u => u.RefreshTokens)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ParteDeTrabajo>(e =>
        {
            e.ToTable("partesdetrabajo");

            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");

            // Fecha y horas
            e.Property(x => x.FechaTrabajo).HasColumnName("fecha_trabajo").HasColumnType("date");
            e.Property(x => x.HoraInicio).HasColumnName("hora_inicio").HasColumnType("time");
            e.Property(x => x.HoraFin).HasColumnName("hora_fin").HasColumnType("time");

            // Trabajo
            e.Property(x => x.Accion).HasColumnName("accion");
            e.Property(x => x.Ticket).HasColumnName("ticket");
            e.Property(x => x.Tienda).HasColumnName("tienda");

            // IDs
            e.Property(x => x.IdCliente).HasColumnName("id_cliente");
            e.Property(x => x.IdGrupo).HasColumnName("id_grupo");
            e.Property(x => x.IdTipo).HasColumnName("id_tipo");
            e.Property(x => x.IdUsuario).HasColumnName("id_usuario");

            // ✅ Estado (TEXT en BD, conversión a INT en modelo)
            // La columna en BD se llama "estado" (no "state") y es de tipo TEXT
            e.Property(x => x.Estado)
                .HasColumnName("estado")
                .HasColumnType("text")
                .HasDefaultValueSql("'activo'")  // ✅ Usar SQL para el valor por defecto
                .HasConversion(
                    // int → text para guardar en BD
                    v => ConvertirEstadoIntATexto(v),
                    // text → int para leer de BD
                    v => ConvertirEstadoTextoAInt(v)
                );

            // Auditoría
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

            // Ignorar propiedades calculadas
            e.Ignore(x => x.EstaAbierto);
            e.Ignore(x => x.EstaPausado);
            e.Ignore(x => x.EstaCerrado);
            e.Ignore(x => x.EstaEnviado);
            e.Ignore(x => x.EstaAnulado);
            e.Ignore(x => x.PuedeEditar);
            e.Ignore(x => x.EstadoNombre);

            // Índices
            e.HasIndex(x => x.FechaTrabajo).HasDatabaseName("idx_partes_fecha_trabajo");
            e.HasIndex(x => new { x.IdUsuario, x.FechaTrabajo }).HasDatabaseName("idx_partes_user_fecha");
            e.HasIndex(x => x.CreatedAt).HasDatabaseName("idx_partes_created_at");
            
            // Check constraint para horas válidas
            e.ToTable(t => t.HasCheckConstraint("ck_partes_horas_validas", "hora_fin >= hora_inicio"));
        });
        
        // ✅ Configurar ParteTag (tabla puente N:N - usa freshdesk_tags)
        b.Entity<ParteTag>(e =>
        {
            e.ToTable("parte_tags");
            
            // Clave compuesta
            e.HasKey(x => new { x.ParteId, x.TagName });
            
            e.Property(x => x.ParteId)
                .HasColumnName("parte_id");
            
            e.Property(x => x.TagName)
                .HasColumnName("tag_name")
                .HasMaxLength(100) // ← Mismo límite que freshdesk_tags
                .IsRequired();
            
            // Relación con ParteDeTrabajo
            e.HasOne(x => x.Parte)
                .WithMany(p => p.ParteTags)
                .HasForeignKey(x => x.ParteId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Relación con freshdesk_tags
            e.HasOne<FreshdeskTag>()
                .WithMany()
                .HasForeignKey(x => x.TagName)
                .HasPrincipalKey(t => t.Name)
                .OnDelete(DeleteBehavior.Restrict); // No borrar tag si está en uso
            
            // Índices
            e.HasIndex(x => x.ParteId)
                .HasDatabaseName("idx_parte_tags_parte_id");
            
            e.HasIndex(x => x.TagName)
                .HasDatabaseName("idx_parte_tags_tag_name");
        });
        
        // ✅ Configurar DataProtectionKeys con schema dinámico
        b.Entity<DataProtectionKey>(e =>
        {
            e.ToTable("dataprotectionkeys"); // ✅ Forzar minúsculas
            e.HasKey(x => x.Id);

            
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.FriendlyName).HasColumnName("friendlyname");
            e.Property(x => x.Xml).HasColumnName("xml");
        });

        // ✅ UserSession (para presencia online)
        b.Entity<UserSession>(e =>
        {
            e.ToTable("user_sessions");
            e.HasKey(x => x.Id);
            
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Property(x => x.LastSeenAt).HasColumnName("last_seen_at").IsRequired();
            e.Property(x => x.RevokedAt).HasColumnName("revoked_at");
            e.Property(x => x.DeviceId).HasColumnName("device_id").HasMaxLength(100);
            e.Property(x => x.DeviceName).HasColumnName("device_name").HasMaxLength(200);
            e.Property(x => x.Ip).HasColumnName("ip").HasMaxLength(45);
            e.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
            
            // Índices para performance
            e.HasIndex(x => x.UserId).HasDatabaseName("idx_sessions_user_id");
            e.HasIndex(x => x.LastSeenAt).HasDatabaseName("idx_sessions_last_seen");
            e.HasIndex(x => new { x.UserId, x.RevokedAt }).HasDatabaseName("idx_sessions_user_active");
            
            // Relación con User
            e.HasOne(x => x.User).WithMany(u => u.Sessions)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
             
            // Ignorar propiedades calculadas
            e.Ignore(x => x.IsActive);
        });
        
        // ✅ Freshdesk Agent Map (caché de agentId por usuario)
        b.Entity<FreshdeskAgentMap>(e =>
        {
            e.ToTable("freshdesk_agent_map");
            e.HasKey(x => x.UserId);
            
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
            e.Property(x => x.AgentId).HasColumnName("agent_id").IsRequired();
            e.Property(x => x.SyncedAt).HasColumnName("synced_at").IsRequired();
            
            e.HasIndex(x => x.Email).HasDatabaseName("idx_freshdesk_agent_email");
        });
        
        // ✅ Freshdesk Tags (caché de tags sincronizados)
        b.Entity<FreshdeskTag>(e =>
        {
            e.ToTable("freshdesk_tags");
            e.HasKey(x => x.Name);

            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
            e.Property(x => x.Source).HasColumnName("source").HasMaxLength(50).IsRequired();
            e.Property(x => x.LastSeenAt).HasColumnName("last_seen_at").IsRequired();

            e.HasIndex(x => x.LastSeenAt).HasDatabaseName("idx_freshdesk_tags_last_seen");
        });

        // ✅ Vista v_partes_stats_full (solo lectura para informes v2)
        b.Entity<GestionTime.Domain.Reports.VPartesStatsFull>(e =>
        {
            e.HasNoKey();
            e.ToView("v_partes_stats_full", _schema);

            e.Property(x => x.FechaTrabajo).HasColumnName("fecha_trabajo");
            e.Property(x => x.HoraInicio).HasColumnName("hora_inicio");
            e.Property(x => x.HoraFin).HasColumnName("hora_fin");
            e.Property(x => x.DuracionHoras).HasColumnName("duracion_horas");
            e.Property(x => x.DuracionMin).HasColumnName("duracion_min");
            e.Property(x => x.Accion).HasColumnName("accion");
            e.Property(x => x.Ticket).HasColumnName("ticket");
            e.Property(x => x.IdCliente).HasColumnName("id_cliente");
            e.Property(x => x.Tienda).HasColumnName("tienda");
            e.Property(x => x.IdGrupo).HasColumnName("id_grupo");
            e.Property(x => x.IdTipo).HasColumnName("id_tipo");
            e.Property(x => x.IdUsuario).HasColumnName("id_usuario");
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.Tags).HasColumnName("tags");
            e.Property(x => x.FechaDia).HasColumnName("fecha_dia");
            e.Property(x => x.SemanaIso).HasColumnName("semana_iso");
            e.Property(x => x.Mes).HasColumnName("mes");
            e.Property(x => x.Anio).HasColumnName("anio");
            e.Property(x => x.AgenteNombre).HasColumnName("agente_nombre");
            e.Property(x => x.AgenteEmail).HasColumnName("agente_email");
            e.Property(x => x.ClienteNombre).HasColumnName("cliente_nombre");
            e.Property(x => x.GrupoNombre).HasColumnName("grupo_nombre");
            e.Property(x => x.TipoNombre).HasColumnName("tipo_nombre");
            e.Property(x => x.DuracionHorasTs).HasColumnName("duracion_horas_ts");
            e.Property(x => x.DuracionMinTs).HasColumnName("duracion_min_ts");
        });
    }

    /// <summary>
    /// Convierte código de estado (int) a texto para la base de datos
    /// </summary>
    private static string ConvertirEstadoIntATexto(int estado)
    {
        return estado switch
        {
            0 => "activo",
            1 => "pausado",
            2 => "cerrado",
            3 => "enviado",
            9 => "anulado",
            _ => "activo"
        };
    }

    /// <summary>
    /// Convierte valores de estado en texto a enteros
    /// Maneja tanto valores numéricos como descriptivos
    /// </summary>
    private static int ConvertirEstadoTextoAInt(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
            return 0;

        // Si es un número válido, usarlo directamente
        if (int.TryParse(valor, out int numero))
            return numero;

        // Si es texto descriptivo, mapear a números
        return valor.ToLowerInvariant().Trim() switch
        {
            "activo" => 0,
            "abierto" => 0,
            "pausado" => 1,
            "cerrado" => 2,
            "enviado" => 3,
            "anulado" => 9,
            _ => 0
        };
    }
}


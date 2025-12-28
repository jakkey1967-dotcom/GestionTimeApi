using GestionTime.Domain.Auth;
using GestionTime.Domain.Work;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GestionTime.Infrastructure.Persistence;

public sealed class GestionTimeDbContext : DbContext
{
    public GestionTimeDbContext(DbContextOptions<GestionTimeDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    // WORK
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Grupo> Grupos => Set<Grupo>();
    public DbSet<Tipo> Tipos => Set<Tipo>();
    public DbSet<ParteDeTrabajo> PartesDeTrabajo => Set<ParteDeTrabajo>();

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

        // Configurar schema 'gestiontime' para organizar las tablas
        b.HasDefaultSchema("gestiontime");

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
            e.Property(x => x.Accion).HasColumnName("accion").HasMaxLength(500);
            e.Property(x => x.Ticket).HasColumnName("ticket").HasMaxLength(50);
            e.Property(x => x.Tienda).HasColumnName("tienda").HasMaxLength(100);

            // IDs
            e.Property(x => x.IdCliente).HasColumnName("id_cliente");
            e.Property(x => x.IdGrupo).HasColumnName("id_grupo");
            e.Property(x => x.IdTipo).HasColumnName("id_tipo");
            e.Property(x => x.IdUsuario).HasColumnName("id_usuario");

            // ✅ MAPEO CORRECTO: Estado → estado (no state)
            // ⚠️ TEMPORAL: Conversión int ↔ text para compatibilidad con BD
            e.Property(x => x.Estado)
                .HasColumnName("estado")
                .HasColumnType("text")
                .HasConversion(
                    v => v.ToString(), // int → text para BD
                    v => ConvertirEstadoTextoAInt(v) // text → int para modelo (maneja 'activo', 'cerrado', etc.)
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
        });
    }

    /// <summary>
    /// Convierte valores de estado en texto a enteros
    /// Maneja tanto valores numéricos como descriptivos
    /// </summary>
    private static int ConvertirEstadoTextoAInt(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
            return 0; // Abierto por defecto

        // Si es un número válido, usarlo directamente
        if (int.TryParse(valor, out int numero))
            return numero;

        // Si es texto descriptivo, mapear a números
        return valor.ToLowerInvariant().Trim() switch
        {
            "abierto" => 0,
            "activo" => 0,      // 'activo' = Abierto
            "pausado" => 1,
            "cerrado" => 2,
            "enviado" => 3,
            "anulado" => 9,
            _ => 0 // Valor desconocido = Abierto por defecto
        };
    }
}


using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GestionTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Columna 'nota' puede existir de migración manual anterior
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'pss_dvnx' AND table_name = 'cliente' AND column_name = 'nota'
                    ) THEN
                        ALTER TABLE pss_dvnx.cliente ADD COLUMN nota text;
                    END IF;
                END $$;");

            migrationBuilder.CreateTable(
                name: "app_settings",
                schema: "pss_dvnx",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_settings", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "client_versions",
                schema: "pss_dvnx",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Desktop"),
                    app_version_raw = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ver_major = table.Column<int>(type: "integer", nullable: false),
                    ver_minor = table.Column<int>(type: "integer", nullable: false),
                    ver_patch = table.Column<int>(type: "integer", nullable: false),
                    ver_prerelease = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    os_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    machine_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    logged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_client_versions_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "pss_dvnx",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Tabla 'cliente_notas' puede existir de migración manual anterior
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS pss_dvnx.cliente_notas (
                    id uuid NOT NULL DEFAULT gen_random_uuid(),
                    cliente_id integer NOT NULL,
                    owner_user_id uuid,
                    nota text NOT NULL,
                    created_at timestamp with time zone NOT NULL DEFAULT now(),
                    updated_at timestamp with time zone NOT NULL DEFAULT now(),
                    created_by uuid,
                    updated_by uuid,
                    CONSTRAINT ""PK_cliente_notas"" PRIMARY KEY (id),
                    CONSTRAINT ""FK_cliente_notas_cliente_cliente_id"" FOREIGN KEY (cliente_id)
                        REFERENCES pss_dvnx.cliente(id) ON DELETE CASCADE
                );");

            migrationBuilder.CreateIndex(
                name: "idx_client_versions_user_logged",
                schema: "pss_dvnx",
                table: "client_versions",
                columns: new[] { "user_id", "logged_at" });

            migrationBuilder.CreateIndex(
                name: "idx_client_versions_ver",
                schema: "pss_dvnx",
                table: "client_versions",
                columns: new[] { "ver_major", "ver_minor", "ver_patch" });

            migrationBuilder.InsertData(
                schema: "pss_dvnx",
                table: "app_settings",
                columns: new[] { "key", "value", "updated_at" },
                values: new object[,]
                {
                    { "min_client_version_desktop", "2.0.0", new DateTimeOffset(2026, 2, 28, 0, 0, 0, TimeSpan.Zero) },
                    { "latest_client_version_desktop", "1.9.5-beta", new DateTimeOffset(2026, 2, 28, 0, 0, 0, TimeSpan.Zero) },
                    { "update_url_desktop", "https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/latest", new DateTimeOffset(2026, 2, 28, 0, 0, 0, TimeSpan.Zero) }
                });

            // Índices de cliente_notas pueden existir de migración manual anterior
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS idx_cliente_notas_cliente_id
                    ON pss_dvnx.cliente_notas (cliente_id) WHERE owner_user_id IS NULL;
                CREATE UNIQUE INDEX IF NOT EXISTS uq_cliente_notas_personal
                    ON pss_dvnx.cliente_notas (cliente_id, owner_user_id) WHERE owner_user_id IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "pss_dvnx",
                table: "app_settings",
                keyColumn: "key",
                keyValues: new object[] { "min_client_version_desktop", "latest_client_version_desktop", "update_url_desktop" });

            migrationBuilder.DropTable(
                name: "app_settings",
                schema: "pss_dvnx");

            migrationBuilder.DropTable(
                name: "client_versions",
                schema: "pss_dvnx");

            migrationBuilder.Sql(@"DROP TABLE IF EXISTS pss_dvnx.cliente_notas;");

            migrationBuilder.Sql(@"
                ALTER TABLE pss_dvnx.cliente DROP COLUMN IF EXISTS nota;");
        }
    }
}

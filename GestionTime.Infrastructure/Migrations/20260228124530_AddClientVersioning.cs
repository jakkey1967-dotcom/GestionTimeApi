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
            migrationBuilder.AddColumn<string>(
                name: "nota",
                schema: "pss_dvnx",
                table: "cliente",
                type: "text",
                nullable: true);

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

            migrationBuilder.CreateTable(
                name: "cliente_notas",
                schema: "pss_dvnx",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    cliente_id = table.Column<int>(type: "integer", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nota = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cliente_notas", x => x.id);
                    table.ForeignKey(
                        name: "FK_cliente_notas_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "pss_dvnx",
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "idx_cliente_notas_cliente_id",
                schema: "pss_dvnx",
                table: "cliente_notas",
                column: "cliente_id",
                unique: true,
                filter: "owner_user_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_cliente_notas_personal",
                schema: "pss_dvnx",
                table: "cliente_notas",
                columns: new[] { "cliente_id", "owner_user_id" },
                unique: true,
                filter: "owner_user_id IS NOT NULL");
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

            migrationBuilder.DropTable(
                name: "cliente_notas",
                schema: "pss_dvnx");

            migrationBuilder.DropColumn(
                name: "nota",
                schema: "pss_dvnx",
                table: "cliente");
        }
    }
}

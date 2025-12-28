using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GestionTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WorkInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "partesdetrabajo",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    fecha_trabajo = table.Column<DateTime>(type: "date", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    hora_fin = table.Column<TimeOnly>(type: "time", nullable: false),
                    accion = table.Column<string>(type: "text", nullable: false),
                    ticket = table.Column<string>(type: "text", nullable: true),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    tienda = table.Column<string>(type: "text", nullable: true),
                    id_grupo = table.Column<int>(type: "integer", nullable: true),
                    id_tipo = table.Column<int>(type: "integer", nullable: true),
                    id_usuario = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false, defaultValue: "activo"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partesdetrabajo", x => x.id);
                    table.CheckConstraint("ck_partes_horas_validas", "hora_fin >= hora_inicio");
                });

            migrationBuilder.CreateIndex(
                name: "idx_partes_created_at",
                table: "partesdetrabajo",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_partes_fecha_trabajo",
                table: "partesdetrabajo",
                column: "fecha_trabajo");

            migrationBuilder.CreateIndex(
                name: "idx_partes_user_fecha",
                table: "partesdetrabajo",
                columns: new[] { "id_usuario", "fecha_trabajo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "partesdetrabajo");
        }
    }
}

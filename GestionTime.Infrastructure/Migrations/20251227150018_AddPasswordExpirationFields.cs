using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordExpirationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "gestiontime");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "users",
                newSchema: "gestiontime");

            migrationBuilder.RenameTable(
                name: "user_roles",
                newName: "user_roles",
                newSchema: "gestiontime");

            migrationBuilder.RenameTable(
                name: "tipo",
                newName: "tipo",
                newSchema: "gestiontime");

            migrationBuilder.RenameTable(
                name: "roles",
                newName: "roles",
                newSchema: "gestiontime");

            migrationBuilder.RenameTable(
                name: "refresh_tokens",
                newName: "refresh_tokens",
                newSchema: "gestiontime");

            migrationBuilder.RenameTable(
                name: "partesdetrabajo",
                newName: "partesdetrabajo",
                newSchema: "gestiontime");

            migrationBuilder.RenameTable(
                name: "grupo",
                newName: "grupo",
                newSchema: "gestiontime");

            migrationBuilder.RenameTable(
                name: "cliente",
                newName: "cliente",
                newSchema: "gestiontime");

            migrationBuilder.RenameColumn(
                name: "estado",
                schema: "gestiontime",
                table: "partesdetrabajo",
                newName: "state");

            migrationBuilder.AddColumn<bool>(
                name: "email_confirmed",
                schema: "gestiontime",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "must_change_password",
                schema: "gestiontime",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "password_changed_at",
                schema: "gestiontime",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "password_expiration_days",
                schema: "gestiontime",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 90);

            migrationBuilder.AlterColumn<int>(
                name: "state",
                schema: "gestiontime",
                table: "partesdetrabajo",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "activo");

            migrationBuilder.CreateTable(
                name: "user_profiles",
                schema: "gestiontime",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    employee_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    hire_date = table.Column<DateTime>(type: "date", nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_profiles_users_id",
                        column: x => x.id,
                        principalSchema: "gestiontime",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_profiles",
                schema: "gestiontime");

            migrationBuilder.DropColumn(
                name: "email_confirmed",
                schema: "gestiontime",
                table: "users");

            migrationBuilder.DropColumn(
                name: "must_change_password",
                schema: "gestiontime",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_changed_at",
                schema: "gestiontime",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_expiration_days",
                schema: "gestiontime",
                table: "users");

            migrationBuilder.RenameTable(
                name: "users",
                schema: "gestiontime",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "user_roles",
                schema: "gestiontime",
                newName: "user_roles");

            migrationBuilder.RenameTable(
                name: "tipo",
                schema: "gestiontime",
                newName: "tipo");

            migrationBuilder.RenameTable(
                name: "roles",
                schema: "gestiontime",
                newName: "roles");

            migrationBuilder.RenameTable(
                name: "refresh_tokens",
                schema: "gestiontime",
                newName: "refresh_tokens");

            migrationBuilder.RenameTable(
                name: "partesdetrabajo",
                schema: "gestiontime",
                newName: "partesdetrabajo");

            migrationBuilder.RenameTable(
                name: "grupo",
                schema: "gestiontime",
                newName: "grupo");

            migrationBuilder.RenameTable(
                name: "cliente",
                schema: "gestiontime",
                newName: "cliente");

            migrationBuilder.RenameColumn(
                name: "state",
                table: "partesdetrabajo",
                newName: "estado");

            migrationBuilder.AlterColumn<string>(
                name: "estado",
                table: "partesdetrabajo",
                type: "text",
                nullable: false,
                defaultValue: "activo",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);
        }
    }
}

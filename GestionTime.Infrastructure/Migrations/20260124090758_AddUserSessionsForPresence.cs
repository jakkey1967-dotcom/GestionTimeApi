using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSessionsForPresence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_sessions",
                schema: "pss_dvnx",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    device_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    device_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_sessions_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "pss_dvnx",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_sessions_last_seen",
                schema: "pss_dvnx",
                table: "user_sessions",
                column: "last_seen_at");

            migrationBuilder.CreateIndex(
                name: "idx_sessions_user_active",
                schema: "pss_dvnx",
                table: "user_sessions",
                columns: new[] { "user_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "idx_sessions_user_id",
                schema: "pss_dvnx",
                table: "user_sessions",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_sessions",
                schema: "pss_dvnx");
        }
    }
}

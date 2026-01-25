using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFreshdeskTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "freshdesk_agent_map",
                schema: "pss_dvnx",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    agent_id = table.Column<long>(type: "bigint", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_freshdesk_agent_map", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "freshdesk_tags",
                schema: "pss_dvnx",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_freshdesk_tags", x => x.name);
                });

            migrationBuilder.CreateIndex(
                name: "idx_freshdesk_agent_email",
                schema: "pss_dvnx",
                table: "freshdesk_agent_map",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "idx_freshdesk_tags_last_seen",
                schema: "pss_dvnx",
                table: "freshdesk_tags",
                column: "last_seen_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "freshdesk_agent_map",
                schema: "pss_dvnx");

            migrationBuilder.DropTable(
                name: "freshdesk_tags",
                schema: "pss_dvnx");
        }
    }
}

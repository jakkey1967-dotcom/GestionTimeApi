using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartesTagsWithFreshdeskTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "parte_tags",
                schema: "pss_dvnx",
                columns: table => new
                {
                    parte_id = table.Column<long>(type: "bigint", nullable: false),
                    tag_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parte_tags", x => new { x.parte_id, x.tag_name });
                    table.ForeignKey(
                        name: "FK_parte_tags_freshdesk_tags_tag_name",
                        column: x => x.tag_name,
                        principalSchema: "pss_dvnx",
                        principalTable: "freshdesk_tags",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_parte_tags_partesdetrabajo_parte_id",
                        column: x => x.parte_id,
                        principalSchema: "pss_dvnx",
                        principalTable: "partesdetrabajo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_parte_tags_parte_id",
                schema: "pss_dvnx",
                table: "parte_tags",
                column: "parte_id");

            migrationBuilder.CreateIndex(
                name: "idx_parte_tags_tag_name",
                schema: "pss_dvnx",
                table: "parte_tags",
                column: "tag_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parte_tags",
                schema: "pss_dvnx");
        }
    }
}

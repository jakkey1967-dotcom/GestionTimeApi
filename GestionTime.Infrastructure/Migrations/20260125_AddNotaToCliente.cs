using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotaToCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE pss_dvnx.cliente 
                ADD COLUMN IF NOT EXISTS nota TEXT;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE pss_dvnx.cliente 
                DROP COLUMN IF EXISTS nota;
            ");
        }
    }
}

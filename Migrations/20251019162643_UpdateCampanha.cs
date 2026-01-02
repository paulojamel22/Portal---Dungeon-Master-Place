using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalDMPlace.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCampanha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NomeSimples",
                table: "Campanhas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NomeSimples",
                table: "Campanhas");
        }
    }
}

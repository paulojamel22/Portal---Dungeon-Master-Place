using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalDMPlace.Migrations
{
    /// <inheritdoc />
    public partial class CampanhaTableUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FoundryUrl",
                table: "Settings",
                newName: "VttUrl");

            migrationBuilder.AddColumn<int>(
                name: "CriadorId",
                table: "Campanhas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CriadorId",
                table: "Campanhas");

            migrationBuilder.RenameColumn(
                name: "VttUrl",
                table: "Settings",
                newName: "FoundryUrl");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalDMPlace.Migrations
{
    /// <inheritdoc />
    public partial class SettingsTableUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerUrl",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CardThumbnailUrl",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChamadaCard",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FonteFamilia",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TemaCorSecundaria",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "CardThumbnailUrl",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "ChamadaCard",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "FonteFamilia",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "TemaCorSecundaria",
                table: "Settings");
        }
    }
}

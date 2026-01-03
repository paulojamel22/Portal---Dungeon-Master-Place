using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalDMPlace.Migrations
{
    /// <inheritdoc />
    public partial class FullDataRemake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Campanha",
                table: "Noticias",
                newName: "CampanhaId");

            migrationBuilder.RenameColumn(
                name: "Campanha",
                table: "Accounts",
                newName: "CampanhaId");

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CampanhaId = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscordWebhookUrl = table.Column<string>(type: "TEXT", nullable: false),
                    TemaCorPrimaria = table.Column<string>(type: "TEXT", nullable: false),
                    ExibirRelogioSessao = table.Column<bool>(type: "INTEGER", nullable: false),
                    FoundryUrl = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settings_Campanhas_CampanhaId",
                        column: x => x.CampanhaId,
                        principalTable: "Campanhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Noticias_CampanhaId",
                table: "Noticias",
                column: "CampanhaId");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_CampanhaId",
                table: "Settings",
                column: "CampanhaId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Noticias_Campanhas_CampanhaId",
                table: "Noticias",
                column: "CampanhaId",
                principalTable: "Campanhas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Noticias_Campanhas_CampanhaId",
                table: "Noticias");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropIndex(
                name: "IX_Noticias_CampanhaId",
                table: "Noticias");

            migrationBuilder.RenameColumn(
                name: "CampanhaId",
                table: "Noticias",
                newName: "Campanha");

            migrationBuilder.RenameColumn(
                name: "CampanhaId",
                table: "Accounts",
                newName: "Campanha");
        }
    }
}

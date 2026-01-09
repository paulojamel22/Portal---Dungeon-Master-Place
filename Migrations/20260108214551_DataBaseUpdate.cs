using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalDMPlace.Migrations
{
    /// <inheritdoc />
    public partial class DataBaseUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlobalSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ManutencaoAtiva = table.Column<bool>(type: "INTEGER", nullable: false),
                    MensagemManutencao = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Noticias_AccountId",
                table: "Noticias",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Campanhas_CriadorId",
                table: "Campanhas",
                column: "CriadorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Campanhas_Accounts_CriadorId",
                table: "Campanhas",
                column: "CriadorId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Noticias_Accounts_AccountId",
                table: "Noticias",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campanhas_Accounts_CriadorId",
                table: "Campanhas");

            migrationBuilder.DropForeignKey(
                name: "FK_Noticias_Accounts_AccountId",
                table: "Noticias");

            migrationBuilder.DropTable(
                name: "GlobalSettings");

            migrationBuilder.DropIndex(
                name: "IX_Noticias_AccountId",
                table: "Noticias");

            migrationBuilder.DropIndex(
                name: "IX_Campanhas_CriadorId",
                table: "Campanhas");
        }
    }
}

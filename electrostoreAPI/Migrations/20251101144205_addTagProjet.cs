using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostore.Migrations
{
    /// <inheritdoc />
    public partial class addTagProjet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsProjetTags_ProjetTags_id_tag",
                table: "ProjetsProjetTags");

            migrationBuilder.DropIndex(
                name: "IX_ProjetsProjetTags_id_tag",
                table: "ProjetsProjetTags");

            migrationBuilder.DropColumn(
                name: "id_tag",
                table: "ProjetsProjetTags");

            migrationBuilder.CreateIndex(
                name: "IX_ProjetsProjetTags_id_projet_tag",
                table: "ProjetsProjetTags",
                column: "id_projet_tag");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsProjetTags_ProjetTags_id_projet_tag",
                table: "ProjetsProjetTags",
                column: "id_projet_tag",
                principalTable: "ProjetTags",
                principalColumn: "id_projet_tag",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsProjetTags_ProjetTags_id_projet_tag",
                table: "ProjetsProjetTags");

            migrationBuilder.DropIndex(
                name: "IX_ProjetsProjetTags_id_projet_tag",
                table: "ProjetsProjetTags");

            migrationBuilder.AddColumn<int>(
                name: "id_tag",
                table: "ProjetsProjetTags",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProjetsProjetTags_id_tag",
                table: "ProjetsProjetTags",
                column: "id_tag");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsProjetTags_ProjetTags_id_tag",
                table: "ProjetsProjetTags",
                column: "id_tag",
                principalTable: "ProjetTags",
                principalColumn: "id_projet_tag",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

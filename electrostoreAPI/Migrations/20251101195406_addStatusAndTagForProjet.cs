using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostore.Migrations
{
    /// <inheritdoc />
    public partial class addStatusAndTagForProjet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "date_fin_projet",
                table: "Projets");

            migrationBuilder.CreateTable(
                name: "ProjetsStatus",
                columns: table => new
                {
                    id_projet_status = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_projet = table.Column<int>(type: "int", nullable: false),
                    status_projet = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjetsStatus", x => x.id_projet_status);
                    table.ForeignKey(
                        name: "FK_ProjetsStatus_Projets_id_projet",
                        column: x => x.id_projet,
                        principalTable: "Projets",
                        principalColumn: "id_projet",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProjetTags",
                columns: table => new
                {
                    id_projet_tag = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nom_projet_tag = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    poids_projet_tag = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjetTags", x => x.id_projet_tag);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProjetsProjetTags",
                columns: table => new
                {
                    id_projet_tag = table.Column<int>(type: "int", nullable: false),
                    id_projet = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjetsProjetTags", x => new { x.id_projet, x.id_projet_tag });
                    table.ForeignKey(
                        name: "FK_ProjetsProjetTags_ProjetTags_id_projet_tag",
                        column: x => x.id_projet_tag,
                        principalTable: "ProjetTags",
                        principalColumn: "id_projet_tag",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjetsProjetTags_Projets_id_projet",
                        column: x => x.id_projet,
                        principalTable: "Projets",
                        principalColumn: "id_projet",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ProjetsProjetTags_id_projet_tag",
                table: "ProjetsProjetTags",
                column: "id_projet_tag");

            migrationBuilder.CreateIndex(
                name: "IX_ProjetsStatus_id_projet",
                table: "ProjetsStatus",
                column: "id_projet");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjetsProjetTags");

            migrationBuilder.DropTable(
                name: "ProjetsStatus");

            migrationBuilder.DropTable(
                name: "ProjetTags");

            migrationBuilder.AddColumn<DateTime>(
                name: "date_fin_projet",
                table: "Projets",
                type: "datetime(6)",
                nullable: true);
        }
    }
}

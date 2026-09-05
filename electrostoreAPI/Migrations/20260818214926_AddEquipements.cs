using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Equipements",
                columns: table => new
                {
                    id_equipement = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    reference_name_equipement = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    friendly_name_equipement = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description_equipement = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status_equipement = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipements", x => x.id_equipement);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EquipementsBoxs",
                columns: table => new
                {
                    id_box = table.Column<int>(type: "int", nullable: false),
                    id_equipement = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipementsBoxs", x => new { x.id_equipement, x.id_box });
                    table.ForeignKey(
                        name: "FK_EquipementsBoxs_Boxs_id_box",
                        column: x => x.id_box,
                        principalTable: "Boxs",
                        principalColumn: "id_box",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EquipementsBoxs_Equipements_id_equipement",
                        column: x => x.id_equipement,
                        principalTable: "Equipements",
                        principalColumn: "id_equipement",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EquipementsComments",
                columns: table => new
                {
                    id_equipement_comment = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_user = table.Column<int>(type: "int", nullable: true),
                    id_equipement = table.Column<int>(type: "int", nullable: false),
                    content_equipement_comment = table.Column<string>(type: "varchar(455)", maxLength: 455, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipementsComments", x => x.id_equipement_comment);
                    table.ForeignKey(
                        name: "FK_EquipementsComments_Equipements_id_equipement",
                        column: x => x.id_equipement,
                        principalTable: "Equipements",
                        principalColumn: "id_equipement",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EquipementsComments_Users_id_user",
                        column: x => x.id_user,
                        principalTable: "Users",
                        principalColumn: "id_user");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EquipementsDocuments",
                columns: table => new
                {
                    id_equipement_document = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    url_equipement_document = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_equipement_document = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type_equipement_document = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    size_equipement_document = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    id_equipement = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipementsDocuments", x => x.id_equipement_document);
                    table.ForeignKey(
                        name: "FK_EquipementsDocuments_Equipements_id_equipement",
                        column: x => x.id_equipement,
                        principalTable: "Equipements",
                        principalColumn: "id_equipement",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EquipementsMaintenances",
                columns: table => new
                {
                    id_equipement_maintenance = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_equipement = table.Column<int>(type: "int", nullable: false),
                    id_user = table.Column<int>(type: "int", nullable: true),
                    type_equipement_maintenance = table.Column<int>(type: "int", nullable: false),
                    date_planned_equipement_maintenance = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_done_equipement_maintenance = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    description_equipement_maintenance = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipementsMaintenances", x => x.id_equipement_maintenance);
                    table.ForeignKey(
                        name: "FK_EquipementsMaintenances_Equipements_id_equipement",
                        column: x => x.id_equipement,
                        principalTable: "Equipements",
                        principalColumn: "id_equipement",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EquipementsMaintenances_Users_id_user",
                        column: x => x.id_user,
                        principalTable: "Users",
                        principalColumn: "id_user");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EquipementsStatus",
                columns: table => new
                {
                    id_equipement_status = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_equipement = table.Column<int>(type: "int", nullable: false),
                    status_equipement = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipementsStatus", x => x.id_equipement_status);
                    table.ForeignKey(
                        name: "FK_EquipementsStatus_Equipements_id_equipement",
                        column: x => x.id_equipement,
                        principalTable: "Equipements",
                        principalColumn: "id_equipement",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EquipementsTags",
                columns: table => new
                {
                    id_tag = table.Column<int>(type: "int", nullable: false),
                    id_equipement = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipementsTags", x => new { x.id_equipement, x.id_tag });
                    table.ForeignKey(
                        name: "FK_EquipementsTags_Equipements_id_equipement",
                        column: x => x.id_equipement,
                        principalTable: "Equipements",
                        principalColumn: "id_equipement",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EquipementsTags_Tags_id_tag",
                        column: x => x.id_tag,
                        principalTable: "Tags",
                        principalColumn: "id_tag",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EquipementsBoxs_id_box",
                table: "EquipementsBoxs",
                column: "id_box");

            migrationBuilder.CreateIndex(
                name: "IX_EquipementsBoxs_id_equipement",
                table: "EquipementsBoxs",
                column: "id_equipement",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipementsComments_id_equipement",
                table: "EquipementsComments",
                column: "id_equipement");

            migrationBuilder.CreateIndex(
                name: "IX_EquipementsComments_id_user",
                table: "EquipementsComments",
                column: "id_user");

            migrationBuilder.CreateIndex(
                name: "IX_EquipementsDocuments_id_equipement",
                table: "EquipementsDocuments",
                column: "id_equipement");

            migrationBuilder.CreateIndex(
                name: "IX_EquipementsMaintenances_id_equipement",
                table: "EquipementsMaintenances",
                column: "id_equipement");

            migrationBuilder.CreateIndex(
                name: "IX_EquipementsMaintenances_id_user",
                table: "EquipementsMaintenances",
                column: "id_user");

            migrationBuilder.CreateIndex(
                name: "IX_EquipementsStatus_id_equipement",
                table: "EquipementsStatus",
                column: "id_equipement");

            migrationBuilder.CreateIndex(
                name: "IX_EquipementsTags_id_tag",
                table: "EquipementsTags",
                column: "id_tag");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipementsBoxs");

            migrationBuilder.DropTable(
                name: "EquipementsComments");

            migrationBuilder.DropTable(
                name: "EquipementsDocuments");

            migrationBuilder.DropTable(
                name: "EquipementsMaintenances");

            migrationBuilder.DropTable(
                name: "EquipementsStatus");

            migrationBuilder.DropTable(
                name: "EquipementsTags");

            migrationBuilder.DropTable(
                name: "Equipements");
        }
    }
}

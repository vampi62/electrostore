using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddZoneAndStorePlanPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_zone",
                table: "Stores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "xmax_store",
                table: "Stores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "xmin_store",
                table: "Stores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ymax_store",
                table: "Stores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ymin_store",
                table: "Stores",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Zones",
                columns: table => new
                {
                    id_zone = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name_zone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description_zone = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    xlength_zone = table.Column<int>(type: "int", nullable: false),
                    ylength_zone = table.Column<int>(type: "int", nullable: false),
                    url_picture_zone = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    url_thumbnail_zone = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zones", x => x.id_zone);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_id_zone",
                table: "Stores",
                column: "id_zone");

            migrationBuilder.AddForeignKey(
                name: "FK_Stores_Zones_id_zone",
                table: "Stores",
                column: "id_zone",
                principalTable: "Zones",
                principalColumn: "id_zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stores_Zones_id_zone",
                table: "Stores");

            migrationBuilder.DropTable(
                name: "Zones");

            migrationBuilder.DropIndex(
                name: "IX_Stores_id_zone",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "id_zone",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "xmax_store",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "xmin_store",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "ymax_store",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "ymin_store",
                table: "Stores");
        }
    }
}

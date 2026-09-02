using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveImgsTableAndAddItemImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Imgs_id_img",
                table: "Items");

            migrationBuilder.DropTable(
                name: "AI");

            migrationBuilder.DropTable(
                name: "Cameras");

            migrationBuilder.DropTable(
                name: "Imgs");

            migrationBuilder.DropIndex(
                name: "IX_Items_id_img",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "id_img",
                table: "Items");

            migrationBuilder.AddColumn<string>(
                name: "url_picture_item",
                table: "Items",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "url_thumbnail_item",
                table: "Items",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "url_picture_item",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "url_thumbnail_item",
                table: "Items");

            migrationBuilder.AddColumn<int>(
                name: "id_img",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AI",
                columns: table => new
                {
                    id_ia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_training_ia = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    description_ia = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_ia = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    trained_ia = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI", x => x.id_ia);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Cameras",
                columns: table => new
                {
                    id_camera = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_seen_camera = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    name_camera = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password_camera = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    url_camera = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_camera = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cameras", x => x.id_camera);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Imgs",
                columns: table => new
                {
                    id_img = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_item = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    description_img = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_img = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    url_picture_img = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    url_thumbnail_img = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Imgs", x => x.id_img);
                    table.ForeignKey(
                        name: "FK_Imgs_Items_id_item",
                        column: x => x.id_item,
                        principalTable: "Items",
                        principalColumn: "id_item",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Items_id_img",
                table: "Items",
                column: "id_img");

            migrationBuilder.CreateIndex(
                name: "IX_Imgs_id_item",
                table: "Imgs",
                column: "id_item");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Imgs_id_img",
                table: "Items",
                column: "id_img",
                principalTable: "Imgs",
                principalColumn: "id_img");
        }
    }
}

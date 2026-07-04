using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddItemsHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemsHistory",
                columns: table => new
                {
                    id_item_history = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_item = table.Column<int>(type: "int", nullable: true),
                    id_box = table.Column<int>(type: "int", nullable: true),
                    id_user = table.Column<int>(type: "int", nullable: true),
                    type = table.Column<int>(type: "int", nullable: false),
                    quantity_change = table.Column<int>(type: "int", nullable: true),
                    old_quantity = table.Column<int>(type: "int", nullable: true),
                    new_quantity = table.Column<int>(type: "int", nullable: true),
                    notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemsHistory", x => x.id_item_history);
                    table.ForeignKey(
                        name: "FK_ItemsHistory_Boxs_id_box",
                        column: x => x.id_box,
                        principalTable: "Boxs",
                        principalColumn: "id_box",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ItemsHistory_Items_id_item",
                        column: x => x.id_item,
                        principalTable: "Items",
                        principalColumn: "id_item",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ItemsHistory_Users_id_user",
                        column: x => x.id_user,
                        principalTable: "Users",
                        principalColumn: "id_user",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsHistory_id_box",
                table: "ItemsHistory",
                column: "id_box");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsHistory_id_item",
                table: "ItemsHistory",
                column: "id_item");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsHistory_id_user",
                table: "ItemsHistory",
                column: "id_user");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemsHistory");
        }
    }
}

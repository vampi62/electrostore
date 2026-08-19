using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddItemVendorsAndPriceHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemsVendors",
                columns: table => new
                {
                    id_item_vendor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_item = table.Column<int>(type: "int", nullable: false),
                    vendor_type_item_vendor = table.Column<int>(type: "int", nullable: false),
                    vendor_sku_item_vendor = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    url_item_vendor = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemsVendors", x => x.id_item_vendor);
                    table.ForeignKey(
                        name: "FK_ItemsVendors_Items_id_item",
                        column: x => x.id_item,
                        principalTable: "Items",
                        principalColumn: "id_item",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ItemVendorPrices",
                columns: table => new
                {
                    id_item_vendor_price = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_item_vendor = table.Column<int>(type: "int", nullable: false),
                    price_item_vendor_price = table.Column<float>(type: "float", nullable: false),
                    currency_item_vendor_price = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    quantity_item_vendor_price = table.Column<int>(type: "int", nullable: false),
                    price_breaks_item_vendor_price = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemVendorPrices", x => x.id_item_vendor_price);
                    table.ForeignKey(
                        name: "FK_ItemVendorPrices_ItemsVendors_id_item_vendor",
                        column: x => x.id_item_vendor,
                        principalTable: "ItemsVendors",
                        principalColumn: "id_item_vendor",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsVendors_id_item",
                table: "ItemsVendors",
                column: "id_item");

            migrationBuilder.CreateIndex(
                name: "IX_ItemVendorPrices_id_item_vendor",
                table: "ItemVendorPrices",
                column: "id_item_vendor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemVendorPrices");

            migrationBuilder.DropTable(
                name: "ItemsVendors");
        }
    }
}

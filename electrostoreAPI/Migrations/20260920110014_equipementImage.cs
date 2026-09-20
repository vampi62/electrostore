using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class equipementImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "url_picture_equipement",
                table: "Equipements",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "url_thumbnail_equipement",
                table: "Equipements",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "url_picture_equipement",
                table: "Equipements");

            migrationBuilder.DropColumn(
                name: "url_thumbnail_equipement",
                table: "Equipements");
        }
    }
}

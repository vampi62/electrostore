using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddStorePositionMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "position_mode_store",
                table: "Stores",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "position_mode_store",
                table: "Stores");
        }
    }
}

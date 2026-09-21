using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class optionalCarrier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Commands_Carriers_id_carrier",
                table: "Commands");

            migrationBuilder.AlterColumn<int>(
                name: "id_carrier",
                table: "Commands",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Commands_Carriers_id_carrier",
                table: "Commands",
                column: "id_carrier",
                principalTable: "Carriers",
                principalColumn: "id_carrier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Commands_Carriers_id_carrier",
                table: "Commands");

            migrationBuilder.AlterColumn<int>(
                name: "id_carrier",
                table: "Commands",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Commands_Carriers_id_carrier",
                table: "Commands",
                column: "id_carrier",
                principalTable: "Carriers",
                principalColumn: "id_carrier",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

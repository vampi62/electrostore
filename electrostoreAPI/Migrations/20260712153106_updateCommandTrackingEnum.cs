using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class updateCommandTrackingEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "sub_status",
                table: "CommandsHistory",
                type: "int",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150,
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "last_sub_status",
                table: "Commands",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_sub_status",
                table: "Commands");

            migrationBuilder.AlterColumn<string>(
                name: "sub_status",
                table: "CommandsHistory",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 150,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}

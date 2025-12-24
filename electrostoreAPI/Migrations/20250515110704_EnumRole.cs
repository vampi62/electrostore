using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostore.Migrations
{
    /// <inheritdoc />
    public partial class EnumRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "nom_item",
                table: "Items",
                newName: "reference_name_item");

            migrationBuilder.RenameColumn(
                name: "url_img",
                table: "Imgs",
                newName: "url_picture_img");

            

            migrationBuilder.Sql(@"UPDATE Users SET role_user = 
                CASE 
                    WHEN role_user = 'admin' THEN '3'
                    ELSE '1'
                END");

            migrationBuilder.AlterColumn<int>(
                name: "role_user",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "type_projet_document",
                table: "ProjetsDocuments",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "type_item_document",
                table: "ItemsDocuments",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "friendly_name_item",
                table: "Items",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "url_thumbnail_img",
                table: "Imgs",
                type: "varchar(165)",
                maxLength: 165,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "type_command_document",
                table: "CommandsDocuments",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "friendly_name_item",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "url_thumbnail_img",
                table: "Imgs");

            migrationBuilder.RenameColumn(
                name: "reference_name_item",
                table: "Items",
                newName: "nom_item");

            migrationBuilder.RenameColumn(
                name: "url_picture_img",
                table: "Imgs",
                newName: "url_img");

            migrationBuilder.AddColumn<string>(
                name: "role_user_Temp",
                table: "Users",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(@"UPDATE Users SET role_user_Temp = 
                CASE 
                    WHEN role_user = 3 THEN 'admin'
                    ELSE 'user'
                END");

            migrationBuilder.DropColumn(
                name: "role_user",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "role_user_Temp",
                table: "Users",
                newName: "role_user");

            migrationBuilder.AlterColumn<string>(
                name: "type_projet_document",
                table: "ProjetsDocuments",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "type_item_document",
                table: "ItemsDocuments",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "type_command_document",
                table: "CommandsDocuments",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}

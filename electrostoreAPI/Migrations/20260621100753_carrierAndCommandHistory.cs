using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class carrierAndCommandHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "event_at",
                table: "CommandsHistory");

            migrationBuilder.DropColumn(
                name: "status_command_history",
                table: "CommandsHistory");

            migrationBuilder.RenameColumn(
                name: "tracking_number",
                table: "CommandsHistory",
                newName: "timezone");

            migrationBuilder.RenameColumn(
                name: "tracking_event",
                table: "CommandsHistory",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "carrier",
                table: "CommandsHistory",
                newName: "longitude");

            migrationBuilder.AlterColumn<string>(
                name: "url_projet_document",
                table: "ProjetsDocuments",
                type: "varchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(2048)",
                oldMaxLength: 2048)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "url_item_document",
                table: "ItemsDocuments",
                type: "varchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(2048)",
                oldMaxLength: 2048)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "url_thumbnail_img",
                table: "Imgs",
                type: "varchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(2063)",
                oldMaxLength: 2063)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "url_picture_img",
                table: "Imgs",
                type: "varchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(2048)",
                oldMaxLength: 2048)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "action_cronjob",
                table: "CronJobs",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "CommandsHistory",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "country",
                table: "CommandsHistory",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "event_time_utc",
                table: "CommandsHistory",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "latitude",
                table: "CommandsHistory",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "location",
                table: "CommandsHistory",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                table: "CommandsHistory",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "stage",
                table: "CommandsHistory",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "state",
                table: "CommandsHistory",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "CommandsHistory",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sub_status",
                table: "CommandsHistory",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "url_command_document",
                table: "CommandsDocuments",
                type: "varchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(2048)",
                oldMaxLength: 2048)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "status_command",
                table: "Commands",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "id_carrier",
                table: "Commands",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "Commands",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_tracking_requested",
                table: "Commands",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_tracking_validated",
                table: "Commands",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "last_status",
                table: "Commands",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "raw_data",
                table: "Commands",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "recipient_adress",
                table: "Commands",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "shipper_adress",
                table: "Commands",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "tracking_number",
                table: "Commands",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_seen_camera",
                table: "Cameras",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Carriers",
                columns: table => new
                {
                    id_carrier = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    key = table.Column<int>(type: "int", nullable: false),
                    country = table.Column<int>(type: "int", nullable: true),
                    country_iso = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tel = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    url = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carriers", x => x.id_carrier);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Commands_id_carrier",
                table: "Commands",
                column: "id_carrier");

            migrationBuilder.AddForeignKey(
                name: "FK_Commands_Carriers_id_carrier",
                table: "Commands",
                column: "id_carrier",
                principalTable: "Carriers",
                principalColumn: "id_carrier",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Commands_Carriers_id_carrier",
                table: "Commands");

            migrationBuilder.DropTable(
                name: "Carriers");

            migrationBuilder.DropIndex(
                name: "IX_Commands_id_carrier",
                table: "Commands");

            migrationBuilder.DropColumn(
                name: "city",
                table: "CommandsHistory");

            migrationBuilder.DropColumn(
                name: "country",
                table: "CommandsHistory");

            migrationBuilder.DropColumn(
                name: "event_time_utc",
                table: "CommandsHistory");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "CommandsHistory");

            migrationBuilder.DropColumn(
                name: "location",
                table: "CommandsHistory");

            migrationBuilder.DropColumn(
                name: "postal_code",
                table: "CommandsHistory");

            migrationBuilder.DropColumn(
                name: "stage",
                table: "CommandsHistory");

            migrationBuilder.DropColumn(
                name: "state",
                table: "CommandsHistory");

            migrationBuilder.DropColumn(
                name: "status",
                table: "CommandsHistory");

            migrationBuilder.DropColumn(
                name: "sub_status",
                table: "CommandsHistory");

            migrationBuilder.DropColumn(
                name: "id_carrier",
                table: "Commands");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "Commands");

            migrationBuilder.DropColumn(
                name: "is_tracking_requested",
                table: "Commands");

            migrationBuilder.DropColumn(
                name: "is_tracking_validated",
                table: "Commands");

            migrationBuilder.DropColumn(
                name: "last_status",
                table: "Commands");

            migrationBuilder.DropColumn(
                name: "raw_data",
                table: "Commands");

            migrationBuilder.DropColumn(
                name: "recipient_adress",
                table: "Commands");

            migrationBuilder.DropColumn(
                name: "shipper_adress",
                table: "Commands");

            migrationBuilder.DropColumn(
                name: "tracking_number",
                table: "Commands");

            migrationBuilder.DropColumn(
                name: "last_seen_camera",
                table: "Cameras");

            migrationBuilder.RenameColumn(
                name: "timezone",
                table: "CommandsHistory",
                newName: "tracking_number");

            migrationBuilder.RenameColumn(
                name: "longitude",
                table: "CommandsHistory",
                newName: "carrier");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "CommandsHistory",
                newName: "tracking_event");

            migrationBuilder.AlterColumn<string>(
                name: "url_projet_document",
                table: "ProjetsDocuments",
                type: "varchar(2048)",
                maxLength: 2048,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(300)",
                oldMaxLength: 300)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "url_item_document",
                table: "ItemsDocuments",
                type: "varchar(2048)",
                maxLength: 2048,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(300)",
                oldMaxLength: 300)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "url_thumbnail_img",
                table: "Imgs",
                type: "varchar(2063)",
                maxLength: 2063,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(300)",
                oldMaxLength: 300)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "url_picture_img",
                table: "Imgs",
                type: "varchar(2048)",
                maxLength: 2048,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(300)",
                oldMaxLength: 300)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "action_cronjob",
                table: "CronJobs",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "event_at",
                table: "CommandsHistory",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "status_command_history",
                table: "CommandsHistory",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "url_command_document",
                table: "CommandsDocuments",
                type: "varchar(2048)",
                maxLength: 2048,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(300)",
                oldMaxLength: 300)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "status_command",
                table: "Commands",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}

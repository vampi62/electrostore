using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class storeMqttPasswordAES : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "mqtt_password_encryption_iv_store",
                table: "Stores",
                type: "varbinary(16)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "mqtt_password_encryption_tag_store",
                table: "Stores",
                type: "varbinary(16)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "mqtt_password_store",
                table: "Stores",
                type: "varbinary(512)",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mqtt_password_encryption_iv_store",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "mqtt_password_encryption_tag_store",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "mqtt_password_store",
                table: "Stores");
        }
    }
}

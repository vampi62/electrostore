using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class RenamePkFkIdentityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id_push_subscription",
                table: "UserPushSubscriptions",
                newName: "id_user_push_subscription");

            migrationBuilder.RenameColumn(
                name: "mqtt_led_id",
                table: "Leds",
                newName: "mqtt_id_led");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id_user_push_subscription",
                table: "UserPushSubscriptions",
                newName: "id_push_subscription");

            migrationBuilder.RenameColumn(
                name: "mqtt_id_led",
                table: "Leds",
                newName: "mqtt_led_id");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class ApplyScalarSuffixAndPkFkNamingConvention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "session_id",
                table: "JwiRefreshTokens",
                newName: "session_id_jwi_refresh");

            migrationBuilder.RenameColumn(
                name: "revoked_reason",
                table: "JwiRefreshTokens",
                newName: "revoked_reason_jwi_refresh");

            migrationBuilder.RenameColumn(
                name: "revoked_by_ip",
                table: "JwiRefreshTokens",
                newName: "revoked_by_ip_jwi_refresh");

            migrationBuilder.RenameColumn(
                name: "created_by_ip",
                table: "JwiRefreshTokens",
                newName: "created_by_ip_jwi_refresh");

            migrationBuilder.RenameColumn(
                name: "auth_method",
                table: "JwiRefreshTokens",
                newName: "auth_method_jwi_refresh");

            migrationBuilder.RenameColumn(
                name: "session_id",
                table: "JwiAccessTokens",
                newName: "session_id_jwi_access");

            migrationBuilder.RenameColumn(
                name: "revoked_reason",
                table: "JwiAccessTokens",
                newName: "revoked_reason_jwi_access");

            migrationBuilder.RenameColumn(
                name: "revoked_by_ip",
                table: "JwiAccessTokens",
                newName: "revoked_by_ip_jwi_access");

            migrationBuilder.RenameColumn(
                name: "created_by_ip",
                table: "JwiAccessTokens",
                newName: "created_by_ip_jwi_access");

            migrationBuilder.RenameColumn(
                name: "auth_method",
                table: "JwiAccessTokens",
                newName: "auth_method_jwi_access");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "ItemsHistory",
                newName: "type_item_history");

            migrationBuilder.RenameColumn(
                name: "quantity_change",
                table: "ItemsHistory",
                newName: "quantity_change_item_history");

            migrationBuilder.RenameColumn(
                name: "old_quantity",
                table: "ItemsHistory",
                newName: "old_quantity_item_history");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "ItemsHistory",
                newName: "notes_item_history");

            migrationBuilder.RenameColumn(
                name: "new_quantity",
                table: "ItemsHistory",
                newName: "new_quantity_item_history");

            migrationBuilder.RenameColumn(
                name: "cron_expression",
                table: "CronJobs",
                newName: "cron_expression_cronjob");

            migrationBuilder.RenameColumn(
                name: "timezone",
                table: "CommandsHistory",
                newName: "timezone_command_history");

            migrationBuilder.RenameColumn(
                name: "sub_status",
                table: "CommandsHistory",
                newName: "sub_status_command_history");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "CommandsHistory",
                newName: "status_command_history");

            migrationBuilder.RenameColumn(
                name: "state",
                table: "CommandsHistory",
                newName: "state_command_history");

            migrationBuilder.RenameColumn(
                name: "stage",
                table: "CommandsHistory",
                newName: "stage_command_history");

            migrationBuilder.RenameColumn(
                name: "postal_code",
                table: "CommandsHistory",
                newName: "postal_code_command_history");

            migrationBuilder.RenameColumn(
                name: "longitude",
                table: "CommandsHistory",
                newName: "longitude_command_history");

            migrationBuilder.RenameColumn(
                name: "location",
                table: "CommandsHistory",
                newName: "location_command_history");

            migrationBuilder.RenameColumn(
                name: "latitude",
                table: "CommandsHistory",
                newName: "latitude_command_history");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "CommandsHistory",
                newName: "description_command_history");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "CommandsHistory",
                newName: "country_command_history");

            migrationBuilder.RenameColumn(
                name: "city",
                table: "CommandsHistory",
                newName: "city_command_history");

            migrationBuilder.RenameColumn(
                name: "tracking_number",
                table: "Commands",
                newName: "tracking_number_command");

            migrationBuilder.RenameColumn(
                name: "shipper_address",
                table: "Commands",
                newName: "shipper_address_command");

            migrationBuilder.RenameColumn(
                name: "recipient_address",
                table: "Commands",
                newName: "recipient_address_command");

            migrationBuilder.RenameColumn(
                name: "raw_data",
                table: "Commands",
                newName: "raw_data_command");

            migrationBuilder.RenameColumn(
                name: "last_sub_status",
                table: "Commands",
                newName: "last_sub_status_command");

            migrationBuilder.RenameColumn(
                name: "last_status",
                table: "Commands",
                newName: "last_status_command");

            migrationBuilder.RenameColumn(
                name: "url",
                table: "Carriers",
                newName: "url_carrier");

            migrationBuilder.RenameColumn(
                name: "tel",
                table: "Carriers",
                newName: "tel_carrier");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Carriers",
                newName: "name_carrier");

            migrationBuilder.RenameColumn(
                name: "key",
                table: "Carriers",
                newName: "key_carrier");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Carriers",
                newName: "email_carrier");

            migrationBuilder.RenameColumn(
                name: "country_iso",
                table: "Carriers",
                newName: "country_iso_carrier");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "Carriers",
                newName: "country_carrier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "session_id_jwi_refresh",
                table: "JwiRefreshTokens",
                newName: "session_id");

            migrationBuilder.RenameColumn(
                name: "revoked_reason_jwi_refresh",
                table: "JwiRefreshTokens",
                newName: "revoked_reason");

            migrationBuilder.RenameColumn(
                name: "revoked_by_ip_jwi_refresh",
                table: "JwiRefreshTokens",
                newName: "revoked_by_ip");

            migrationBuilder.RenameColumn(
                name: "created_by_ip_jwi_refresh",
                table: "JwiRefreshTokens",
                newName: "created_by_ip");

            migrationBuilder.RenameColumn(
                name: "auth_method_jwi_refresh",
                table: "JwiRefreshTokens",
                newName: "auth_method");

            migrationBuilder.RenameColumn(
                name: "session_id_jwi_access",
                table: "JwiAccessTokens",
                newName: "session_id");

            migrationBuilder.RenameColumn(
                name: "revoked_reason_jwi_access",
                table: "JwiAccessTokens",
                newName: "revoked_reason");

            migrationBuilder.RenameColumn(
                name: "revoked_by_ip_jwi_access",
                table: "JwiAccessTokens",
                newName: "revoked_by_ip");

            migrationBuilder.RenameColumn(
                name: "created_by_ip_jwi_access",
                table: "JwiAccessTokens",
                newName: "created_by_ip");

            migrationBuilder.RenameColumn(
                name: "auth_method_jwi_access",
                table: "JwiAccessTokens",
                newName: "auth_method");

            migrationBuilder.RenameColumn(
                name: "type_item_history",
                table: "ItemsHistory",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "quantity_change_item_history",
                table: "ItemsHistory",
                newName: "quantity_change");

            migrationBuilder.RenameColumn(
                name: "old_quantity_item_history",
                table: "ItemsHistory",
                newName: "old_quantity");

            migrationBuilder.RenameColumn(
                name: "notes_item_history",
                table: "ItemsHistory",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "new_quantity_item_history",
                table: "ItemsHistory",
                newName: "new_quantity");

            migrationBuilder.RenameColumn(
                name: "cron_expression_cronjob",
                table: "CronJobs",
                newName: "cron_expression");

            migrationBuilder.RenameColumn(
                name: "timezone_command_history",
                table: "CommandsHistory",
                newName: "timezone");

            migrationBuilder.RenameColumn(
                name: "sub_status_command_history",
                table: "CommandsHistory",
                newName: "sub_status");

            migrationBuilder.RenameColumn(
                name: "status_command_history",
                table: "CommandsHistory",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "state_command_history",
                table: "CommandsHistory",
                newName: "state");

            migrationBuilder.RenameColumn(
                name: "stage_command_history",
                table: "CommandsHistory",
                newName: "stage");

            migrationBuilder.RenameColumn(
                name: "postal_code_command_history",
                table: "CommandsHistory",
                newName: "postal_code");

            migrationBuilder.RenameColumn(
                name: "longitude_command_history",
                table: "CommandsHistory",
                newName: "longitude");

            migrationBuilder.RenameColumn(
                name: "location_command_history",
                table: "CommandsHistory",
                newName: "location");

            migrationBuilder.RenameColumn(
                name: "latitude_command_history",
                table: "CommandsHistory",
                newName: "latitude");

            migrationBuilder.RenameColumn(
                name: "description_command_history",
                table: "CommandsHistory",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "country_command_history",
                table: "CommandsHistory",
                newName: "country");

            migrationBuilder.RenameColumn(
                name: "city_command_history",
                table: "CommandsHistory",
                newName: "city");

            migrationBuilder.RenameColumn(
                name: "tracking_number_command",
                table: "Commands",
                newName: "tracking_number");

            migrationBuilder.RenameColumn(
                name: "shipper_address_command",
                table: "Commands",
                newName: "shipper_address");

            migrationBuilder.RenameColumn(
                name: "recipient_address_command",
                table: "Commands",
                newName: "recipient_address");

            migrationBuilder.RenameColumn(
                name: "raw_data_command",
                table: "Commands",
                newName: "raw_data");

            migrationBuilder.RenameColumn(
                name: "last_sub_status_command",
                table: "Commands",
                newName: "last_sub_status");

            migrationBuilder.RenameColumn(
                name: "last_status_command",
                table: "Commands",
                newName: "last_status");

            migrationBuilder.RenameColumn(
                name: "url_carrier",
                table: "Carriers",
                newName: "url");

            migrationBuilder.RenameColumn(
                name: "tel_carrier",
                table: "Carriers",
                newName: "tel");

            migrationBuilder.RenameColumn(
                name: "name_carrier",
                table: "Carriers",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "key_carrier",
                table: "Carriers",
                newName: "key");

            migrationBuilder.RenameColumn(
                name: "email_carrier",
                table: "Carriers",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "country_iso_carrier",
                table: "Carriers",
                newName: "country_iso");

            migrationBuilder.RenameColumn(
                name: "country_carrier",
                table: "Carriers",
                newName: "country");
        }
    }
}

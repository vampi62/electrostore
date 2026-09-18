using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class cronjob_error : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EquipementsBoxs_id_equipement",
                table: "EquipementsBoxs");

            migrationBuilder.AddColumn<string>(
                name: "last_error_cronjob",
                table: "CronJobs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "status_cronjob",
                table: "CronJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_error_cronjob",
                table: "CronJobs");

            migrationBuilder.DropColumn(
                name: "status_cronjob",
                table: "CronJobs");

            migrationBuilder.CreateIndex(
                name: "IX_EquipementsBoxs_id_equipement",
                table: "EquipementsBoxs",
                column: "id_equipement",
                unique: true);
        }
    }
}

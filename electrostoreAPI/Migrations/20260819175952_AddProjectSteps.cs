using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectsSteps",
                columns: table => new
                {
                    id_project_step = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_project = table.Column<int>(type: "int", nullable: false),
                    name_project_step = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description_project_step = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status_project_step = table.Column<int>(type: "int", nullable: false),
                    order_project_step = table.Column<int>(type: "int", nullable: false),
                    planned_start_project_step = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    planned_end_project_step = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    actual_start_project_step = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    actual_end_project_step = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectsSteps", x => x.id_project_step);
                    table.ForeignKey(
                        name: "FK_ProjectsSteps_Projects_id_project",
                        column: x => x.id_project,
                        principalTable: "Projects",
                        principalColumn: "id_project",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectsSteps_id_project",
                table: "ProjectsSteps",
                column: "id_project");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectsSteps");
        }
    }
}

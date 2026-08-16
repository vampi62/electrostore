using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class RenameFrenchClassesToEnglish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "CommandsCommentaires",
                newName: "CommandsComments");

            migrationBuilder.RenameTable(
                name: "IA",
                newName: "AI");

            migrationBuilder.RenameTable(
                name: "Projets",
                newName: "Projects");

            migrationBuilder.RenameTable(
                name: "ProjetsCommentaires",
                newName: "ProjectsComments");

            migrationBuilder.RenameTable(
                name: "ProjetsDocuments",
                newName: "ProjectsDocuments");

            migrationBuilder.RenameTable(
                name: "ProjetsItems",
                newName: "ProjectsItems");

            migrationBuilder.RenameTable(
                name: "ProjetsProjetTags",
                newName: "ProjectsProjectTags");

            migrationBuilder.RenameTable(
                name: "ProjetsStatus",
                newName: "ProjectsStatus");

            migrationBuilder.RenameTable(
                name: "ProjetTags",
                newName: "ProjectTags");

            migrationBuilder.RenameIndex(
                name: "IX_CommandsCommentaires_id_command",
                table: "CommandsComments",
                newName: "IX_CommandsComments_id_command");

            migrationBuilder.RenameIndex(
                name: "IX_CommandsCommentaires_id_user",
                table: "CommandsComments",
                newName: "IX_CommandsComments_id_user");

            migrationBuilder.RenameIndex(
                name: "IX_ProjetsCommentaires_id_project",
                table: "ProjectsComments",
                newName: "IX_ProjectsComments_id_project");

            migrationBuilder.RenameIndex(
                name: "IX_ProjetsCommentaires_id_user",
                table: "ProjectsComments",
                newName: "IX_ProjectsComments_id_user");

            migrationBuilder.RenameIndex(
                name: "IX_ProjetsDocuments_id_project",
                table: "ProjectsDocuments",
                newName: "IX_ProjectsDocuments_id_project");

            migrationBuilder.RenameIndex(
                name: "IX_ProjetsItems_id_item",
                table: "ProjectsItems",
                newName: "IX_ProjectsItems_id_item");

            migrationBuilder.RenameIndex(
                name: "IX_ProjetsProjetTags_id_project_tag",
                table: "ProjectsProjectTags",
                newName: "IX_ProjectsProjectTags_id_project_tag");

            migrationBuilder.RenameIndex(
                name: "IX_ProjetsStatus_id_project",
                table: "ProjectsStatus",
                newName: "IX_ProjectsStatus_id_project");

            migrationBuilder.DropForeignKey(
                name: "FK_CommandsCommentaires_Commands_id_command",
                table: "CommandsComments");

            migrationBuilder.DropForeignKey(
                name: "FK_CommandsCommentaires_Users_id_user",
                table: "CommandsComments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsCommentaires_Projets_id_project",
                table: "ProjectsComments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsCommentaires_Users_id_user",
                table: "ProjectsComments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsDocuments_Projets_id_project",
                table: "ProjectsDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsItems_Items_id_item",
                table: "ProjectsItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsItems_Projets_id_project",
                table: "ProjectsItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsStatus_Projets_id_project",
                table: "ProjectsStatus");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsProjetTags_ProjetTags_id_project_tag",
                table: "ProjectsProjectTags");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsProjetTags_Projets_id_project",
                table: "ProjectsProjectTags");

            migrationBuilder.AddForeignKey(
                name: "FK_CommandsComments_Commands_id_command",
                table: "CommandsComments",
                column: "id_command",
                principalTable: "Commands",
                principalColumn: "id_command",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CommandsComments_Users_id_user",
                table: "CommandsComments",
                column: "id_user",
                principalTable: "Users",
                principalColumn: "id_user");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectsComments_Projects_id_project",
                table: "ProjectsComments",
                column: "id_project",
                principalTable: "Projects",
                principalColumn: "id_project",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectsComments_Users_id_user",
                table: "ProjectsComments",
                column: "id_user",
                principalTable: "Users",
                principalColumn: "id_user");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectsDocuments_Projects_id_project",
                table: "ProjectsDocuments",
                column: "id_project",
                principalTable: "Projects",
                principalColumn: "id_project",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectsItems_Items_id_item",
                table: "ProjectsItems",
                column: "id_item",
                principalTable: "Items",
                principalColumn: "id_item",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectsItems_Projects_id_project",
                table: "ProjectsItems",
                column: "id_project",
                principalTable: "Projects",
                principalColumn: "id_project",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectsStatus_Projects_id_project",
                table: "ProjectsStatus",
                column: "id_project",
                principalTable: "Projects",
                principalColumn: "id_project",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectsProjectTags_ProjectTags_id_project_tag",
                table: "ProjectsProjectTags",
                column: "id_project_tag",
                principalTable: "ProjectTags",
                principalColumn: "id_project_tag",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectsProjectTags_Projects_id_project",
                table: "ProjectsProjectTags",
                column: "id_project",
                principalTable: "Projects",
                principalColumn: "id_project",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommandsComments_Commands_id_command",
                table: "CommandsComments");

            migrationBuilder.DropForeignKey(
                name: "FK_CommandsComments_Users_id_user",
                table: "CommandsComments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectsComments_Projects_id_project",
                table: "ProjectsComments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectsComments_Users_id_user",
                table: "ProjectsComments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectsDocuments_Projects_id_project",
                table: "ProjectsDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectsItems_Items_id_item",
                table: "ProjectsItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectsItems_Projects_id_project",
                table: "ProjectsItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectsStatus_Projects_id_project",
                table: "ProjectsStatus");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectsProjectTags_ProjectTags_id_project_tag",
                table: "ProjectsProjectTags");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectsProjectTags_Projects_id_project",
                table: "ProjectsProjectTags");

            migrationBuilder.AddForeignKey(
                name: "FK_CommandsCommentaires_Commands_id_command",
                table: "CommandsComments",
                column: "id_command",
                principalTable: "Commands",
                principalColumn: "id_command",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CommandsCommentaires_Users_id_user",
                table: "CommandsComments",
                column: "id_user",
                principalTable: "Users",
                principalColumn: "id_user");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsCommentaires_Projets_id_project",
                table: "ProjectsComments",
                column: "id_project",
                principalTable: "Projects",
                principalColumn: "id_project",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsCommentaires_Users_id_user",
                table: "ProjectsComments",
                column: "id_user",
                principalTable: "Users",
                principalColumn: "id_user");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsDocuments_Projets_id_project",
                table: "ProjectsDocuments",
                column: "id_project",
                principalTable: "Projects",
                principalColumn: "id_project",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsItems_Items_id_item",
                table: "ProjectsItems",
                column: "id_item",
                principalTable: "Items",
                principalColumn: "id_item",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsItems_Projets_id_project",
                table: "ProjectsItems",
                column: "id_project",
                principalTable: "Projects",
                principalColumn: "id_project",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsStatus_Projets_id_project",
                table: "ProjectsStatus",
                column: "id_project",
                principalTable: "Projects",
                principalColumn: "id_project",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsProjetTags_ProjetTags_id_project_tag",
                table: "ProjectsProjectTags",
                column: "id_project_tag",
                principalTable: "ProjectTags",
                principalColumn: "id_project_tag",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsProjetTags_Projets_id_project",
                table: "ProjectsProjectTags",
                column: "id_project",
                principalTable: "Projects",
                principalColumn: "id_project",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.RenameIndex(
                name: "IX_CommandsComments_id_command",
                table: "CommandsComments",
                newName: "IX_CommandsCommentaires_id_command");

            migrationBuilder.RenameIndex(
                name: "IX_CommandsComments_id_user",
                table: "CommandsComments",
                newName: "IX_CommandsCommentaires_id_user");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectsComments_id_project",
                table: "ProjectsComments",
                newName: "IX_ProjetsCommentaires_id_project");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectsComments_id_user",
                table: "ProjectsComments",
                newName: "IX_ProjetsCommentaires_id_user");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectsDocuments_id_project",
                table: "ProjectsDocuments",
                newName: "IX_ProjetsDocuments_id_project");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectsItems_id_item",
                table: "ProjectsItems",
                newName: "IX_ProjetsItems_id_item");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectsProjectTags_id_project_tag",
                table: "ProjectsProjectTags",
                newName: "IX_ProjetsProjetTags_id_project_tag");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectsStatus_id_project",
                table: "ProjectsStatus",
                newName: "IX_ProjetsStatus_id_project");

            migrationBuilder.RenameTable(
                name: "CommandsComments",
                newName: "CommandsCommentaires");

            migrationBuilder.RenameTable(
                name: "AI",
                newName: "IA");

            migrationBuilder.RenameTable(
                name: "Projects",
                newName: "Projets");

            migrationBuilder.RenameTable(
                name: "ProjectsComments",
                newName: "ProjetsCommentaires");

            migrationBuilder.RenameTable(
                name: "ProjectsDocuments",
                newName: "ProjetsDocuments");

            migrationBuilder.RenameTable(
                name: "ProjectsItems",
                newName: "ProjetsItems");

            migrationBuilder.RenameTable(
                name: "ProjectsProjectTags",
                newName: "ProjetsProjetTags");

            migrationBuilder.RenameTable(
                name: "ProjectsStatus",
                newName: "ProjetsStatus");

            migrationBuilder.RenameTable(
                name: "ProjectTags",
                newName: "ProjetTags");
        }
    }
}

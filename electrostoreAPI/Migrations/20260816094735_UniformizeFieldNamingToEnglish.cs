using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class UniformizeFieldNamingToEnglish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsCommentaires_Projets_id_projet",
                table: "ProjetsCommentaires");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsDocuments_Projets_id_projet",
                table: "ProjetsDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsItems_Projets_id_projet",
                table: "ProjetsItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsProjetTags_ProjetTags_id_projet_tag",
                table: "ProjetsProjetTags");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsProjetTags_Projets_id_projet",
                table: "ProjetsProjetTags");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsStatus_Projets_id_projet",
                table: "ProjetsStatus");

            migrationBuilder.RenameColumn(
                name: "nom_user",
                table: "Users",
                newName: "name_user");

            migrationBuilder.RenameColumn(
                name: "prenom_user",
                table: "Users",
                newName: "firstname_user");

            migrationBuilder.RenameColumn(
                name: "mdp_user",
                table: "Users",
                newName: "password_user");

            migrationBuilder.RenameColumn(
                name: "poids_tag",
                table: "Tags",
                newName: "weight_tag");

            migrationBuilder.RenameColumn(
                name: "nom_tag",
                table: "Tags",
                newName: "name_tag");

            migrationBuilder.RenameColumn(
                name: "nom_store",
                table: "Stores",
                newName: "name_store");

            migrationBuilder.RenameColumn(
                name: "poids_projet_tag",
                table: "ProjetTags",
                newName: "weight_project_tag");

            migrationBuilder.RenameColumn(
                name: "nom_projet_tag",
                table: "ProjetTags",
                newName: "name_project_tag");

            migrationBuilder.RenameColumn(
                name: "id_projet_tag",
                table: "ProjetTags",
                newName: "id_project_tag");

            migrationBuilder.RenameColumn(
                name: "status_projet",
                table: "ProjetsStatus",
                newName: "status_project");

            migrationBuilder.RenameColumn(
                name: "id_projet",
                table: "ProjetsStatus",
                newName: "id_project");

            migrationBuilder.RenameColumn(
                name: "id_projet_status",
                table: "ProjetsStatus",
                newName: "id_project_status");

            migrationBuilder.RenameIndex(
                name: "IX_ProjetsStatus_id_projet",
                table: "ProjetsStatus",
                newName: "IX_ProjetsStatus_id_project");

            migrationBuilder.RenameColumn(
                name: "id_projet_tag",
                table: "ProjetsProjetTags",
                newName: "id_project_tag");

            migrationBuilder.RenameColumn(
                name: "id_projet",
                table: "ProjetsProjetTags",
                newName: "id_project");

            migrationBuilder.RenameIndex(
                name: "IX_ProjetsProjetTags_id_projet_tag",
                table: "ProjetsProjetTags",
                newName: "IX_ProjetsProjetTags_id_project_tag");

            migrationBuilder.RenameColumn(
                name: "qte_projet_item",
                table: "ProjetsItems",
                newName: "quantity_project_item");

            migrationBuilder.RenameColumn(
                name: "id_projet",
                table: "ProjetsItems",
                newName: "id_project");

            migrationBuilder.RenameColumn(
                name: "url_projet_document",
                table: "ProjetsDocuments",
                newName: "url_project_document");

            migrationBuilder.RenameColumn(
                name: "type_projet_document",
                table: "ProjetsDocuments",
                newName: "type_project_document");

            migrationBuilder.RenameColumn(
                name: "size_projet_document",
                table: "ProjetsDocuments",
                newName: "size_project_document");

            migrationBuilder.RenameColumn(
                name: "name_projet_document",
                table: "ProjetsDocuments",
                newName: "name_project_document");

            migrationBuilder.RenameColumn(
                name: "id_projet",
                table: "ProjetsDocuments",
                newName: "id_project");

            migrationBuilder.RenameColumn(
                name: "id_projet_document",
                table: "ProjetsDocuments",
                newName: "id_project_document");

            migrationBuilder.RenameIndex(
                name: "IX_ProjetsDocuments_id_projet",
                table: "ProjetsDocuments",
                newName: "IX_ProjetsDocuments_id_project");

            migrationBuilder.RenameColumn(
                name: "id_projet",
                table: "ProjetsCommentaires",
                newName: "id_project");

            migrationBuilder.RenameColumn(
                name: "contenu_projet_commentaire",
                table: "ProjetsCommentaires",
                newName: "content_project_comment");

            migrationBuilder.RenameColumn(
                name: "id_projet_commentaire",
                table: "ProjetsCommentaires",
                newName: "id_project_comment");

            migrationBuilder.RenameIndex(
                name: "IX_ProjetsCommentaires_id_projet",
                table: "ProjetsCommentaires",
                newName: "IX_ProjetsCommentaires_id_project");

            migrationBuilder.RenameColumn(
                name: "url_projet",
                table: "Projets",
                newName: "url_project");

            migrationBuilder.RenameColumn(
                name: "status_projet",
                table: "Projets",
                newName: "status_project");

            migrationBuilder.RenameColumn(
                name: "nom_projet",
                table: "Projets",
                newName: "name_project");

            migrationBuilder.RenameColumn(
                name: "description_projet",
                table: "Projets",
                newName: "description_project");

            migrationBuilder.RenameColumn(
                name: "id_projet",
                table: "Projets",
                newName: "id_project");

            migrationBuilder.RenameColumn(
                name: "seuil_max_item_item_box",
                table: "ItemsBoxs",
                newName: "threshold_max_item_item_box");

            migrationBuilder.RenameColumn(
                name: "qte_item_box",
                table: "ItemsBoxs",
                newName: "quantity_item_box");

            migrationBuilder.RenameColumn(
                name: "seuil_min_item",
                table: "Items",
                newName: "threshold_min_item");

            migrationBuilder.RenameColumn(
                name: "nom_img",
                table: "Imgs",
                newName: "name_img");

            migrationBuilder.RenameColumn(
                name: "nom_ia",
                table: "IA",
                newName: "name_ia");

            migrationBuilder.RenameColumn(
                name: "qte_command_item",
                table: "CommandsItems",
                newName: "quantity_command_item");

            migrationBuilder.RenameColumn(
                name: "prix_command_item",
                table: "CommandsItems",
                newName: "price_command_item");

            migrationBuilder.RenameColumn(
                name: "contenu_command_commentaire",
                table: "CommandsCommentaires",
                newName: "content_command_comment");

            migrationBuilder.RenameColumn(
                name: "id_command_commentaire",
                table: "CommandsCommentaires",
                newName: "id_command_comment");

            migrationBuilder.RenameColumn(
                name: "shipper_adress",
                table: "Commands",
                newName: "shipper_address");

            migrationBuilder.RenameColumn(
                name: "recipient_adress",
                table: "Commands",
                newName: "recipient_address");

            migrationBuilder.RenameColumn(
                name: "prix_command",
                table: "Commands",
                newName: "price_command");

            migrationBuilder.RenameColumn(
                name: "date_livraison_command",
                table: "Commands",
                newName: "date_delivery_command");

            migrationBuilder.RenameColumn(
                name: "nom_camera",
                table: "Cameras",
                newName: "name_camera");

            migrationBuilder.RenameColumn(
                name: "mdp_camera",
                table: "Cameras",
                newName: "password_camera");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsCommentaires_Projets_id_project",
                table: "ProjetsCommentaires",
                column: "id_project",
                principalTable: "Projets",
                principalColumn: "id_project",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsDocuments_Projets_id_project",
                table: "ProjetsDocuments",
                column: "id_project",
                principalTable: "Projets",
                principalColumn: "id_project",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsItems_Projets_id_project",
                table: "ProjetsItems",
                column: "id_project",
                principalTable: "Projets",
                principalColumn: "id_project",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsProjetTags_ProjetTags_id_project_tag",
                table: "ProjetsProjetTags",
                column: "id_project_tag",
                principalTable: "ProjetTags",
                principalColumn: "id_project_tag",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsProjetTags_Projets_id_project",
                table: "ProjetsProjetTags",
                column: "id_project",
                principalTable: "Projets",
                principalColumn: "id_project",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsStatus_Projets_id_project",
                table: "ProjetsStatus",
                column: "id_project",
                principalTable: "Projets",
                principalColumn: "id_project",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsCommentaires_Projets_id_project",
                table: "ProjetsCommentaires");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsDocuments_Projets_id_project",
                table: "ProjetsDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsItems_Projets_id_project",
                table: "ProjetsItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsProjetTags_ProjetTags_id_project_tag",
                table: "ProjetsProjetTags");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsProjetTags_Projets_id_project",
                table: "ProjetsProjetTags");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsStatus_Projets_id_project",
                table: "ProjetsStatus");

            migrationBuilder.RenameColumn(
                name: "password_user",
                table: "Users",
                newName: "mdp_user");

            migrationBuilder.RenameColumn(
                name: "name_user",
                table: "Users",
                newName: "nom_user");

            migrationBuilder.RenameColumn(
                name: "firstname_user",
                table: "Users",
                newName: "prenom_user");

            migrationBuilder.RenameColumn(
                name: "weight_tag",
                table: "Tags",
                newName: "poids_tag");

            migrationBuilder.RenameColumn(
                name: "name_tag",
                table: "Tags",
                newName: "nom_tag");

            migrationBuilder.RenameColumn(
                name: "name_store",
                table: "Stores",
                newName: "nom_store");

            migrationBuilder.RenameColumn(
                name: "weight_project_tag",
                table: "ProjetTags",
                newName: "poids_projet_tag");

            migrationBuilder.RenameColumn(
                name: "name_project_tag",
                table: "ProjetTags",
                newName: "nom_projet_tag");

            migrationBuilder.RenameColumn(
                name: "id_project_tag",
                table: "ProjetTags",
                newName: "id_projet_tag");

            migrationBuilder.RenameColumn(
                name: "status_project",
                table: "ProjetsStatus",
                newName: "status_projet");

            migrationBuilder.RenameColumn(
                name: "id_project",
                table: "ProjetsStatus",
                newName: "id_projet");

            migrationBuilder.RenameColumn(
                name: "id_project_status",
                table: "ProjetsStatus",
                newName: "id_projet_status");

            migrationBuilder.RenameIndex(
                name: "IX_ProjetsStatus_id_project",
                table: "ProjetsStatus",
                newName: "IX_ProjetsStatus_id_projet");

            migrationBuilder.RenameColumn(
                name: "id_project_tag",
                table: "ProjetsProjetTags",
                newName: "id_projet_tag");

            migrationBuilder.RenameColumn(
                name: "id_project",
                table: "ProjetsProjetTags",
                newName: "id_projet");

            migrationBuilder.RenameIndex(
                name: "IX_ProjetsProjetTags_id_project_tag",
                table: "ProjetsProjetTags",
                newName: "IX_ProjetsProjetTags_id_projet_tag");

            migrationBuilder.RenameColumn(
                name: "quantity_project_item",
                table: "ProjetsItems",
                newName: "qte_projet_item");

            migrationBuilder.RenameColumn(
                name: "id_project",
                table: "ProjetsItems",
                newName: "id_projet");

            migrationBuilder.RenameColumn(
                name: "url_project_document",
                table: "ProjetsDocuments",
                newName: "url_projet_document");

            migrationBuilder.RenameColumn(
                name: "type_project_document",
                table: "ProjetsDocuments",
                newName: "type_projet_document");

            migrationBuilder.RenameColumn(
                name: "size_project_document",
                table: "ProjetsDocuments",
                newName: "size_projet_document");

            migrationBuilder.RenameColumn(
                name: "name_project_document",
                table: "ProjetsDocuments",
                newName: "name_projet_document");

            migrationBuilder.RenameColumn(
                name: "id_project",
                table: "ProjetsDocuments",
                newName: "id_projet");

            migrationBuilder.RenameColumn(
                name: "id_project_document",
                table: "ProjetsDocuments",
                newName: "id_projet_document");

            migrationBuilder.RenameIndex(
                name: "IX_ProjetsDocuments_id_project",
                table: "ProjetsDocuments",
                newName: "IX_ProjetsDocuments_id_projet");

            migrationBuilder.RenameColumn(
                name: "id_project",
                table: "ProjetsCommentaires",
                newName: "id_projet");

            migrationBuilder.RenameColumn(
                name: "content_project_comment",
                table: "ProjetsCommentaires",
                newName: "contenu_projet_commentaire");

            migrationBuilder.RenameColumn(
                name: "id_project_comment",
                table: "ProjetsCommentaires",
                newName: "id_projet_commentaire");

            migrationBuilder.RenameIndex(
                name: "IX_ProjetsCommentaires_id_project",
                table: "ProjetsCommentaires",
                newName: "IX_ProjetsCommentaires_id_projet");

            migrationBuilder.RenameColumn(
                name: "url_project",
                table: "Projets",
                newName: "url_projet");

            migrationBuilder.RenameColumn(
                name: "status_project",
                table: "Projets",
                newName: "status_projet");

            migrationBuilder.RenameColumn(
                name: "name_project",
                table: "Projets",
                newName: "nom_projet");

            migrationBuilder.RenameColumn(
                name: "description_project",
                table: "Projets",
                newName: "description_projet");

            migrationBuilder.RenameColumn(
                name: "id_project",
                table: "Projets",
                newName: "id_projet");

            migrationBuilder.RenameColumn(
                name: "threshold_max_item_item_box",
                table: "ItemsBoxs",
                newName: "seuil_max_item_item_box");

            migrationBuilder.RenameColumn(
                name: "quantity_item_box",
                table: "ItemsBoxs",
                newName: "qte_item_box");

            migrationBuilder.RenameColumn(
                name: "threshold_min_item",
                table: "Items",
                newName: "seuil_min_item");

            migrationBuilder.RenameColumn(
                name: "name_img",
                table: "Imgs",
                newName: "nom_img");

            migrationBuilder.RenameColumn(
                name: "name_ia",
                table: "IA",
                newName: "nom_ia");

            migrationBuilder.RenameColumn(
                name: "quantity_command_item",
                table: "CommandsItems",
                newName: "qte_command_item");

            migrationBuilder.RenameColumn(
                name: "price_command_item",
                table: "CommandsItems",
                newName: "prix_command_item");

            migrationBuilder.RenameColumn(
                name: "content_command_comment",
                table: "CommandsCommentaires",
                newName: "contenu_command_commentaire");

            migrationBuilder.RenameColumn(
                name: "id_command_comment",
                table: "CommandsCommentaires",
                newName: "id_command_commentaire");

            migrationBuilder.RenameColumn(
                name: "shipper_address",
                table: "Commands",
                newName: "shipper_adress");

            migrationBuilder.RenameColumn(
                name: "recipient_address",
                table: "Commands",
                newName: "recipient_adress");

            migrationBuilder.RenameColumn(
                name: "price_command",
                table: "Commands",
                newName: "prix_command");

            migrationBuilder.RenameColumn(
                name: "date_delivery_command",
                table: "Commands",
                newName: "date_livraison_command");

            migrationBuilder.RenameColumn(
                name: "password_camera",
                table: "Cameras",
                newName: "mdp_camera");

            migrationBuilder.RenameColumn(
                name: "name_camera",
                table: "Cameras",
                newName: "nom_camera");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsCommentaires_Projets_id_projet",
                table: "ProjetsCommentaires",
                column: "id_projet",
                principalTable: "Projets",
                principalColumn: "id_projet",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsDocuments_Projets_id_projet",
                table: "ProjetsDocuments",
                column: "id_projet",
                principalTable: "Projets",
                principalColumn: "id_projet",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsItems_Projets_id_projet",
                table: "ProjetsItems",
                column: "id_projet",
                principalTable: "Projets",
                principalColumn: "id_projet",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsProjetTags_ProjetTags_id_projet_tag",
                table: "ProjetsProjetTags",
                column: "id_projet_tag",
                principalTable: "ProjetTags",
                principalColumn: "id_projet_tag",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsProjetTags_Projets_id_projet",
                table: "ProjetsProjetTags",
                column: "id_projet",
                principalTable: "Projets",
                principalColumn: "id_projet",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsStatus_Projets_id_projet",
                table: "ProjetsStatus",
                column: "id_projet",
                principalTable: "Projets",
                principalColumn: "id_projet",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

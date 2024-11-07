using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostore.Migrations
{
    /// <inheritdoc />
    public partial class uniformName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommandsDocuments_Commands_Commandid_command",
                table: "CommandsDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemsDocuments_Items_Itemid_item",
                table: "ItemsDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsDocuments_Projets_Projetid_projet",
                table: "ProjetsDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ProjetsDocuments_Projetid_projet",
                table: "ProjetsDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ItemsDocuments_Itemid_item",
                table: "ItemsDocuments");

            migrationBuilder.DropIndex(
                name: "IX_CommandsDocuments_Commandid_command",
                table: "CommandsDocuments");

            migrationBuilder.DropColumn(
                name: "Projetid_projet",
                table: "ProjetsDocuments");

            migrationBuilder.DropColumn(
                name: "Itemid_item",
                table: "ItemsDocuments");

            migrationBuilder.DropColumn(
                name: "datasheet_item",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Commandid_command",
                table: "CommandsDocuments");

            migrationBuilder.RenameColumn(
                name: "qte_projetitem",
                table: "ProjetsItems",
                newName: "qte_projet_item");

            migrationBuilder.RenameColumn(
                name: "date_projetcommentaire",
                table: "ProjetsCommentaires",
                newName: "date_projet_commentaire");

            migrationBuilder.RenameColumn(
                name: "date_modif_projetcommentaire",
                table: "ProjetsCommentaires",
                newName: "date_modif_projet_commentaire");

            migrationBuilder.RenameColumn(
                name: "contenu_projetcommentaire",
                table: "ProjetsCommentaires",
                newName: "contenu_projet_commentaire");

            migrationBuilder.RenameColumn(
                name: "id_projetcommentaire",
                table: "ProjetsCommentaires",
                newName: "id_projet_commentaire");

            migrationBuilder.RenameColumn(
                name: "seuil_max_itemitembox",
                table: "ItemsBoxs",
                newName: "seuil_max_item_item_box");

            migrationBuilder.RenameColumn(
                name: "qte_itembox",
                table: "ItemsBoxs",
                newName: "qte_item_box");

            migrationBuilder.RenameColumn(
                name: "qte_commanditem",
                table: "CommandsItems",
                newName: "qte_command_item");

            migrationBuilder.RenameColumn(
                name: "prix_commanditem",
                table: "CommandsItems",
                newName: "prix_command_item");

            migrationBuilder.RenameColumn(
                name: "date_modif_commandcommentaire",
                table: "CommandsCommentaires",
                newName: "date_modif_command_commentaire");

            migrationBuilder.RenameColumn(
                name: "date_commandcommentaire",
                table: "CommandsCommentaires",
                newName: "date_command_commentaire");

            migrationBuilder.RenameColumn(
                name: "contenu_commandcommentaire",
                table: "CommandsCommentaires",
                newName: "contenu_command_commentaire");

            migrationBuilder.AddColumn<string>(
                name: "url_projet_document",
                table: "ProjetsDocuments",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "url_item_document",
                table: "ItemsDocuments",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "url_command_document",
                table: "CommandsDocuments",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ProjetsDocuments_id_projet",
                table: "ProjetsDocuments",
                column: "id_projet");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsDocuments_id_item",
                table: "ItemsDocuments",
                column: "id_item");

            migrationBuilder.CreateIndex(
                name: "IX_CommandsDocuments_id_command",
                table: "CommandsDocuments",
                column: "id_command");

            migrationBuilder.AddForeignKey(
                name: "FK_CommandsDocuments_Commands_id_command",
                table: "CommandsDocuments",
                column: "id_command",
                principalTable: "Commands",
                principalColumn: "id_command",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemsDocuments_Items_id_item",
                table: "ItemsDocuments",
                column: "id_item",
                principalTable: "Items",
                principalColumn: "id_item",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsDocuments_Projets_id_projet",
                table: "ProjetsDocuments",
                column: "id_projet",
                principalTable: "Projets",
                principalColumn: "id_projet",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommandsDocuments_Commands_id_command",
                table: "CommandsDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemsDocuments_Items_id_item",
                table: "ItemsDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetsDocuments_Projets_id_projet",
                table: "ProjetsDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ProjetsDocuments_id_projet",
                table: "ProjetsDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ItemsDocuments_id_item",
                table: "ItemsDocuments");

            migrationBuilder.DropIndex(
                name: "IX_CommandsDocuments_id_command",
                table: "CommandsDocuments");

            migrationBuilder.DropColumn(
                name: "url_projet_document",
                table: "ProjetsDocuments");

            migrationBuilder.DropColumn(
                name: "url_item_document",
                table: "ItemsDocuments");

            migrationBuilder.DropColumn(
                name: "url_command_document",
                table: "CommandsDocuments");

            migrationBuilder.RenameColumn(
                name: "qte_projet_item",
                table: "ProjetsItems",
                newName: "qte_projetitem");

            migrationBuilder.RenameColumn(
                name: "date_projet_commentaire",
                table: "ProjetsCommentaires",
                newName: "date_projetcommentaire");

            migrationBuilder.RenameColumn(
                name: "date_modif_projet_commentaire",
                table: "ProjetsCommentaires",
                newName: "date_modif_projetcommentaire");

            migrationBuilder.RenameColumn(
                name: "contenu_projet_commentaire",
                table: "ProjetsCommentaires",
                newName: "contenu_projetcommentaire");

            migrationBuilder.RenameColumn(
                name: "id_projet_commentaire",
                table: "ProjetsCommentaires",
                newName: "id_projetcommentaire");

            migrationBuilder.RenameColumn(
                name: "seuil_max_item_item_box",
                table: "ItemsBoxs",
                newName: "seuil_max_itemitembox");

            migrationBuilder.RenameColumn(
                name: "qte_item_box",
                table: "ItemsBoxs",
                newName: "qte_itembox");

            migrationBuilder.RenameColumn(
                name: "qte_command_item",
                table: "CommandsItems",
                newName: "qte_commanditem");

            migrationBuilder.RenameColumn(
                name: "prix_command_item",
                table: "CommandsItems",
                newName: "prix_commanditem");

            migrationBuilder.RenameColumn(
                name: "date_modif_command_commentaire",
                table: "CommandsCommentaires",
                newName: "date_modif_commandcommentaire");

            migrationBuilder.RenameColumn(
                name: "date_command_commentaire",
                table: "CommandsCommentaires",
                newName: "date_commandcommentaire");

            migrationBuilder.RenameColumn(
                name: "contenu_command_commentaire",
                table: "CommandsCommentaires",
                newName: "contenu_commandcommentaire");

            migrationBuilder.AddColumn<int>(
                name: "Projetid_projet",
                table: "ProjetsDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Itemid_item",
                table: "ItemsDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "datasheet_item",
                table: "Items",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Commandid_command",
                table: "CommandsDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProjetsDocuments_Projetid_projet",
                table: "ProjetsDocuments",
                column: "Projetid_projet");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsDocuments_Itemid_item",
                table: "ItemsDocuments",
                column: "Itemid_item");

            migrationBuilder.CreateIndex(
                name: "IX_CommandsDocuments_Commandid_command",
                table: "CommandsDocuments",
                column: "Commandid_command");

            migrationBuilder.AddForeignKey(
                name: "FK_CommandsDocuments_Commands_Commandid_command",
                table: "CommandsDocuments",
                column: "Commandid_command",
                principalTable: "Commands",
                principalColumn: "id_command",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemsDocuments_Items_Itemid_item",
                table: "ItemsDocuments",
                column: "Itemid_item",
                principalTable: "Items",
                principalColumn: "id_item",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetsDocuments_Projets_Projetid_projet",
                table: "ProjetsDocuments",
                column: "Projetid_projet",
                principalTable: "Projets",
                principalColumn: "id_projet",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostore.Migrations
{
    /// <inheritdoc />
    public partial class initDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Cameras",
                columns: table => new
                {
                    id_camera = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nom_camera = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    url_camera = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_camera = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mdp_camera = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cameras", x => x.id_camera);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Commands",
                columns: table => new
                {
                    id_command = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    prix_command = table.Column<float>(type: "float", nullable: false),
                    url_command = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status_command = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_command = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_livraison_command = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commands", x => x.id_command);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "IA",
                columns: table => new
                {
                    id_ia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nom_ia = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description_ia = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_ia = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    trained_ia = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IA", x => x.id_ia);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Projets",
                columns: table => new
                {
                    id_projet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nom_projet = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description_projet = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    url_projet = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status_projet = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_projet = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_fin_projet = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projets", x => x.id_projet);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Stores",
                columns: table => new
                {
                    id_store = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nom_store = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    xlength_store = table.Column<int>(type: "int", nullable: false),
                    ylength_store = table.Column<int>(type: "int", nullable: false),
                    mqtt_name_store = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stores", x => x.id_store);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    id_tag = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nom_tag = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    poids_tag = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.id_tag);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id_user = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nom_user = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    prenom_user = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email_user = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mdp_user = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    role_user = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reset_token = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reset_token_expiration = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.id_user);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CommandsDocuments",
                columns: table => new
                {
                    id_command_document = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    url_command_document = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_command_document = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type_command_document = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    size_command_document = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    date_command_document = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    id_command = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandsDocuments", x => x.id_command_document);
                    table.ForeignKey(
                        name: "FK_CommandsDocuments_Commands_id_command",
                        column: x => x.id_command,
                        principalTable: "Commands",
                        principalColumn: "id_command",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProjetsDocuments",
                columns: table => new
                {
                    id_projet_document = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    url_projet_document = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_projet_document = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type_projet_document = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    size_projet_document = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    date_projet_document = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    id_projet = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjetsDocuments", x => x.id_projet_document);
                    table.ForeignKey(
                        name: "FK_ProjetsDocuments_Projets_id_projet",
                        column: x => x.id_projet,
                        principalTable: "Projets",
                        principalColumn: "id_projet",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Boxs",
                columns: table => new
                {
                    id_box = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_store = table.Column<int>(type: "int", nullable: false),
                    xstart_box = table.Column<int>(type: "int", nullable: false),
                    ystart_box = table.Column<int>(type: "int", nullable: false),
                    xend_box = table.Column<int>(type: "int", nullable: false),
                    yend_box = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boxs", x => x.id_box);
                    table.ForeignKey(
                        name: "FK_Boxs_Stores_id_store",
                        column: x => x.id_store,
                        principalTable: "Stores",
                        principalColumn: "id_store",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Leds",
                columns: table => new
                {
                    id_led = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_store = table.Column<int>(type: "int", nullable: false),
                    x_led = table.Column<int>(type: "int", nullable: false),
                    y_led = table.Column<int>(type: "int", nullable: false),
                    mqtt_led_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leds", x => x.id_led);
                    table.ForeignKey(
                        name: "FK_Leds_Stores_id_store",
                        column: x => x.id_store,
                        principalTable: "Stores",
                        principalColumn: "id_store",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StoresTags",
                columns: table => new
                {
                    id_tag = table.Column<int>(type: "int", nullable: false),
                    id_store = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoresTags", x => new { x.id_store, x.id_tag });
                    table.ForeignKey(
                        name: "FK_StoresTags_Stores_id_store",
                        column: x => x.id_store,
                        principalTable: "Stores",
                        principalColumn: "id_store",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoresTags_Tags_id_tag",
                        column: x => x.id_tag,
                        principalTable: "Tags",
                        principalColumn: "id_tag",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CommandsCommentaires",
                columns: table => new
                {
                    id_commandcommentaire = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_user = table.Column<int>(type: "int", nullable: true),
                    id_command = table.Column<int>(type: "int", nullable: false),
                    contenu_command_commentaire = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_command_commentaire = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modif_command_commentaire = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandsCommentaires", x => x.id_commandcommentaire);
                    table.ForeignKey(
                        name: "FK_CommandsCommentaires_Commands_id_command",
                        column: x => x.id_command,
                        principalTable: "Commands",
                        principalColumn: "id_command",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommandsCommentaires_Users_id_user",
                        column: x => x.id_user,
                        principalTable: "Users",
                        principalColumn: "id_user");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "JWIAccessToken",
                columns: table => new
                {
                    id_jwi_access = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    is_revoked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by_ip = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revoked_by_ip = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revoked_reason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_user = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JWIAccessToken", x => x.id_jwi_access);
                    table.ForeignKey(
                        name: "FK_JWIAccessToken_Users_id_user",
                        column: x => x.id_user,
                        principalTable: "Users",
                        principalColumn: "id_user",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProjetsCommentaires",
                columns: table => new
                {
                    id_projet_commentaire = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_user = table.Column<int>(type: "int", nullable: true),
                    id_projet = table.Column<int>(type: "int", nullable: false),
                    contenu_projet_commentaire = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_projet_commentaire = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modif_projet_commentaire = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjetsCommentaires", x => x.id_projet_commentaire);
                    table.ForeignKey(
                        name: "FK_ProjetsCommentaires_Projets_id_projet",
                        column: x => x.id_projet,
                        principalTable: "Projets",
                        principalColumn: "id_projet",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjetsCommentaires_Users_id_user",
                        column: x => x.id_user,
                        principalTable: "Users",
                        principalColumn: "id_user");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BoxsTags",
                columns: table => new
                {
                    id_box = table.Column<int>(type: "int", nullable: false),
                    id_tag = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoxsTags", x => new { x.id_box, x.id_tag });
                    table.ForeignKey(
                        name: "FK_BoxsTags_Boxs_id_box",
                        column: x => x.id_box,
                        principalTable: "Boxs",
                        principalColumn: "id_box",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BoxsTags_Tags_id_tag",
                        column: x => x.id_tag,
                        principalTable: "Tags",
                        principalColumn: "id_tag",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "JWIRefreshToken",
                columns: table => new
                {
                    id_jwi_refresh = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    is_revoked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by_ip = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revoked_by_ip = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revoked_reason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_user = table.Column<int>(type: "int", nullable: false),
                    id_jwi_access = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JWIRefreshToken", x => x.id_jwi_refresh);
                    table.ForeignKey(
                        name: "FK_JWIRefreshToken_JWIAccessToken_id_jwi_access",
                        column: x => x.id_jwi_access,
                        principalTable: "JWIAccessToken",
                        principalColumn: "id_jwi_access",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JWIRefreshToken_Users_id_user",
                        column: x => x.id_user,
                        principalTable: "Users",
                        principalColumn: "id_user",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CommandsItems",
                columns: table => new
                {
                    id_item = table.Column<int>(type: "int", nullable: false),
                    id_command = table.Column<int>(type: "int", nullable: false),
                    qte_command_item = table.Column<int>(type: "int", nullable: false),
                    prix_command_item = table.Column<float>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandsItems", x => new { x.id_command, x.id_item });
                    table.ForeignKey(
                        name: "FK_CommandsItems_Commands_id_command",
                        column: x => x.id_command,
                        principalTable: "Commands",
                        principalColumn: "id_command",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Imgs",
                columns: table => new
                {
                    id_img = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_item = table.Column<int>(type: "int", nullable: false),
                    nom_img = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    url_img = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description_img = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_img = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Imgs", x => x.id_img);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    id_item = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_img = table.Column<int>(type: "int", nullable: true),
                    nom_item = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    seuil_min_item = table.Column<int>(type: "int", nullable: false),
                    description_item = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.id_item);
                    table.ForeignKey(
                        name: "FK_Items_Imgs_id_img",
                        column: x => x.id_img,
                        principalTable: "Imgs",
                        principalColumn: "id_img");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ItemsBoxs",
                columns: table => new
                {
                    id_box = table.Column<int>(type: "int", nullable: false),
                    id_item = table.Column<int>(type: "int", nullable: false),
                    qte_item_box = table.Column<int>(type: "int", nullable: false),
                    seuil_max_item_item_box = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemsBoxs", x => new { x.id_item, x.id_box });
                    table.ForeignKey(
                        name: "FK_ItemsBoxs_Boxs_id_box",
                        column: x => x.id_box,
                        principalTable: "Boxs",
                        principalColumn: "id_box",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemsBoxs_Items_id_item",
                        column: x => x.id_item,
                        principalTable: "Items",
                        principalColumn: "id_item",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ItemsDocuments",
                columns: table => new
                {
                    id_item_document = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    url_item_document = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_item_document = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type_item_document = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    size_item_document = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    date_item_document = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    id_item = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemsDocuments", x => x.id_item_document);
                    table.ForeignKey(
                        name: "FK_ItemsDocuments_Items_id_item",
                        column: x => x.id_item,
                        principalTable: "Items",
                        principalColumn: "id_item",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ItemsTags",
                columns: table => new
                {
                    id_tag = table.Column<int>(type: "int", nullable: false),
                    id_item = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemsTags", x => new { x.id_item, x.id_tag });
                    table.ForeignKey(
                        name: "FK_ItemsTags_Items_id_item",
                        column: x => x.id_item,
                        principalTable: "Items",
                        principalColumn: "id_item",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemsTags_Tags_id_tag",
                        column: x => x.id_tag,
                        principalTable: "Tags",
                        principalColumn: "id_tag",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProjetsItems",
                columns: table => new
                {
                    id_projet = table.Column<int>(type: "int", nullable: false),
                    id_item = table.Column<int>(type: "int", nullable: false),
                    qte_projet_item = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjetsItems", x => new { x.id_projet, x.id_item });
                    table.ForeignKey(
                        name: "FK_ProjetsItems_Items_id_item",
                        column: x => x.id_item,
                        principalTable: "Items",
                        principalColumn: "id_item",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjetsItems_Projets_id_projet",
                        column: x => x.id_projet,
                        principalTable: "Projets",
                        principalColumn: "id_projet",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Boxs_id_store",
                table: "Boxs",
                column: "id_store");

            migrationBuilder.CreateIndex(
                name: "IX_BoxsTags_id_tag",
                table: "BoxsTags",
                column: "id_tag");

            migrationBuilder.CreateIndex(
                name: "IX_CommandsCommentaires_id_command",
                table: "CommandsCommentaires",
                column: "id_command");

            migrationBuilder.CreateIndex(
                name: "IX_CommandsCommentaires_id_user",
                table: "CommandsCommentaires",
                column: "id_user");

            migrationBuilder.CreateIndex(
                name: "IX_CommandsDocuments_id_command",
                table: "CommandsDocuments",
                column: "id_command");

            migrationBuilder.CreateIndex(
                name: "IX_CommandsItems_id_item",
                table: "CommandsItems",
                column: "id_item");

            migrationBuilder.CreateIndex(
                name: "IX_Imgs_id_item",
                table: "Imgs",
                column: "id_item");

            migrationBuilder.CreateIndex(
                name: "IX_Items_id_img",
                table: "Items",
                column: "id_img");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsBoxs_id_box",
                table: "ItemsBoxs",
                column: "id_box");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsDocuments_id_item",
                table: "ItemsDocuments",
                column: "id_item");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsTags_id_tag",
                table: "ItemsTags",
                column: "id_tag");

            migrationBuilder.CreateIndex(
                name: "IX_JWIAccessToken_id_user",
                table: "JWIAccessToken",
                column: "id_user");

            migrationBuilder.CreateIndex(
                name: "IX_JWIRefreshToken_id_jwi_access",
                table: "JWIRefreshToken",
                column: "id_jwi_access");

            migrationBuilder.CreateIndex(
                name: "IX_JWIRefreshToken_id_user",
                table: "JWIRefreshToken",
                column: "id_user");

            migrationBuilder.CreateIndex(
                name: "IX_Leds_id_store",
                table: "Leds",
                column: "id_store");

            migrationBuilder.CreateIndex(
                name: "IX_ProjetsCommentaires_id_projet",
                table: "ProjetsCommentaires",
                column: "id_projet");

            migrationBuilder.CreateIndex(
                name: "IX_ProjetsCommentaires_id_user",
                table: "ProjetsCommentaires",
                column: "id_user");

            migrationBuilder.CreateIndex(
                name: "IX_ProjetsDocuments_id_projet",
                table: "ProjetsDocuments",
                column: "id_projet");

            migrationBuilder.CreateIndex(
                name: "IX_ProjetsItems_id_item",
                table: "ProjetsItems",
                column: "id_item");

            migrationBuilder.CreateIndex(
                name: "IX_StoresTags_id_tag",
                table: "StoresTags",
                column: "id_tag");

            migrationBuilder.AddForeignKey(
                name: "FK_CommandsItems_Items_id_item",
                table: "CommandsItems",
                column: "id_item",
                principalTable: "Items",
                principalColumn: "id_item",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Imgs_Items_id_item",
                table: "Imgs",
                column: "id_item",
                principalTable: "Items",
                principalColumn: "id_item",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Imgs_Items_id_item",
                table: "Imgs");

            migrationBuilder.DropTable(
                name: "BoxsTags");

            migrationBuilder.DropTable(
                name: "Cameras");

            migrationBuilder.DropTable(
                name: "CommandsCommentaires");

            migrationBuilder.DropTable(
                name: "CommandsDocuments");

            migrationBuilder.DropTable(
                name: "CommandsItems");

            migrationBuilder.DropTable(
                name: "IA");

            migrationBuilder.DropTable(
                name: "ItemsBoxs");

            migrationBuilder.DropTable(
                name: "ItemsDocuments");

            migrationBuilder.DropTable(
                name: "ItemsTags");

            migrationBuilder.DropTable(
                name: "JWIRefreshToken");

            migrationBuilder.DropTable(
                name: "Leds");

            migrationBuilder.DropTable(
                name: "ProjetsCommentaires");

            migrationBuilder.DropTable(
                name: "ProjetsDocuments");

            migrationBuilder.DropTable(
                name: "ProjetsItems");

            migrationBuilder.DropTable(
                name: "StoresTags");

            migrationBuilder.DropTable(
                name: "Commands");

            migrationBuilder.DropTable(
                name: "Boxs");

            migrationBuilder.DropTable(
                name: "JWIAccessToken");

            migrationBuilder.DropTable(
                name: "Projets");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Stores");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Imgs");
        }
    }
}

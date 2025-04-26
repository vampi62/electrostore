using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostore.Migrations
{
    /// <inheritdoc />
    public partial class removeOldDateTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JWIRefreshToken");

            migrationBuilder.DropTable(
                name: "JWIAccessToken");

            migrationBuilder.DropColumn(
                name: "date_projet_document",
                table: "ProjetsDocuments");

            migrationBuilder.DropColumn(
                name: "date_modif_projet_commentaire",
                table: "ProjetsCommentaires");

            migrationBuilder.DropColumn(
                name: "date_projet_commentaire",
                table: "ProjetsCommentaires");

            migrationBuilder.DropColumn(
                name: "date_item_document",
                table: "ItemsDocuments");

            migrationBuilder.DropColumn(
                name: "date_img",
                table: "Imgs");

            migrationBuilder.DropColumn(
                name: "date_ia",
                table: "IA");

            migrationBuilder.DropColumn(
                name: "date_command_document",
                table: "CommandsDocuments");

            migrationBuilder.DropColumn(
                name: "date_command_commentaire",
                table: "CommandsCommentaires");

            migrationBuilder.DropColumn(
                name: "date_modif_command_commentaire",
                table: "CommandsCommentaires");

            migrationBuilder.CreateTable(
                name: "JwiAccessTokens",
                columns: table => new
                {
                    id_jwi_access = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    is_revoked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_by_ip = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revoked_by_ip = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revoked_reason = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_user = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JwiAccessTokens", x => x.id_jwi_access);
                    table.ForeignKey(
                        name: "FK_JwiAccessTokens_Users_id_user",
                        column: x => x.id_user,
                        principalTable: "Users",
                        principalColumn: "id_user",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "JwiRefreshTokens",
                columns: table => new
                {
                    id_jwi_refresh = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    is_revoked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_by_ip = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revoked_by_ip = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revoked_reason = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_user = table.Column<int>(type: "int", nullable: false),
                    id_jwi_access = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JwiRefreshTokens", x => x.id_jwi_refresh);
                    table.ForeignKey(
                        name: "FK_JwiRefreshTokens_JwiAccessTokens_id_jwi_access",
                        column: x => x.id_jwi_access,
                        principalTable: "JwiAccessTokens",
                        principalColumn: "id_jwi_access",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JwiRefreshTokens_Users_id_user",
                        column: x => x.id_user,
                        principalTable: "Users",
                        principalColumn: "id_user",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_JwiAccessTokens_id_user",
                table: "JwiAccessTokens",
                column: "id_user");

            migrationBuilder.CreateIndex(
                name: "IX_JwiRefreshTokens_id_jwi_access",
                table: "JwiRefreshTokens",
                column: "id_jwi_access");

            migrationBuilder.CreateIndex(
                name: "IX_JwiRefreshTokens_id_user",
                table: "JwiRefreshTokens",
                column: "id_user");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JwiRefreshTokens");

            migrationBuilder.DropTable(
                name: "JwiAccessTokens");

            migrationBuilder.AddColumn<DateTime>(
                name: "date_projet_document",
                table: "ProjetsDocuments",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_modif_projet_commentaire",
                table: "ProjetsCommentaires",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_projet_commentaire",
                table: "ProjetsCommentaires",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_item_document",
                table: "ItemsDocuments",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_img",
                table: "Imgs",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_ia",
                table: "IA",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_command_document",
                table: "CommandsDocuments",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_command_commentaire",
                table: "CommandsCommentaires",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_modif_command_commentaire",
                table: "CommandsCommentaires",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "JWIAccessToken",
                columns: table => new
                {
                    id_jwi_access = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    id_user = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by_ip = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    is_revoked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revoked_by_ip = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revoked_reason = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                name: "JWIRefreshToken",
                columns: table => new
                {
                    id_jwi_refresh = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    id_jwi_access = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    id_user = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by_ip = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    is_revoked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revoked_by_ip = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revoked_reason = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
        }
    }
}

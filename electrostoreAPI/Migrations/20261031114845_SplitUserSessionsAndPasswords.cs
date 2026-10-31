using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostoreAPI.Migrations
{
    /// <inheritdoc />
    public partial class SplitUserSessionsAndPasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_verified",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_login",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UsersExternalLogins",
                columns: table => new
                {
                    id_user_external_login = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_user = table.Column<int>(type: "int", nullable: false),
                    provider = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_subject = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersExternalLogins", x => x.id_user_external_login);
                    table.ForeignKey(
                        name: "FK_UsersExternalLogins_Users_id_user",
                        column: x => x.id_user,
                        principalTable: "Users",
                        principalColumn: "id_user",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UsersPasswords",
                columns: table => new
                {
                    id_user = table.Column<int>(type: "int", nullable: false),
                    password_user = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password_last_changed = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    reset_token = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    reset_token_expiration = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersPasswords", x => x.id_user);
                    table.ForeignKey(
                        name: "FK_UsersPasswords_Users_id_user",
                        column: x => x.id_user,
                        principalTable: "Users",
                        principalColumn: "id_user",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UsersSessions",
                columns: table => new
                {
                    id_user_session = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    id_user = table.Column<int>(type: "int", nullable: false),
                    auth_method = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id_user_external_login = table.Column<int>(type: "int", nullable: true),
                    created_by_ip = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revoked_by_ip = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersSessions", x => x.id_user_session);
                    table.ForeignKey(
                        name: "FK_UsersSessions_UsersExternalLogins_id_user_external_login",
                        column: x => x.id_user_external_login,
                        principalTable: "UsersExternalLogins",
                        principalColumn: "id_user_external_login",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UsersSessions_Users_id_user",
                        column: x => x.id_user,
                        principalTable: "Users",
                        principalColumn: "id_user",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "JwtTokens",
                columns: table => new
                {
                    id_jwi = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    id_user_session = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    token_type = table.Column<int>(type: "int", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revoked_reason = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    auth_method = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revoked_by_ip = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JwtTokens", x => x.id_jwi);
                    table.ForeignKey(
                        name: "FK_JwtTokens_UsersSessions_id_user_session",
                        column: x => x.id_user_session,
                        principalTable: "UsersSessions",
                        principalColumn: "id_user_session",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_JwtTokens_id_user_session",
                table: "JwtTokens",
                column: "id_user_session");

            migrationBuilder.CreateIndex(
                name: "IX_UsersExternalLogins_id_user",
                table: "UsersExternalLogins",
                column: "id_user");

            migrationBuilder.CreateIndex(
                name: "IX_UsersExternalLogins_provider_provider_subject",
                table: "UsersExternalLogins",
                columns: new[] { "provider", "provider_subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsersSessions_id_user",
                table: "UsersSessions",
                column: "id_user");

            migrationBuilder.CreateIndex(
                name: "IX_UsersSessions_id_user_external_login",
                table: "UsersSessions",
                column: "id_user_external_login");

            // move the existing data into the new tables before dropping the old columns / tables
            migrationBuilder.Sql(@"
                INSERT INTO `UsersPasswords` (`id_user`, `password_user`, `password_last_changed`, `reset_token`, `reset_token_expiration`, `created_at`, `updated_at`)
                SELECT `id_user`, `password_user`, NULL, `reset_token`, `reset_token_expiration`, `created_at`, `updated_at`
                FROM `Users`;");

            // one session per distinct session id, described by its first refresh token and revoked when its latest refresh token is
            migrationBuilder.Sql(@"
                INSERT INTO `UsersSessions` (`id_user_session`, `id_user`, `auth_method`, `id_user_external_login`, `created_by_ip`, `revoked_at`, `revoked_by_ip`, `created_at`, `updated_at`)
                SELECT f.`session_id_jwi_refresh`, f.`id_user`, f.`auth_method_jwi_refresh`, NULL, f.`created_by_ip_jwi_refresh`,
                    CASE WHEN l.`is_revoked` = 1 THEN COALESCE(l.`revoked_at`, l.`updated_at`) ELSE NULL END,
                    CASE WHEN l.`is_revoked` = 1 THEN l.`revoked_by_ip_jwi_refresh` ELSE NULL END,
                    f.`created_at`, l.`updated_at`
                FROM (
                    SELECT t.*, ROW_NUMBER() OVER (PARTITION BY t.`session_id_jwi_refresh` ORDER BY t.`created_at` ASC) AS `rn`
                    FROM `JwiRefreshTokens` t
                ) f
                JOIN (
                    SELECT t.*, ROW_NUMBER() OVER (PARTITION BY t.`session_id_jwi_refresh` ORDER BY t.`expires_at` DESC) AS `rn`
                    FROM `JwiRefreshTokens` t
                ) l ON l.`session_id_jwi_refresh` = f.`session_id_jwi_refresh` AND l.`rn` = 1
                WHERE f.`rn` = 1;");

            // TokenType: 0 = Access, 1 = Refresh
            migrationBuilder.Sql(@"
                INSERT INTO `JwtTokens` (`id_jwi`, `id_user_session`, `token_type`, `expires_at`, `revoked_at`, `revoked_reason`, `auth_method`, `created_by_ip`, `revoked_by_ip`, `created_at`, `updated_at`)
                SELECT `id_jwi_refresh`, `session_id_jwi_refresh`, 1, `expires_at`,
                    CASE WHEN `is_revoked` = 1 THEN COALESCE(`revoked_at`, `updated_at`) ELSE NULL END,
                    `revoked_reason_jwi_refresh`, `auth_method_jwi_refresh`, `created_by_ip_jwi_refresh`, `revoked_by_ip_jwi_refresh`, `created_at`, `updated_at`
                FROM `JwiRefreshTokens`;");

            migrationBuilder.Sql(@"
                INSERT INTO `JwtTokens` (`id_jwi`, `id_user_session`, `token_type`, `expires_at`, `revoked_at`, `revoked_reason`, `auth_method`, `created_by_ip`, `revoked_by_ip`, `created_at`, `updated_at`)
                SELECT a.`id_jwi_access`, a.`session_id_jwi_access`, 0, a.`expires_at`,
                    CASE WHEN a.`is_revoked` = 1 THEN COALESCE(a.`revoked_at`, a.`updated_at`) ELSE NULL END,
                    a.`revoked_reason_jwi_access`, a.`auth_method_jwi_access`, a.`created_by_ip_jwi_access`, a.`revoked_by_ip_jwi_access`, a.`created_at`, a.`updated_at`
                FROM `JwiAccessTokens` a
                WHERE EXISTS (SELECT 1 FROM `UsersSessions` s WHERE s.`id_user_session` = a.`session_id_jwi_access`);");

            migrationBuilder.DropTable(
                name: "JwiRefreshTokens");

            migrationBuilder.DropTable(
                name: "JwiAccessTokens");

            migrationBuilder.DropColumn(
                name: "password_user",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "reset_token",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "reset_token_expiration",
                table: "Users");

            migrationBuilder.AddColumn<DateTime>(
                name: "reset_token_sent_at",
                table: "UsersPasswords",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "email_verification_token",
                table: "Users",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "email_verification_token_expiration",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "email_verification_token_sent_at",
                table: "Users",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reset_token_sent_at",
                table: "UsersPasswords");

            migrationBuilder.DropColumn(
                name: "email_verification_token",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "email_verification_token_expiration",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "email_verification_token_sent_at",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "password_user",
                table: "Users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "reset_token",
                table: "Users",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "reset_token_expiration",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            // users without a password (external login only) get an empty hash, they can't log in with a password until they reset it
            migrationBuilder.Sql(@"
                UPDATE `Users` u
                JOIN `UsersPasswords` p ON p.`id_user` = u.`id_user`
                SET u.`password_user` = p.`password_user`, u.`reset_token` = p.`reset_token`, u.`reset_token_expiration` = p.`reset_token_expiration`;");

            migrationBuilder.DropTable(
                name: "JwtTokens");

            migrationBuilder.DropTable(
                name: "UsersPasswords");

            migrationBuilder.DropTable(
                name: "UsersSessions");

            migrationBuilder.DropTable(
                name: "UsersExternalLogins");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "is_verified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "last_login",
                table: "Users");

            // the sessions / tokens are not restored: the users just have to log in again
            migrationBuilder.CreateTable(
                name: "JwiAccessTokens",
                columns: table => new
                {
                    id_jwi_access = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    id_user = table.Column<int>(type: "int", nullable: false),
                    auth_method_jwi_access = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by_ip_jwi_access = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    is_revoked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revoked_by_ip_jwi_access = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revoked_reason_jwi_access = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id_jwi_access = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
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
                    id_jwi_access = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    id_user = table.Column<int>(type: "int", nullable: false),
                    auth_method_jwi_refresh = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by_ip_jwi_refresh = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    is_revoked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revoked_by_ip_jwi_refresh = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revoked_reason_jwi_refresh = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id_jwi_refresh = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
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
    }
}

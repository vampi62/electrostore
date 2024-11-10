using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace electrostore.Migrations
{
    /// <inheritdoc />
    public partial class JWIservice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "id_jwi_access",
                table: "JWIRefreshToken",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_JWIRefreshToken_id_jwi_access",
                table: "JWIRefreshToken",
                column: "id_jwi_access");

            migrationBuilder.AddForeignKey(
                name: "FK_JWIRefreshToken_JWIAccessToken_id_jwi_access",
                table: "JWIRefreshToken",
                column: "id_jwi_access",
                principalTable: "JWIAccessToken",
                principalColumn: "id_jwi_access",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JWIRefreshToken_JWIAccessToken_id_jwi_access",
                table: "JWIRefreshToken");

            migrationBuilder.DropIndex(
                name: "IX_JWIRefreshToken_id_jwi_access",
                table: "JWIRefreshToken");

            migrationBuilder.DropColumn(
                name: "id_jwi_access",
                table: "JWIRefreshToken");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gestaurante.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountActionTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountActionTokens",
                columns: table => new
                {
                    IdAccountActionToken = table.Column<Guid>(type: "uuid", nullable: false),
                    UserType = table.Column<int>(type: "integer", nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountActionTokens", x => x.IdAccountActionToken);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountActionTokens_Purpose_ExpiresAt",
                table: "AccountActionTokens",
                columns: new[] { "Purpose", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountActionTokens_TokenHash",
                table: "AccountActionTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountActionTokens_UserType_Purpose_UserId_ConsumedAt",
                table: "AccountActionTokens",
                columns: new[] { "UserType", "Purpose", "UserId", "ConsumedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountActionTokens");
        }
    }
}

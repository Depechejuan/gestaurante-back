using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gestaurante.Migrations
{
    /// <inheritdoc />
    public partial class AddMesaPublicSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IdMesaPublicSession",
                table: "Pedidos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MesaPublicSessions",
                columns: table => new
                {
                    IdMesaPublicSession = table.Column<Guid>(type: "uuid", nullable: false),
                    IdMesa = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MesaPublicSessions", x => x.IdMesaPublicSession);
                    table.ForeignKey(
                        name: "FK_MesaPublicSessions_Mesas_IdMesa",
                        column: x => x.IdMesa,
                        principalTable: "Mesas",
                        principalColumn: "IdMesa",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_IdMesaPublicSession",
                table: "Pedidos",
                column: "IdMesaPublicSession");

            migrationBuilder.CreateIndex(
                name: "IX_MesaPublicSessions_IdMesa_IsActive_ExpiresAt",
                table: "MesaPublicSessions",
                columns: new[] { "IdMesa", "IsActive", "ExpiresAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_MesaPublicSessions_IdMesaPublicSession",
                table: "Pedidos",
                column: "IdMesaPublicSession",
                principalTable: "MesaPublicSessions",
                principalColumn: "IdMesaPublicSession",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_MesaPublicSessions_IdMesaPublicSession",
                table: "Pedidos");

            migrationBuilder.DropTable(
                name: "MesaPublicSessions");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_IdMesaPublicSession",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "IdMesaPublicSession",
                table: "Pedidos");
        }
    }
}

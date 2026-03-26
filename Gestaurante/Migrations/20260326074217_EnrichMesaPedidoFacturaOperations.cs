using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gestaurante.Migrations
{
    /// <inheritdoc />
    public partial class EnrichMesaPedidoFacturaOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IdFactura",
                table: "Pedidos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IdMesa",
                table: "Pedidos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IdMesa",
                table: "Facturas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "DetallesPedido",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCancelacion",
                table: "DetallesPedido",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_IdFactura",
                table: "Pedidos",
                column: "IdFactura");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_IdMesa",
                table: "Pedidos",
                column: "IdMesa");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_IdMesa",
                table: "Facturas",
                column: "IdMesa");

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_Mesas_IdMesa",
                table: "Facturas",
                column: "IdMesa",
                principalTable: "Mesas",
                principalColumn: "IdMesa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Facturas_IdFactura",
                table: "Pedidos",
                column: "IdFactura",
                principalTable: "Facturas",
                principalColumn: "NumeroFactura",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Mesas_IdMesa",
                table: "Pedidos",
                column: "IdMesa",
                principalTable: "Mesas",
                principalColumn: "IdMesa",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_Mesas_IdMesa",
                table: "Facturas");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Facturas_IdFactura",
                table: "Pedidos");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Mesas_IdMesa",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_IdFactura",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_IdMesa",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_IdMesa",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "IdFactura",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "IdMesa",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "IdMesa",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "DetallesPedido");

            migrationBuilder.DropColumn(
                name: "FechaCancelacion",
                table: "DetallesPedido");
        }
    }
}

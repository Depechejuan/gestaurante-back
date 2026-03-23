using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gestaurante.Migrations
{
    /// <inheritdoc />
    public partial class SyncEmpleadoAuthStateAndImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetallesPedido_Pedidos_PedidoIdPedido",
                table: "DetallesPedido");

            migrationBuilder.DropIndex(
                name: "IX_DetallesPedido_PedidoIdPedido",
                table: "DetallesPedido");

            migrationBuilder.DropColumn(
                name: "PedidoIdPedido",
                table: "DetallesPedido");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "Empleados",
                newName: "ImageURL");

            migrationBuilder.AlterColumn<string>(
                name: "NUSS",
                table: "Empleados",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "DNI",
                table: "Empleados",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(13)",
                oldMaxLength: 13);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Empleados",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_NUSS",
                table: "Empleados",
                column: "NUSS",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPedido_IdPedido",
                table: "DetallesPedido",
                column: "IdPedido");

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesPedido_Pedidos_IdPedido",
                table: "DetallesPedido",
                column: "IdPedido",
                principalTable: "Pedidos",
                principalColumn: "IdPedido",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetallesPedido_Pedidos_IdPedido",
                table: "DetallesPedido");

            migrationBuilder.DropIndex(
                name: "IX_Empleados_NUSS",
                table: "Empleados");

            migrationBuilder.DropIndex(
                name: "IX_DetallesPedido_IdPedido",
                table: "DetallesPedido");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Empleados");

            migrationBuilder.RenameColumn(
                name: "ImageURL",
                table: "Empleados",
                newName: "ImageUrl");

            migrationBuilder.AlterColumn<string>(
                name: "NUSS",
                table: "Empleados",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(13)",
                oldMaxLength: 13);

            migrationBuilder.AlterColumn<string>(
                name: "DNI",
                table: "Empleados",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AddColumn<Guid>(
                name: "PedidoIdPedido",
                table: "DetallesPedido",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPedido_PedidoIdPedido",
                table: "DetallesPedido",
                column: "PedidoIdPedido");

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesPedido_Pedidos_PedidoIdPedido",
                table: "DetallesPedido",
                column: "PedidoIdPedido",
                principalTable: "Pedidos",
                principalColumn: "IdPedido");
        }
    }
}

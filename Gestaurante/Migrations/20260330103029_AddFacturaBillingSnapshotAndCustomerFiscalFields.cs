using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gestaurante.Migrations
{
    /// <inheritdoc />
    public partial class AddFacturaBillingSnapshotAndCustomerFiscalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingCity",
                table: "UsuariosCliente",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BillingPostalCode",
                table: "UsuariosCliente",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BillingProvince",
                table: "UsuariosCliente",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BillingStreet",
                table: "UsuariosCliente",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Cif",
                table: "UsuariosCliente",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Dni",
                table: "UsuariosCliente",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FiscalName",
                table: "UsuariosCliente",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BillingCity",
                table: "Facturas",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "Madrid");

            migrationBuilder.AddColumn<string>(
                name: "BillingDocument",
                table: "Facturas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "00000000X");

            migrationBuilder.AddColumn<string>(
                name: "BillingEmail",
                table: "Facturas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "anonimo@gestaurante.local");

            migrationBuilder.AddColumn<string>(
                name: "BillingName",
                table: "Facturas",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "Cliente anónimo");

            migrationBuilder.AddColumn<string>(
                name: "BillingPhone",
                table: "Facturas",
                type: "character varying(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "600000000");

            migrationBuilder.AddColumn<string>(
                name: "BillingPostalCode",
                table: "Facturas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "28000");

            migrationBuilder.AddColumn<string>(
                name: "BillingProvince",
                table: "Facturas",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "Madrid");

            migrationBuilder.AddColumn<string>(
                name: "BillingStreet",
                table: "Facturas",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Calle Falsa 123");

            migrationBuilder.AddColumn<Guid>(
                name: "IdUsuarioCliente",
                table: "Facturas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_IdUsuarioCliente",
                table: "Facturas",
                column: "IdUsuarioCliente");

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_UsuariosCliente_IdUsuarioCliente",
                table: "Facturas",
                column: "IdUsuarioCliente",
                principalTable: "UsuariosCliente",
                principalColumn: "IdUsuarioCliente",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_UsuariosCliente_IdUsuarioCliente",
                table: "Facturas");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_IdUsuarioCliente",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "BillingCity",
                table: "UsuariosCliente");

            migrationBuilder.DropColumn(
                name: "BillingPostalCode",
                table: "UsuariosCliente");

            migrationBuilder.DropColumn(
                name: "BillingProvince",
                table: "UsuariosCliente");

            migrationBuilder.DropColumn(
                name: "BillingStreet",
                table: "UsuariosCliente");

            migrationBuilder.DropColumn(
                name: "Cif",
                table: "UsuariosCliente");

            migrationBuilder.DropColumn(
                name: "Dni",
                table: "UsuariosCliente");

            migrationBuilder.DropColumn(
                name: "FiscalName",
                table: "UsuariosCliente");

            migrationBuilder.DropColumn(
                name: "BillingCity",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "BillingDocument",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "BillingEmail",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "BillingName",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "BillingPhone",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "BillingPostalCode",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "BillingProvince",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "BillingStreet",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "IdUsuarioCliente",
                table: "Facturas");
        }
    }
}

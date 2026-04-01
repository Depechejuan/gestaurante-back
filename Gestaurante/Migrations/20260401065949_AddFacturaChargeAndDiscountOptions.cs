using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gestaurante.Migrations
{
    /// <inheritdoc />
    public partial class AddFacturaChargeAndDiscountOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CambioEntregado",
                table: "Facturas",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCobro",
                table: "Facturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ImporteEntregado",
                table: "Facturas",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MetodoCobro",
                table: "Facturas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoDescuento",
                table: "Facturas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "ValorDescuento",
                table: "Facturas",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.Sql("""
                UPDATE "Facturas"
                SET "ValorDescuento" = COALESCE("Descuento", 0),
                    "TipoDescuento" = 0
                WHERE COALESCE("Descuento", 0) > 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CambioEntregado",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "FechaCobro",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "ImporteEntregado",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "MetodoCobro",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "TipoDescuento",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "ValorDescuento",
                table: "Facturas");
        }
    }
}

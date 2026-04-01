using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gestaurante.Migrations
{
    /// <inheritdoc />
    public partial class AddFacturaDiscountReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MotivoDescuento",
                table: "Facturas",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MotivoDescuento",
                table: "Facturas");
        }
    }
}

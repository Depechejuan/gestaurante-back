using Gestaurante.Models.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gestaurante.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260331120000_AddPedidoOnlineShippingFee")]
    public partial class AddPedidoOnlineShippingFee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Pedidos"
                ADD COLUMN IF NOT EXISTS "GastosEnvio" numeric(10,2) NOT NULL DEFAULT 0;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GastosEnvio",
                table: "Pedidos");
        }
    }
}

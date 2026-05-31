using Gestaurante.Models.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gestaurante.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260531170000_AddOnlineFulfillmentStates")]
    public partial class AddOnlineFulfillmentStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Pedidos" AS p
                SET "Estado" = CASE
                        WHEN p."TipoEntrega" = 2 THEN 7
                        WHEN p."TipoEntrega" = 1 THEN 8
                        ELSE p."Estado"
                    END,
                    "FechaModificacion" = NOW() AT TIME ZONE 'UTC'
                WHERE p."Estado" = 3
                    AND p."CanalPedido" = 2
                    AND p."TipoEntrega" IN (1, 2)
                    AND p."IdFactura" IS NULL
                    AND EXISTS (
                        SELECT 1
                        FROM "DetallesPedido" AS d
                        WHERE d."IdPedido" = p."IdPedido"
                            AND d."Estado" <> 1
                    )
                    AND NOT EXISTS (
                        SELECT 1
                        FROM "DetallesPedido" AS d
                        WHERE d."IdPedido" = p."IdPedido"
                            AND d."Estado" <> 1
                            AND d."Estado" <> 4
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Pedidos" AS p
                SET "Estado" = 3,
                    "FechaModificacion" = NOW() AT TIME ZONE 'UTC'
                WHERE p."Estado" IN (7, 8)
                    AND p."CanalPedido" = 2
                    AND p."TipoEntrega" IN (1, 2)
                    AND p."IdFactura" IS NULL
                    AND EXISTS (
                        SELECT 1
                        FROM "DetallesPedido" AS d
                        WHERE d."IdPedido" = p."IdPedido"
                            AND d."Estado" <> 1
                    )
                    AND NOT EXISTS (
                        SELECT 1
                        FROM "DetallesPedido" AS d
                        WHERE d."IdPedido" = p."IdPedido"
                            AND d."Estado" <> 1
                            AND d."Estado" <> 4
                    );
                """);
        }
    }
}

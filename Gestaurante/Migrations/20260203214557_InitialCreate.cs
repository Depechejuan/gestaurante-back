using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gestaurante.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    IdCategoria = table.Column<Guid>(type: "uuid", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Descripción de la categoría")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.IdCategoria);
                });

            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    FirstLastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SecondLastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DNI = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    NUSS = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    NumeroMesas = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ingredientes",
                columns: table => new
                {
                    IdIngrediente = table.Column<Guid>(type: "uuid", maxLength: 100, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Alergenico = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Indica si el ingrediente es alergénico"),
                    Disponible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Indica si el ingrediente está disponible para usar"),
                    Imagen = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, defaultValue: "", comment: "URL de la imagen"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredientes", x => x.IdIngrediente);
                });

            migrationBuilder.CreateTable(
                name: "Mesas",
                columns: table => new
                {
                    IdMesa = table.Column<Guid>(type: "uuid", maxLength: 100, nullable: false),
                    Capacidad = table.Column<int>(type: "integer", nullable: false, defaultValue: 4),
                    Estado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Estado de la mesa: true=Disponible, false=Ocupada"),
                    Ubicacion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Interior", comment: "Ubicación física de la mesa en el restaurante")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mesas", x => x.IdMesa);
                });

            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    IdPedido = table.Column<Guid>(type: "uuid", maxLength: 100, nullable: false),
                    FechaPedido = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.IdPedido);
                });

            migrationBuilder.CreateTable(
                name: "Platos",
                columns: table => new
                {
                    IdPlato = table.Column<Guid>(type: "uuid", maxLength: 100, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Nombre del plato"),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Descripción del plato"),
                    Imagen = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "URL de la imagen"),
                    Disponible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Disponibilidad del plato"),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IdCategoria = table.Column<Guid>(type: "uuid", nullable: false),
                    PlatoIdPlato = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Platos", x => x.IdPlato);
                    table.ForeignKey(
                        name: "FK_Platos_Categorias",
                        column: x => x.IdCategoria,
                        principalTable: "Categorias",
                        principalColumn: "IdCategoria",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Platos_Platos_PlatoIdPlato",
                        column: x => x.PlatoIdPlato,
                        principalTable: "Platos",
                        principalColumn: "IdPlato");
                });

            migrationBuilder.CreateTable(
                name: "Facturas",
                columns: table => new
                {
                    NumeroFactura = table.Column<Guid>(type: "uuid", maxLength: 100, nullable: false),
                    PrecioTotal = table.Column<double>(type: "numeric(10,2)", nullable: false, defaultValue: 0.0),
                    Descuento = table.Column<double>(type: "numeric(5,2)", nullable: false, defaultValue: 0.0),
                    Estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FechaFactura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    IdPedido = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facturas", x => x.NumeroFactura);
                    table.ForeignKey(
                        name: "FK_Facturas_Pedidos_IdPedido",
                        column: x => x.IdPedido,
                        principalTable: "Pedidos",
                        principalColumn: "IdPedido",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesPedido",
                columns: table => new
                {
                    IdDetallePedido = table.Column<Guid>(type: "uuid", maxLength: 100, nullable: false),
                    IdPlato = table.Column<Guid>(type: "uuid", nullable: false),
                    IdPedido = table.Column<Guid>(type: "uuid", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    PrecioUnitario = table.Column<double>(type: "numeric(10,2)", nullable: false),
                    PedidoIdPedido = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallePedido", x => x.IdDetallePedido);
                    table.ForeignKey(
                        name: "FK_DetallesPedido_Pedidos_PedidoIdPedido",
                        column: x => x.PedidoIdPedido,
                        principalTable: "Pedidos",
                        principalColumn: "IdPedido");
                    table.ForeignKey(
                        name: "FK_DetallesPedido_Platos_IdPlato",
                        column: x => x.IdPlato,
                        principalTable: "Platos",
                        principalColumn: "IdPlato",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlatoIngrediente",
                columns: table => new
                {
                    IdPlato = table.Column<Guid>(type: "uuid", nullable: false),
                    IdIngrediente = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatoIngrediente", x => new { x.IdPlato, x.IdIngrediente });
                    table.ForeignKey(
                        name: "FK_PlatoIngrediente_Ingredientes_IdIngrediente",
                        column: x => x.IdIngrediente,
                        principalTable: "Ingredientes",
                        principalColumn: "IdIngrediente",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlatoIngrediente_Platos_IdPlato",
                        column: x => x.IdPlato,
                        principalTable: "Platos",
                        principalColumn: "IdPlato",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPedido_IdPlato",
                table: "DetallesPedido",
                column: "IdPlato");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPedido_PedidoIdPedido",
                table: "DetallesPedido",
                column: "PedidoIdPedido");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_DNI",
                table: "Empleados",
                column: "DNI",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_Email",
                table: "Empleados",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_IdPedido",
                table: "Facturas",
                column: "IdPedido");

            migrationBuilder.CreateIndex(
                name: "IX_PlatoIngrediente_IdIngrediente",
                table: "PlatoIngrediente",
                column: "IdIngrediente");

            migrationBuilder.CreateIndex(
                name: "IX_Platos_IdCategoria",
                table: "Platos",
                column: "IdCategoria");

            migrationBuilder.CreateIndex(
                name: "IX_Platos_Nombre",
                table: "Platos",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Platos_PlatoIdPlato",
                table: "Platos",
                column: "PlatoIdPlato");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesPedido");

            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.DropTable(
                name: "Facturas");

            migrationBuilder.DropTable(
                name: "Mesas");

            migrationBuilder.DropTable(
                name: "PlatoIngrediente");

            migrationBuilder.DropTable(
                name: "Pedidos");

            migrationBuilder.DropTable(
                name: "Ingredientes");

            migrationBuilder.DropTable(
                name: "Platos");

            migrationBuilder.DropTable(
                name: "Categorias");
        }
    }
}

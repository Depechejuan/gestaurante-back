using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gestaurante.Migrations
{
    /// <inheritdoc />
    public partial class AddOnlineOrdersCustomers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CanalPedido",
                table: "Pedidos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ClienteDireccionSnapshot",
                table: "Pedidos",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClienteEmail",
                table: "Pedidos",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClienteNombre",
                table: "Pedidos",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClienteTelefono",
                table: "Pedidos",
                type: "character varying(25)",
                maxLength: 25,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EstadoPago",
                table: "Pedidos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "IdUsuarioCliente",
                table: "Pedidos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notas",
                table: "Pedidos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TipoEntrega",
                table: "Pedidos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CanalPedido",
                table: "Facturas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UsuariosCliente",
                columns: table => new
                {
                    IdUsuarioCliente = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LastName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Phone = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EmailVerificado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosCliente", x => x.IdUsuarioCliente);
                });

            migrationBuilder.CreateTable(
                name: "ClienteDirecciones",
                columns: table => new
                {
                    IdClienteDireccion = table.Column<Guid>(type: "uuid", nullable: false),
                    IdUsuarioCliente = table.Column<Guid>(type: "uuid", nullable: false),
                    Alias = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Province = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, defaultValue: ""),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClienteDirecciones", x => x.IdClienteDireccion);
                    table.ForeignKey(
                        name: "FK_ClienteDirecciones_UsuariosCliente_IdUsuarioCliente",
                        column: x => x.IdUsuarioCliente,
                        principalTable: "UsuariosCliente",
                        principalColumn: "IdUsuarioCliente",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClienteEmailVerifications",
                columns: table => new
                {
                    IdClienteEmailVerification = table.Column<Guid>(type: "uuid", nullable: false),
                    IdUsuarioCliente = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClienteEmailVerifications", x => x.IdClienteEmailVerification);
                    table.ForeignKey(
                        name: "FK_ClienteEmailVerifications_UsuariosCliente_IdUsuarioCliente",
                        column: x => x.IdUsuarioCliente,
                        principalTable: "UsuariosCliente",
                        principalColumn: "IdUsuarioCliente",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClienteMetodosPago",
                columns: table => new
                {
                    IdClienteMetodoPago = table.Column<Guid>(type: "uuid", nullable: false),
                    IdUsuarioCliente = table.Column<Guid>(type: "uuid", nullable: false),
                    MockPaymentToken = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Brand = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Last4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    HolderName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ExpMonth = table.Column<int>(type: "integer", nullable: false),
                    ExpYear = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClienteMetodosPago", x => x.IdClienteMetodoPago);
                    table.ForeignKey(
                        name: "FK_ClienteMetodosPago_UsuariosCliente_IdUsuarioCliente",
                        column: x => x.IdUsuarioCliente,
                        principalTable: "UsuariosCliente",
                        principalColumn: "IdUsuarioCliente",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_IdUsuarioCliente",
                table: "Pedidos",
                column: "IdUsuarioCliente");

            migrationBuilder.CreateIndex(
                name: "IX_ClienteDirecciones_IdUsuarioCliente",
                table: "ClienteDirecciones",
                column: "IdUsuarioCliente");

            migrationBuilder.CreateIndex(
                name: "IX_ClienteEmailVerifications_IdUsuarioCliente_ExpiresAt",
                table: "ClienteEmailVerifications",
                columns: new[] { "IdUsuarioCliente", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClienteMetodosPago_IdUsuarioCliente",
                table: "ClienteMetodosPago",
                column: "IdUsuarioCliente");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosCliente_Email",
                table: "UsuariosCliente",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_UsuariosCliente_IdUsuarioCliente",
                table: "Pedidos",
                column: "IdUsuarioCliente",
                principalTable: "UsuariosCliente",
                principalColumn: "IdUsuarioCliente",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_UsuariosCliente_IdUsuarioCliente",
                table: "Pedidos");

            migrationBuilder.DropTable(
                name: "ClienteDirecciones");

            migrationBuilder.DropTable(
                name: "ClienteEmailVerifications");

            migrationBuilder.DropTable(
                name: "ClienteMetodosPago");

            migrationBuilder.DropTable(
                name: "UsuariosCliente");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_IdUsuarioCliente",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "CanalPedido",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "ClienteDireccionSnapshot",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "ClienteEmail",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "ClienteNombre",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "ClienteTelefono",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EstadoPago",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "IdUsuarioCliente",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "Notas",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "TipoEntrega",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "CanalPedido",
                table: "Facturas");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gestaurante.Migrations
{
    /// <inheritdoc />
    public partial class AddFotoUrlToEmpleado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Empleados",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Empleados");
        }
    }
}

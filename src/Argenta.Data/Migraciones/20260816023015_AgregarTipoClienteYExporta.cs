using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Argenta.Data.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarTipoClienteYExporta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Regimen",
                table: "Clientes");

            migrationBuilder.AddColumn<bool>(
                name: "Exporta",
                table: "Clientes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // defaultValue: 1 = Comercial: los clientes existentes (creados antes de
            // que existiera este campo) se convierten a Comercial por defecto.
            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "Clientes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Exporta",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Clientes");

            migrationBuilder.AddColumn<string>(
                name: "Regimen",
                table: "Clientes",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}

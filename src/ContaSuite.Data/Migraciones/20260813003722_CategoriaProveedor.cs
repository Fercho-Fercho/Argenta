using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContaSuite.Data.Migraciones
{
    /// <inheritdoc />
    public partial class CategoriaProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Categoria",
                table: "Proveedores",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Convierte los datos existentes: los proveedores marcados como
            // EsGasolina = 1 pasan a Categoria = 1 (Gasolinera); el resto
            // queda en 0 (Normal), ya establecido por el valor por defecto.
            migrationBuilder.Sql("UPDATE Proveedores SET Categoria = 1 WHERE EsGasolina = 1;");

            // La categoría "Empresa Eléctrica" no existía antes de esta
            // migración, así que ningún proveedor pudo haberla tenido ya
            // marcada: se reclasifica explícitamente por NIT (326445,
            // EMPRESA ELECTRICA DE GUATEMALA SOCIEDAD ANONIMA) el proveedor
            // real conocido de esta categoría, si ya existe en el catálogo.
            migrationBuilder.Sql("UPDATE Proveedores SET Categoria = 2 WHERE Nit = '326445';");

            migrationBuilder.DropColumn(
                name: "EsGasolina",
                table: "Proveedores");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsGasolina",
                table: "Proveedores",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE Proveedores SET EsGasolina = 1 WHERE Categoria = 1;");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Proveedores");
        }
    }
}

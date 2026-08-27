using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Argenta.Data.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarSeleccionFactura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeleccionesFactura",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NitCliente = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Anio = table.Column<int>(type: "INTEGER", nullable: false),
                    Mes = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoLibro = table.Column<int>(type: "INTEGER", nullable: false),
                    IdentificadorFactura = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Incluida = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeleccionesFactura", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeleccionesFactura_NitCliente_Anio_Mes_TipoLibro_IdentificadorFactura",
                table: "SeleccionesFactura",
                columns: new[] { "NitCliente", "Anio", "Mes", "TipoLibro", "IdentificadorFactura" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeleccionesFactura");
        }
    }
}

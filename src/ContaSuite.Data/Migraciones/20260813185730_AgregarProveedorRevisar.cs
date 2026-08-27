using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContaSuite.Data.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarProveedorRevisar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProveedoresRevisar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nit = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Accion = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProveedoresRevisar", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProveedoresRevisar_Nit",
                table: "ProveedoresRevisar",
                column: "Nit",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProveedoresRevisar");
        }
    }
}

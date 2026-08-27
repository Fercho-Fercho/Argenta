using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Argenta.Data.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarEstablecimientos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Establecimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Exporta = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Establecimientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Establecimientos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Establecimientos_ClienteId_Numero",
                table: "Establecimientos",
                columns: new[] { "ClienteId", "Numero" },
                unique: true);

            // Clientes existentes (creados antes de que existiera Establecimiento):
            // se les crea un establecimiento por defecto (Numero = 1) que hereda
            // el Tipo/Exporta que el cliente tenía, para no perder esa configuración.
            migrationBuilder.Sql(
                """
                INSERT INTO Establecimientos (ClienteId, Numero, Nombre, Tipo, Exporta)
                SELECT Id, 1, 'Principal', Tipo, Exporta FROM Clientes;
                """);

            migrationBuilder.DropColumn(
                name: "Exporta",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Clientes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Exporta",
                table: "Clientes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "Clientes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Recupera Tipo/Exporta desde el establecimiento Numero = 1, si existe.
            migrationBuilder.Sql(
                """
                UPDATE Clientes
                SET Tipo = (SELECT Tipo FROM Establecimientos WHERE Establecimientos.ClienteId = Clientes.Id AND Establecimientos.Numero = 1),
                    Exporta = (SELECT Exporta FROM Establecimientos WHERE Establecimientos.ClienteId = Clientes.Id AND Establecimientos.Numero = 1)
                WHERE EXISTS (SELECT 1 FROM Establecimientos WHERE Establecimientos.ClienteId = Clientes.Id AND Establecimientos.Numero = 1);
                """);

            migrationBuilder.DropTable(
                name: "Establecimientos");
        }
    }
}

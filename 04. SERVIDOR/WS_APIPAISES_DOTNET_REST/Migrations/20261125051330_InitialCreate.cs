using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WS_APIPAISES_DOTNET_REST.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Regiones",
                columns: table => new
                {
                    IdRegion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, collation: "Latin1_General_100_CI_AI")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REGIONES", x => x.IdRegion);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Usuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, collation: "Latin1_General_100_CI_AI"),
                    ClaveHash = table.Column<string>(type: "char(64)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Latin1_General_100_CI_AI")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIOS", x => x.IdUsuario);
                });

            migrationBuilder.CreateTable(
                name: "Paises",
                columns: table => new
                {
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Latin1_General_100_CI_AI"),
                    Capital = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Latin1_General_100_CI_AI"),
                    IdRegion = table.Column<int>(type: "int", nullable: false),
                    Poblacion = table.Column<long>(type: "bigint", nullable: false),
                    Moneda = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Latin1_General_100_CI_AI")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAISES", x => x.Nombre);
                    table.CheckConstraint("CK_PAISES_POBLACION", "[Poblacion] >= 0");
                    table.ForeignKey(
                        name: "FK_PAISES_REGIONES",
                        column: x => x.IdRegion,
                        principalTable: "Regiones",
                        principalColumn: "IdRegion",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Regiones",
                columns: new[] { "IdRegion", "Nombre" },
                values: new object[,]
                {
                    { 1, "América" },
                    { 2, "Europa" },
                    { 3, "Asia" },
                    { 4, "África" },
                    { 5, "Oceanía" },
                    { 6, "Antártida" }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "IdUsuario", "ClaveHash", "Nombre", "Usuario" },
                values: new object[,]
                {
                    { 1, "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9", "Administrador", "admin" },
                    { 2, "cb84af049da6d367b89c4565c747db7a50054d15226dd353f36dc6c26bb3fb9e", "Mateo Sosa", "msosa" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PAISES_POBLACION",
                table: "Paises",
                column: "Poblacion");

            migrationBuilder.CreateIndex(
                name: "IX_PAISES_REGION",
                table: "Paises",
                column: "IdRegion");

            migrationBuilder.CreateIndex(
                name: "AK_REGIONES_NOMBRE",
                table: "Regiones",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "AK_USUARIOS_USUARIO",
                table: "Usuarios",
                column: "Usuario",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Paises");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Regiones");
        }
    }
}

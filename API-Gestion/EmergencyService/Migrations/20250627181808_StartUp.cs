using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace EmergencyService.Migrations
{
    /// <inheritdoc />
    public partial class StartUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "emergencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_emergencia = table.Column<string>(type: "text", nullable: false),
                    zona_afectada = table.Column<string>(type: "text", nullable: false),
                    AreaAfectada = table.Column<Polygon>(type: "geometry", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    gravedad = table.Column<string>(type: "text", nullable: false),
                    fechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emergencies", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "emergencies");
        }
    }
}

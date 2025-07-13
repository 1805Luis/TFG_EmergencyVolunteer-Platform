using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssigmentService.Migrations
{
    /// <inheritdoc />
    public partial class StartUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "grupos_voluntarios",
                columns: table => new
                {
                    voluntario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    grupo_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rol = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grupos_voluntarios", x => new { x.voluntario_id, x.grupo_id });
                });

            migrationBuilder.CreateTable(
                name: "tareas_grupos",
                columns: table => new
                {
                    tarea_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    grupo_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fecha_completada = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tareas_grupos", x => new { x.tarea_id, x.grupo_id });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "grupos_voluntarios");

            migrationBuilder.DropTable(
                name: "tareas_grupos");
        }
    }
}

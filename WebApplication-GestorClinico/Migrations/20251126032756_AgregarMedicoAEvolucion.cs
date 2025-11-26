using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication_GestorClinico.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMedicoAEvolucion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MedicoId",
                table: "EvolucionesMedicas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_EvolucionesMedicas_MedicoId",
                table: "EvolucionesMedicas",
                column: "MedicoId");

            migrationBuilder.AddForeignKey(
                name: "FK_EvolucionesMedicas_Medicos_MedicoId",
                table: "EvolucionesMedicas",
                column: "MedicoId",
                principalTable: "Medicos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvolucionesMedicas_Medicos_MedicoId",
                table: "EvolucionesMedicas");

            migrationBuilder.DropIndex(
                name: "IX_EvolucionesMedicas_MedicoId",
                table: "EvolucionesMedicas");

            migrationBuilder.DropColumn(
                name: "MedicoId",
                table: "EvolucionesMedicas");
        }
    }
}

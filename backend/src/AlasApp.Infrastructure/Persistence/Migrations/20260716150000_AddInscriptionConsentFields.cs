using AlasApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AlasApp.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AlasAppDbContext))]
    [Migration("20260716150000_AddInscriptionConsentFields")]
    /// <inheritdoc />
    public partial class AddInscriptionConsentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RiesgosAceptados",
                table: "Inscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsoImagenAceptado",
                table: "Inscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RiesgosAceptados",
                table: "Inscriptions");

            migrationBuilder.DropColumn(
                name: "UsoImagenAceptado",
                table: "Inscriptions");
        }
    }
}

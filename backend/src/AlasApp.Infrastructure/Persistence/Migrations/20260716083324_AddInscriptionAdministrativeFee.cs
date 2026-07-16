using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlasApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInscriptionAdministrativeFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdministrativeFeeUsd",
                table: "Inscriptions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseAmountUsd",
                table: "Inscriptions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdministrativeFeeUsd",
                table: "Inscriptions");

            migrationBuilder.DropColumn(
                name: "BaseAmountUsd",
                table: "Inscriptions");
        }
    }
}

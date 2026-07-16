using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlasApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryMembershipFees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MembresiaAnualUsd",
                table: "Categories",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MembresiaPorEventoUsd",
                table: "Categories",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MembresiaAnualUsd",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "MembresiaPorEventoUsd",
                table: "Categories");
        }
    }
}

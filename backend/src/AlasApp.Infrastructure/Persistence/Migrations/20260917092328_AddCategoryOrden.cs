using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlasApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryOrden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Orden",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Orden",
                table: "Categories");
        }
    }
}

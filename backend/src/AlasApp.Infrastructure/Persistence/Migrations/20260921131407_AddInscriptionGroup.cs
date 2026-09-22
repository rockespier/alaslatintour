using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlasApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInscriptionGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InscriptionGroupId",
                table: "Inscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("UPDATE [Inscriptions] SET [InscriptionGroupId] = NEWID() WHERE [InscriptionGroupId] IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "InscriptionGroupId",
                table: "Inscriptions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inscriptions_InscriptionGroupId",
                table: "Inscriptions",
                column: "InscriptionGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inscriptions_InscriptionGroupId",
                table: "Inscriptions");

            migrationBuilder.DropColumn(
                name: "InscriptionGroupId",
                table: "Inscriptions");
        }
    }
}

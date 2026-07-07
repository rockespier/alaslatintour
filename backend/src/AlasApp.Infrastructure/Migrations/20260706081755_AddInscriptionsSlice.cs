using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlasApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInscriptionsSlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Inscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShirtNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MontoUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstadoAdmin = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EstadoCompetidor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    TransaccionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReglamentoAceptado = table.Column<bool>(type: "bit", nullable: false),
                    InscripcionAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inscriptions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscriptions_Competitors_CompetitorId",
                        column: x => x.CompetitorId,
                        principalTable: "Competitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscriptions_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inscriptions_CategoryId",
                table: "Inscriptions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscriptions_CompetitorId_EventId_CategoryId",
                table: "Inscriptions",
                columns: new[] { "CompetitorId", "EventId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inscriptions_EstadoAdmin",
                table: "Inscriptions",
                column: "EstadoAdmin");

            migrationBuilder.CreateIndex(
                name: "IX_Inscriptions_EventId",
                table: "Inscriptions",
                column: "EventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inscriptions");
        }
    }
}

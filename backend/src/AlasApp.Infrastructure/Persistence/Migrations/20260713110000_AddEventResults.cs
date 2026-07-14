using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlasApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Place = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LigaPoints = table.Column<int>(type: "int", nullable: false),
                    PrizeUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HeatOla1 = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    HeatOla2 = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventResults_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventResults_Competitors_CompetitorId",
                        column: x => x.CompetitorId,
                        principalTable: "Competitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventResults_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventResults_CategoryId",
                table: "EventResults",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_EventResults_CompetitorId",
                table: "EventResults",
                column: "CompetitorId");

            migrationBuilder.CreateIndex(
                name: "IX_EventResults_EventId_CategoryId_CompetitorId",
                table: "EventResults",
                columns: new[] { "EventId", "CategoryId", "CompetitorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventResults_EventId_CategoryId_Place",
                table: "EventResults",
                columns: new[] { "EventId", "CategoryId", "Place" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventResults");
        }
    }
}

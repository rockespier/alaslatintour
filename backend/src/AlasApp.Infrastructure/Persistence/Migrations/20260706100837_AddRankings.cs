using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlasApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRankings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RankingSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CircuitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    CachedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankingSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RankingSnapshots_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RankingSnapshots_Circuits_CircuitId",
                        column: x => x.CircuitId,
                        principalTable: "Circuits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RankingSnapshotEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RankingSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Events = table.Column<int>(type: "int", nullable: false),
                    Variation = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankingSnapshotEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RankingSnapshotEntries_RankingSnapshots_RankingSnapshotId",
                        column: x => x.RankingSnapshotId,
                        principalTable: "RankingSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RankingSnapshotEntries_RankingSnapshotId_Position",
                table: "RankingSnapshotEntries",
                columns: new[] { "RankingSnapshotId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RankingSnapshots_CategoryId_Year_CachedAtUtc",
                table: "RankingSnapshots",
                columns: new[] { "CategoryId", "Year", "CachedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RankingSnapshots_CircuitId_CategoryId_Year",
                table: "RankingSnapshots",
                columns: new[] { "CircuitId", "CategoryId", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RankingSnapshotEntries");

            migrationBuilder.DropTable(
                name: "RankingSnapshots");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlasApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AgeRestriction = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinAge = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxAge = table.Column<int>(type: "INTEGER", nullable: true),
                    SuccessorCategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_SuccessorCategoryId",
                        column: x => x.SuccessorCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Circuits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Temporada = table.Column<int>(type: "INTEGER", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Region = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Modalidad = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SurfScoresCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LastSyncAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Circuits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategoryTariffs",
                columns: table => new
                {
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StarLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Usd = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Cop = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryTariffs", x => new { x.CategoryId, x.StarLevel });
                    table.ForeignKey(
                        name: "FK_CategoryTariffs_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CircuitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FechaInicio = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Pais = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Ciudad = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Playa = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Stars = table.Column<int>(type: "INTEGER", nullable: false),
                    CapacidadMaxima = table.Column<int>(type: "INTEGER", nullable: false),
                    PrizeAmountUsd = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SurfScoresCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AccessType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UseCircuitTariffs = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_Circuits_CircuitId",
                        column: x => x.CircuitId,
                        principalTable: "Circuits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventCategories",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomTariffUsd = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    CustomTariffCop = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Capacidad = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCategories", x => new { x.EventId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_EventCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventCategories_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Nombre",
                table: "Categories",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Status",
                table: "Categories",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_SuccessorCategoryId",
                table: "Categories",
                column: "SuccessorCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Circuits_Estado",
                table: "Circuits",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Circuits_Temporada",
                table: "Circuits",
                column: "Temporada");

            migrationBuilder.CreateIndex(
                name: "IX_EventCategories_CategoryId",
                table: "EventCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CircuitId",
                table: "Events",
                column: "CircuitId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Estado",
                table: "Events",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Events_FechaInicio",
                table: "Events",
                column: "FechaInicio");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Pais",
                table: "Events",
                column: "Pais");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryTariffs");

            migrationBuilder.DropTable(
                name: "EventCategories");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Circuits");
        }
    }
}

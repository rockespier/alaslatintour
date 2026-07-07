using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlasApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitorsPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: these columns are now created in InitialCreate for clean bootstrap from empty databases.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op paired with Up().
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class OptimizeMeetingSchedulerIndexes : Migration
{
    private static readonly string[] IndexColumns = ["start_time", "end_time"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_meetings_start_time_end_time",
            schema: "public",
            table: "meetings",
            columns: IndexColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_meetings_start_time_end_time",
            schema: "public",
            table: "meetings");
    }
}
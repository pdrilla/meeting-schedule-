using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;
/// <inheritdoc />
public partial class AddMeetingSchedulerTables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "meeting_users",
            schema: "public",
            columns: static table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: static table => table.PrimaryKey("pk_meeting_users", static x => x.id));

        migrationBuilder.CreateTable(
            name: "meetings",
            schema: "public",
            columns: static table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                participant_ids = table.Column<string>(type: "text", nullable: false),
                start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: static table => table.PrimaryKey("pk_meetings", static x => x.id));

        migrationBuilder.CreateIndex(
            name: "ix_meetings_end_time",
            schema: "public",
            table: "meetings",
            column: "end_time");

        migrationBuilder.CreateIndex(
            name: "ix_meetings_start_time",
            schema: "public",
            table: "meetings",
            column: "start_time");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "meetings",
            schema: "public");

        migrationBuilder.DropTable(
            name: "meeting_users",
            schema: "public");
    }
}
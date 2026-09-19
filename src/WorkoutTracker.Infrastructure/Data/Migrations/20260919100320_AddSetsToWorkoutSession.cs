using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSetsToWorkoutSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sets",
                schema: "workout_tracker",
                table: "workout_sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_workout_session_sets_allowed",
                schema: "workout_tracker",
                table: "workout_sessions",
                sql: "sets IS NULL OR sets IN (3, 5)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_workout_session_sets_allowed",
                schema: "workout_tracker",
                table: "workout_sessions");

            migrationBuilder.DropColumn(
                name: "sets",
                schema: "workout_tracker",
                table: "workout_sessions");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOverallEffortToWorkoutSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "overall_effort",
                schema: "workout_tracker",
                table: "workout_sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_workout_session_overall_effort_range",
                schema: "workout_tracker",
                table: "workout_sessions",
                sql: "overall_effort IS NULL OR (overall_effort >= 1 AND overall_effort <= 10)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_workout_session_overall_effort_range",
                schema: "workout_tracker",
                table: "workout_sessions");

            migrationBuilder.DropColumn(
                name: "overall_effort",
                schema: "workout_tracker",
                table: "workout_sessions");
        }
    }
}

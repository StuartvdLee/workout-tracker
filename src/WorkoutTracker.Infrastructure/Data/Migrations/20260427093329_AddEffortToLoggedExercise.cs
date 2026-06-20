using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEffortToLoggedExercise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "effort",
                schema: "workout_tracker",
                table: "logged_exercises",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_logged_exercise_effort_range",
                schema: "workout_tracker",
                table: "logged_exercises",
                sql: "effort IS NULL OR (effort >= 1 AND effort <= 10)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_logged_exercise_effort_range",
                schema: "workout_tracker",
                table: "logged_exercises");

            migrationBuilder.DropColumn(
                name: "effort",
                schema: "workout_tracker",
                table: "logged_exercises");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutTemplateAndSessionEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ix_exercises_name_lower",
                schema: "workout_tracker",
                table: "exercises",
                newName: "ix_exercises_name");

            migrationBuilder.CreateTable(
                name: "planned_workouts",
                schema: "workout_tracker",
                columns: table => new
                {
                    planned_workout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_planned_workouts", x => x.planned_workout_id);
                    table.CheckConstraint("ck_planned_workouts_name_length", "length(name) <= 150");
                });

            migrationBuilder.CreateTable(
                name: "planned_workout_exercises",
                schema: "workout_tracker",
                columns: table => new
                {
                    planned_workout_exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    planned_workout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    target_reps = table.Column<string>(type: "text", nullable: true),
                    target_weight = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_planned_workout_exercises", x => x.planned_workout_exercise_id);
                    table.ForeignKey(
                        name: "fk_planned_workout_exercises_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalSchema: "workout_tracker",
                        principalTable: "exercises",
                        principalColumn: "exercise_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_planned_workout_exercises_planned_workouts_planned_workout_",
                        column: x => x.planned_workout_id,
                        principalSchema: "workout_tracker",
                        principalTable: "planned_workouts",
                        principalColumn: "planned_workout_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_sessions",
                schema: "workout_tracker",
                columns: table => new
                {
                    workout_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    planned_workout_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workout_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workout_sessions", x => x.workout_session_id);
                    table.ForeignKey(
                        name: "fk_workout_sessions_planned_workouts_planned_workout_id",
                        column: x => x.planned_workout_id,
                        principalSchema: "workout_tracker",
                        principalTable: "planned_workouts",
                        principalColumn: "planned_workout_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "logged_exercises",
                schema: "workout_tracker",
                columns: table => new
                {
                    logged_exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    logged_reps = table.Column<int>(type: "integer", nullable: true),
                    logged_weight = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_logged_exercises", x => x.logged_exercise_id);
                    table.ForeignKey(
                        name: "fk_logged_exercises_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalSchema: "workout_tracker",
                        principalTable: "exercises",
                        principalColumn: "exercise_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_logged_exercises_workout_sessions_workout_session_id",
                        column: x => x.workout_session_id,
                        principalSchema: "workout_tracker",
                        principalTable: "workout_sessions",
                        principalColumn: "workout_session_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_logged_exercises_exercise_id",
                schema: "workout_tracker",
                table: "logged_exercises",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "ix_logged_exercises_workout_session_id",
                schema: "workout_tracker",
                table: "logged_exercises",
                column: "workout_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_planned_workout_exercises_exercise_id",
                schema: "workout_tracker",
                table: "planned_workout_exercises",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "ix_planned_workout_exercises_planned_workout_id_exercise_id",
                schema: "workout_tracker",
                table: "planned_workout_exercises",
                columns: ["planned_workout_id", "exercise_id"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_planned_workouts_name",
                schema: "workout_tracker",
                table: "planned_workouts",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workout_sessions_planned_workout_id",
                schema: "workout_tracker",
                table: "workout_sessions",
                column: "planned_workout_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "logged_exercises",
                schema: "workout_tracker");

            migrationBuilder.DropTable(
                name: "planned_workout_exercises",
                schema: "workout_tracker");

            migrationBuilder.DropTable(
                name: "workout_sessions",
                schema: "workout_tracker");

            migrationBuilder.DropTable(
                name: "planned_workouts",
                schema: "workout_tracker");

            migrationBuilder.RenameIndex(
                name: "ix_exercises_name",
                schema: "workout_tracker",
                table: "exercises",
                newName: "ix_exercises_name_lower");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workout_tracker");

            migrationBuilder.CreateTable(
                name: "exercises",
                schema: "workout_tracker",
                columns: table => new
                {
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercises", x => x.exercise_id);
                });

            migrationBuilder.CreateTable(
                name: "workout_types",
                schema: "workout_tracker",
                columns: table => new
                {
                    workout_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workout_types", x => x.workout_type_id);
                });

            migrationBuilder.CreateTable(
                name: "workouts",
                schema: "workout_tracker",
                columns: table => new
                {
                    workout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workouts", x => x.workout_id);
                    table.ForeignKey(
                        name: "fk_workouts_workout_types_workout_type_id",
                        column: x => x.workout_type_id,
                        principalSchema: "workout_tracker",
                        principalTable: "workout_types",
                        principalColumn: "workout_type_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_exercises",
                schema: "workout_tracker",
                columns: table => new
                {
                    workout_exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sets = table.Column<int>(type: "integer", nullable: true),
                    reps = table.Column<int>(type: "integer", nullable: true),
                    weight = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workout_exercises", x => x.workout_exercise_id);
                    table.ForeignKey(
                        name: "fk_workout_exercises_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalSchema: "workout_tracker",
                        principalTable: "exercises",
                        principalColumn: "exercise_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_workout_exercises_workouts_workout_id",
                        column: x => x.workout_id,
                        principalSchema: "workout_tracker",
                        principalTable: "workouts",
                        principalColumn: "workout_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workout_exercises_exercise_id",
                schema: "workout_tracker",
                table: "workout_exercises",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "ix_workout_exercises_workout_id",
                schema: "workout_tracker",
                table: "workout_exercises",
                column: "workout_id");

            migrationBuilder.CreateIndex(
                name: "ix_workouts_workout_type_id",
                schema: "workout_tracker",
                table: "workouts",
                column: "workout_type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workout_exercises",
                schema: "workout_tracker");

            migrationBuilder.DropTable(
                name: "exercises",
                schema: "workout_tracker");

            migrationBuilder.DropTable(
                name: "workouts",
                schema: "workout_tracker");

            migrationBuilder.DropTable(
                name: "workout_types",
                schema: "workout_tracker");
        }
    }
}

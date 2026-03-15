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
                name: "Exercises",
                schema: "workout_tracker",
                columns: table => new
                {
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.ExerciseId);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutTypes",
                schema: "workout_tracker",
                columns: table => new
                {
                    WorkoutTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutTypes", x => x.WorkoutTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Workouts",
                schema: "workout_tracker",
                columns: table => new
                {
                    WorkoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workouts", x => x.WorkoutId);
                    table.ForeignKey(
                        name: "FK_Workouts_WorkoutTypes_WorkoutTypeId",
                        column: x => x.WorkoutTypeId,
                        principalSchema: "workout_tracker",
                        principalTable: "WorkoutTypes",
                        principalColumn: "WorkoutTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutExercises",
                schema: "workout_tracker",
                columns: table => new
                {
                    WorkoutExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sets = table.Column<int>(type: "integer", nullable: true),
                    Reps = table.Column<int>(type: "integer", nullable: true),
                    Weight = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutExercises", x => x.WorkoutExerciseId);
                    table.ForeignKey(
                        name: "FK_WorkoutExercises_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalSchema: "workout_tracker",
                        principalTable: "Exercises",
                        principalColumn: "ExerciseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutExercises_Workouts_WorkoutId",
                        column: x => x.WorkoutId,
                        principalSchema: "workout_tracker",
                        principalTable: "Workouts",
                        principalColumn: "WorkoutId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_ExerciseId",
                schema: "workout_tracker",
                table: "WorkoutExercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_WorkoutId",
                schema: "workout_tracker",
                table: "WorkoutExercises",
                column: "WorkoutId");

            migrationBuilder.CreateIndex(
                name: "IX_Workouts_WorkoutTypeId",
                schema: "workout_tracker",
                table: "Workouts",
                column: "WorkoutTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkoutExercises",
                schema: "workout_tracker");

            migrationBuilder.DropTable(
                name: "Exercises",
                schema: "workout_tracker");

            migrationBuilder.DropTable(
                name: "Workouts",
                schema: "workout_tracker");

            migrationBuilder.DropTable(
                name: "WorkoutTypes",
                schema: "workout_tracker");
        }
    }
}

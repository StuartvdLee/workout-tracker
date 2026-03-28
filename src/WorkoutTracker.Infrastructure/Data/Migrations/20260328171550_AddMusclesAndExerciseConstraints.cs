using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Prefer 'static readonly' fields over constant array arguments

namespace WorkoutTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMusclesAndExerciseConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "workout_tracker",
                table: "exercises",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "muscles",
                schema: "workout_tracker",
                columns: table => new
                {
                    muscle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_muscles", x => x.muscle_id);
                });

            migrationBuilder.CreateTable(
                name: "exercise_muscles",
                schema: "workout_tracker",
                columns: table => new
                {
                    exercise_muscle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    muscle_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercise_muscles", x => x.exercise_muscle_id);
                    table.ForeignKey(
                        name: "fk_exercise_muscles_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalSchema: "workout_tracker",
                        principalTable: "exercises",
                        principalColumn: "exercise_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_exercise_muscles_muscles_muscle_id",
                        column: x => x.muscle_id,
                        principalSchema: "workout_tracker",
                        principalTable: "muscles",
                        principalColumn: "muscle_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "workout_tracker",
                table: "muscles",
                columns: new[] { "muscle_id", "name" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-0000-0000-000000000001"), "Back" },
                    { new Guid("a1000000-0000-0000-0000-000000000002"), "Biceps" },
                    { new Guid("a1000000-0000-0000-0000-000000000003"), "Calves" },
                    { new Guid("a1000000-0000-0000-0000-000000000004"), "Chest" },
                    { new Guid("a1000000-0000-0000-0000-000000000005"), "Core" },
                    { new Guid("a1000000-0000-0000-0000-000000000006"), "Forearms" },
                    { new Guid("a1000000-0000-0000-0000-000000000007"), "Glutes" },
                    { new Guid("a1000000-0000-0000-0000-000000000008"), "Hamstrings" },
                    { new Guid("a1000000-0000-0000-0000-000000000009"), "Quads" },
                    { new Guid("a1000000-0000-0000-0000-00000000000a"), "Shoulders" },
                    { new Guid("a1000000-0000-0000-0000-00000000000b"), "Triceps" }
                });

            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ix_exercises_name_lower
                  ON workout_tracker.exercises (LOWER(name));");

            migrationBuilder.AddCheckConstraint(
                name: "ck_exercises_name_length",
                schema: "workout_tracker",
                table: "exercises",
                sql: "length(name) <= 150");

            migrationBuilder.CreateIndex(
                name: "ix_exercise_muscles_exercise_id_muscle_id",
                schema: "workout_tracker",
                table: "exercise_muscles",
                columns: new[] { "exercise_id", "muscle_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exercise_muscles_muscle_id",
                schema: "workout_tracker",
                table: "exercise_muscles",
                column: "muscle_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exercise_muscles",
                schema: "workout_tracker");

            migrationBuilder.DropTable(
                name: "muscles",
                schema: "workout_tracker");

            migrationBuilder.Sql(
                @"DROP INDEX IF EXISTS workout_tracker.ix_exercises_name_lower;");

            migrationBuilder.DropCheckConstraint(
                name: "ck_exercises_name_length",
                schema: "workout_tracker",
                table: "exercises");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "workout_tracker",
                table: "exercises",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);
        }
    }
}

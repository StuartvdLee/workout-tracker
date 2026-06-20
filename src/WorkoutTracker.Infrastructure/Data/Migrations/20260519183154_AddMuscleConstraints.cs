using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMuscleConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "workout_tracker",
                table: "muscles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX IF NOT EXISTS ix_muscles_name
                  ON workout_tracker.muscles (LOWER(name));");

            migrationBuilder.Sql(
                @"DO $$
                  BEGIN
                      IF NOT EXISTS (
                          SELECT 1
                          FROM pg_constraint
                          WHERE conname = 'ck_muscles_name_length'
                      ) THEN
                          ALTER TABLE workout_tracker.muscles
                          ADD CONSTRAINT ck_muscles_name_length CHECK (length(name) <= 100);
                      END IF;
                  END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DROP INDEX IF EXISTS workout_tracker.ix_muscles_name;");

            migrationBuilder.Sql(
                @"ALTER TABLE workout_tracker.muscles
                  DROP CONSTRAINT IF EXISTS ck_muscles_name_length;");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "workout_tracker",
                table: "muscles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}

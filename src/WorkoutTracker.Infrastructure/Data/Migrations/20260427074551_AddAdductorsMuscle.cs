using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // Prefer 'static readonly' fields over constant array arguments

namespace WorkoutTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdductorsMuscle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "workout_tracker",
                table: "muscles",
                columns: new[] { "muscle_id", "name" },
                values: new object[] { new Guid("a1000000-0000-0000-0000-00000000000c"), "Adductors" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "workout_tracker",
                table: "muscles",
                keyColumn: "muscle_id",
                keyValue: new Guid("a1000000-0000-0000-0000-00000000000c"));
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProtectCompletionFingerprintsAndFenceRejections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "request_fingerprint",
                schema: "workout_tracker",
                table: "workout_completion_receipts",
                type: "character varying(67)",
                maxLength: 67,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.CreateTable(
                name: "workout_completion_rejections",
                schema: "workout_tracker",
                columns: table => new
                {
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_fingerprint = table.Column<string>(type: "character varying(67)", maxLength: 67, nullable: false),
                    error = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workout_completion_rejections", x => new { x.operation_id, x.request_fingerprint });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workout_completion_rejections",
                schema: "workout_tracker");

            migrationBuilder.AlterColumn<string>(
                name: "request_fingerprint",
                schema: "workout_tracker",
                table: "workout_completion_receipts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(67)",
                oldMaxLength: 67);
        }
    }
}

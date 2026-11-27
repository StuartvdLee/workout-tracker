using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UseUnkeyedCompletionFingerprints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS completion_receipt_key_required ON workout_tracker.workout_completion_receipts;
                DROP TRIGGER IF EXISTS completion_rejection_key_required ON workout_tracker.workout_completion_rejections;
                DROP FUNCTION IF EXISTS workout_tracker.require_completion_key_verifier();
                DROP TABLE IF EXISTS workout_tracker.workout_completion_key_verifiers;
                ALTER TABLE workout_tracker.workout_completion_receipts
                    ADD CONSTRAINT ck_completion_receipt_fingerprint_version
                    CHECK (request_fingerprint LIKE 's1:%') NOT VALID;
                ALTER TABLE workout_tracker.workout_completion_rejections
                    ADD CONSTRAINT ck_completion_rejection_fingerprint_version
                    CHECK (request_fingerprint LIKE 's1:%') NOT VALID;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Rolling back completion fingerprint versions could bypass retained rejection fences.");
        }
    }
}

# History Load p95 Measurement Workflow

1. Seed at least 1,000 exercise entries for a single normalized exercise.
2. Request `GET /api/exercises/{exerciseName}/history?page=1&pageSize=25` repeatedly.
3. Collect `X-Request-Duration-Ms` values from responses.
4. Compute p95 and compare with target `< 3000ms`.

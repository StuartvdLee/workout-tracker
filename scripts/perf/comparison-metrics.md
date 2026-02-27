# Comparison Response p95 Measurement Workflow

1. Ensure at least two prior entries exist for one exercise.
2. Request `GET /api/entries/{entryId}/comparison` for representative entries.
3. Collect `X-Request-Duration-Ms` values.
4. Compute p95 and compare with target `< 1000ms`.

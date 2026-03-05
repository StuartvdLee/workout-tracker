# Save Entry p95 Measurement Workflow

1. Start backend and frontend using quickstart steps.
2. Seed at least 100 exercise entry create requests.
3. Capture `X-Request-Duration-Ms` for `POST /api/sessions/{sessionId}/entries`.
4. Compute p95 latency from captured durations.
5. Record result and compare against target `< 2000ms`.

## Simplified Homepage Session Start Evidence (US1/US2)

- Measure homepage initial render to visible title/dropdown/button and verify p95 `<= 1000ms`.
- Measure successful `POST /api/sessions` latency for valid workout type and verify p95 `<= 2000ms`.
- Measure invalid start-session submissions (missing workout type) and confirm validation response latency
	does not regress by more than 20% versus baseline.
- Record measurement timestamp, environment, sample size, and p95 values in PR evidence.

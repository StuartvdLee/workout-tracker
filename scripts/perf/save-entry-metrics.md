# Save Entry p95 Measurement Workflow

1. Start backend and frontend using quickstart steps.
2. Seed at least 100 exercise entry create requests.
3. Capture `X-Request-Duration-Ms` for `POST /api/sessions/{sessionId}/entries`.
4. Compute p95 latency from captured durations.
5. Record result and compare against target `< 2000ms`.

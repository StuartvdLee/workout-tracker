# Data Model: Workout Statistics Dashboard

No new tables, fields, migration, or persisted statistics. Source data is read from existing completed history; the response and UI state are derived.

## Reporting period

| Field | Rule |
|---|---|
| kind | `week`, `month`, or `year` |
| localStart | Inclusive local calendar date: Monday, first of month, or January 1 |
| localEndExclusive | Local start of next adjacent period; displayed end is preceding local date |
| startUtc, endUtc | UTC instants obtained independently from local boundaries; filter `startUtc <= CompletedAt < endUtc` |

Initialize at the current week. Switching kind selects the current period of that kind; Previous moves one period back; Next moves one forward only up to current; Current resets to current of selected kind. Local calendar operations must work through DST and year changes.

## Completed workout (existing)

`WorkoutSession` has a stable ID, nullable integer `OverallEffort`, and logged exercises; its completion instant is EF shadow property `CompletedAt`. A row in the selected instant range is one completed workout. No duration, reps or sets metric is inferred. Deletion or edits change subsequent derived results.

## Exercise activity (derived)

Group by stable exercise ID, with a safely rendered existing exercise name or a clearly labeled fallback if unavailable. Count each `(session ID, exercise ID)` at most once, even if duplicate logged exercise entries occur. A period with workouts but no identifiable exercise activity reports unavailable rather than a fabricated exercise count. Sorting is deterministic by display name then ID.

## Effort observation (derived)

At most one observation per workout, sourced from `OverallEffort`; it has completion instant, stable session ID for tie-breaking, and integer value 1–10. Null, malformed, or out-of-range values are omitted without affecting workout or exercise counts. Order by instant then ID. Contributor count equals observation count. An empty observation collection for a nonempty period means effort unavailable, not zero.

## Period statistic and UI state

The response contains the workout count, exercise activity and ordered effort observations plus contributor count. A zero workout count selects the empty-period state; a positive count with no observations selects the effort-unavailable state. Partial valid data displays the exact contributor count.

Client state consists of kind, local period boundaries, request identity, and `loading`, `ready`, `empty`, or `error`. On navigation initiate a new read; only the latest request may update the display. Retry repeats the selected period. Never retain a prior period's statistics as if they were the newly selected result.

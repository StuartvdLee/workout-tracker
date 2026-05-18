import { navigate } from "../router.js";

interface SessionExerciseWithPrevious {
  readonly loggedExerciseId: string;
  readonly exerciseName: string;
  readonly loggedWeight: string | null;
  readonly effort: number | null;
  readonly previousWeight: string | null;
  readonly previousEffort: number | null;
}

interface SessionDetailWithPrevious {
  readonly workoutSessionId: string;
  readonly plannedWorkoutId: string | null;
  readonly workoutName: string | null;
  readonly completedAt: string;
  readonly exercises: SessionExerciseWithPrevious[];
}

const detailDateFormatter = new Intl.DateTimeFormat("en-GB", {
  day: "numeric",
  month: "long",
  year: "numeric",
});

const detailTimeFormatter = new Intl.DateTimeFormat("en-US", {
  hour: "numeric",
  minute: "2-digit",
  hour12: true,
});

export async function render(container: HTMLElement): Promise<void> {
  container.innerHTML = `
    <div class="session-detail">
      <div class="session-detail__loading" id="session-detail-loading">Loading...</div>
      <div id="session-detail-content" style="display:none;"></div>
      <div class="session-detail__error" id="session-detail-error" style="display:none;"></div>
    </div>
  `;

  const params = new URLSearchParams(window.location.search);
  const sessionId = params.get("id");

  if (!sessionId) {
    navigate("/history");
    return;
  }

  await loadSessionDetail(sessionId);
}

async function loadSessionDetail(sessionId: string): Promise<void> {
  const loadingEl = document.getElementById("session-detail-loading") as HTMLElement | null;
  const contentEl = document.getElementById("session-detail-content") as HTMLElement | null;
  const errorEl = document.getElementById("session-detail-error") as HTMLElement | null;

  try {
    const response = await fetch(`/api/sessions/${encodeURIComponent(sessionId)}`);

    if (response.status === 404) {
      if (loadingEl) loadingEl.style.display = "none";
      if (errorEl) {
        errorEl.textContent = "Session not found.";
        errorEl.style.display = "";
      }
      return;
    }

    if (!response.ok) {
      throw new Error(`Failed to load session: ${response.status}`);
    }

    const session: SessionDetailWithPrevious = await response.json();

    if (loadingEl) loadingEl.style.display = "none";
    if (contentEl) {
      contentEl.innerHTML = renderDetail(session);
      contentEl.style.display = "";
      contentEl.querySelector<HTMLButtonElement>(".session-detail__back")?.addEventListener("click", () => {
        navigate("/history");
      });
    }
  } catch {
    if (loadingEl) loadingEl.style.display = "none";
    if (errorEl) {
      errorEl.textContent = "Failed to load session details.";
      errorEl.style.display = "";
    }
  }
}

function formatDate(isoDate: string): string {
  const date = new Date(isoDate);
  return `${detailDateFormatter.format(date)} · ${detailTimeFormatter.format(date)}`;
}

function renderDetail(session: SessionDetailWithPrevious): string {
  const workoutName = session.workoutName ?? "Workout";
  const dateStr = formatDate(session.completedAt);

  const rows =
    session.exercises.length === 0
      ? `<tr><td class="session-detail__empty-cell" colspan="5">No exercises logged</td></tr>`
      : session.exercises
          .map((ex) => {
            const weight = ex.loggedWeight !== null ? escapeHtml(ex.loggedWeight) : `<span class="session-detail__no-data">—</span>`;
            const prevWeight = ex.previousWeight !== null ? escapeHtml(ex.previousWeight) : `<span class="session-detail__no-data">—</span>`;
            const effort = ex.effort !== null ? `${ex.effort}` : `<span class="session-detail__no-data">—</span>`;
            const prevEffort = ex.previousEffort !== null ? `${ex.previousEffort}` : `<span class="session-detail__no-data">—</span>`;

            return `
            <tr class="session-detail__row">
              <td class="session-detail__cell session-detail__cell--exercise">${escapeHtml(ex.exerciseName)}</td>
              <td class="session-detail__cell">${weight}</td>
              <td class="session-detail__cell session-detail__cell--prev">${prevWeight}</td>
              <td class="session-detail__cell">${effort}</td>
              <td class="session-detail__cell session-detail__cell--prev">${prevEffort}</td>
            </tr>`;
          })
          .join("");

  return `
    <div class="session-detail__header">
      <button class="session-detail__back" type="button" aria-label="Back to history">← Back</button>
      <div class="session-detail__title-group">
        <h1 class="session-detail__title">${escapeHtml(workoutName)}</h1>
        <span class="session-detail__date">${escapeHtml(dateStr)}</span>
      </div>
    </div>
    <div class="session-detail__table-wrapper">
      <table class="session-detail__table" aria-label="Session exercises">
        <thead>
          <tr class="session-detail__head-row">
            <th class="session-detail__th" scope="col">Exercise</th>
            <th class="session-detail__th" scope="col">Weight</th>
            <th class="session-detail__th session-detail__th--prev" scope="col">Prev. Weight</th>
            <th class="session-detail__th" scope="col">Effort</th>
            <th class="session-detail__th session-detail__th--prev" scope="col">Prev. Effort</th>
          </tr>
        </thead>
        <tbody>
          ${rows}
        </tbody>
      </table>
    </div>`;
}

function escapeHtml(text: string): string {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

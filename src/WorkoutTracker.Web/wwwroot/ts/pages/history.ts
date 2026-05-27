import { navigate } from "../router.js";

interface WorkoutSession {
  readonly workoutSessionId: string;
  readonly plannedWorkoutId: string | null;
  readonly workoutName: string;
  readonly completedAt: string;
  readonly loggedExercises: { exerciseId: string }[];
}

const historyDateFormatter = new Intl.DateTimeFormat("en-GB", {
  day: "numeric",
  month: "long",
  year: "numeric",
});

const historyTimeFormatter = new Intl.DateTimeFormat("en-US", {
  hour: "numeric",
  minute: "2-digit",
  hour12: true,
});

export async function render(container: HTMLElement): Promise<void> {
  const params = new URLSearchParams(window.location.search);
  const showDeletedBanner = params.get("deleted") === "1";

  if (showDeletedBanner) {
    history.replaceState(null, "", "/history");
  }

  container.innerHTML = `
    <div class="history-page">
      <h1 class="history-page__title">Workout History</h1>
      ${showDeletedBanner ? `<p class="history-page__banner" role="status">Session deleted.</p>` : ""}
      <div class="history-page__loading" id="history-loading">Loading...</div>
      <div class="history-page__empty" id="history-empty" style="display:none;">
        No workouts logged yet. Complete your first workout and it will appear here!
      </div>
      <div id="history-list"></div>
    </div>
  `;

  await loadSessions();
}

async function loadSessions(): Promise<void> {
  const loadingEl = document.getElementById("history-loading") as HTMLElement | null;
  const emptyEl = document.getElementById("history-empty") as HTMLElement | null;
  const listEl = document.getElementById("history-list") as HTMLElement | null;

  try {
    const response = await fetch("/api/sessions");
    if (!response.ok) {
      throw new Error(`Failed to load sessions: ${response.status}`);
    }

    const sessions: WorkoutSession[] = await response.json();

    if (loadingEl) loadingEl.style.display = "none";

    if (sessions.length === 0) {
      if (emptyEl) emptyEl.style.display = "";
      return;
    }

    if (listEl) {
      renderSessions(sessions, listEl);
    }
  } catch {
    if (loadingEl) loadingEl.textContent = "Failed to load workout history.";
  }
}

function formatDate(isoDate: string): string {
  const datePart = historyDateFormatter.format(new Date(isoDate));
  return `${datePart} · ${formatTime(isoDate)}`;
}

function formatTime(isoDate: string): string {
  return historyTimeFormatter.format(new Date(isoDate));
}

function renderSessions(sessions: WorkoutSession[], container: HTMLElement): void {
  container.innerHTML = sessions.map((s) => renderSession(s)).join("");
  container.querySelectorAll<HTMLButtonElement>(".history-session__header").forEach((btn) => {
    const sessionId = btn.closest<HTMLElement>(".history-session")?.dataset.sessionId;
    if (sessionId) {
      btn.addEventListener("click", () => { navigate(`/history/session?id=${encodeURIComponent(sessionId)}`); });
    }
  });
}

function renderSession(session: WorkoutSession): string {
  const exerciseCount = session.loggedExercises.length;
  const exerciseLabel = exerciseCount === 1 ? "1 exercise" : `${exerciseCount} exercises`;

  return `
    <div class="history-session" data-session-id="${escapeHtml(session.workoutSessionId)}">
      <button class="history-session__header" type="button">
        <div class="history-session__info">
          <span class="history-session__workout-name">${escapeHtml(session.workoutName)}</span>
          <span class="history-session__date">${escapeHtml(formatDate(session.completedAt))}</span>
        </div>
        <span class="history-session__exercise-count">${exerciseLabel}</span>
      </button>
    </div>`;
}

function escapeHtml(text: string): string {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

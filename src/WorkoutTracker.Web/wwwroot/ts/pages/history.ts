import { getEffortLabel } from "../utils.js";

interface LoggedExercise {
  readonly loggedExerciseId: string;
  readonly exerciseId: string;
  readonly exerciseName: string;
  readonly loggedWeight: string | null;
  readonly notes: string | null;
  readonly effort: number | null;
}

interface WorkoutSession {
  readonly workoutSessionId: string;
  readonly plannedWorkoutId: string | null;
  readonly workoutName: string;
  readonly completedAt: string;
  readonly loggedExercises: LoggedExercise[];
}

export async function render(container: HTMLElement): Promise<void> {
  container.innerHTML = `
    <div class="history-page">
      <h1 class="history-page__title">Workout History</h1>
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

function getDateLabel(isoDate: string): string {
  const date = new Date(isoDate);
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const sessionDate = new Date(date);
  sessionDate.setHours(0, 0, 0, 0);

  const diffDays = Math.round(
    (today.getTime() - sessionDate.getTime()) / (1000 * 60 * 60 * 24)
  );

  if (diffDays <= 0) return "Today";
  if (diffDays === 1) return "Yesterday";
  return `${diffDays} days ago`;
}

function formatTime(isoDate: string): string {
  const date = new Date(isoDate);
  return new Intl.DateTimeFormat("en-US", {
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  }).format(date);
}

function renderSessions(sessions: WorkoutSession[], container: HTMLElement): void {
  // Group sessions by date label, preserving order (API returns DESC by completedAt)
  const groups: { label: string; sessions: WorkoutSession[] }[] = [];
  let currentLabel = "";

  for (const session of sessions) {
    const label = getDateLabel(session.completedAt);
    if (label !== currentLabel) {
      groups.push({ label, sessions: [session] });
      currentLabel = label;
    } else {
      groups[groups.length - 1].sessions.push(session);
    }
  }

  const html = groups
    .map(
      (group) => `
      <div class="history-group" role="region" aria-label="Sessions from ${escapeHtml(group.label)}">
        <div class="history-group__date-label">${escapeHtml(group.label)}</div>
        ${group.sessions.map((s) => renderSession(s)).join("")}
      </div>`
    )
    .join("");

  container.innerHTML = html;

  // Attach expand/collapse listeners
  container.querySelectorAll<HTMLButtonElement>(".history-session__header").forEach((btn) => {
    btn.addEventListener("click", () => {
      toggleSession(btn);
    });
  });
}

function renderSession(session: WorkoutSession): string {
  const exerciseCount = session.loggedExercises.length;
  const exerciseLabel = exerciseCount === 1 ? "1 exercise" : `${exerciseCount} exercises`;

  const exercisesHtml =
    exerciseCount === 0
      ? `<div class="history-session__exercise"><span class="history-session__exercise-name">No exercises logged</span></div>`
      : session.loggedExercises
          .map((ex) => {
            const parts: string[] = [];
            if (ex.loggedWeight !== null) parts.push(`${escapeHtml(ex.loggedWeight)} KG`);
            if (ex.effort !== null) parts.push(getEffortLabel(ex.effort));
            if (ex.notes !== null) parts.push(`— ${escapeHtml(ex.notes)}`);
            const dataStr = parts.join(" ");

            return `
            <div class="history-session__exercise">
              <span class="history-session__exercise-name">${escapeHtml(ex.exerciseName)}</span>
              <span class="history-session__exercise-data">${dataStr}</span>
            </div>`;
          })
          .join("");

  return `
    <div class="history-session" data-session-id="${escapeHtml(session.workoutSessionId)}">
      <button class="history-session__header" type="button" aria-expanded="false" aria-controls="session-details-${escapeHtml(session.workoutSessionId)}">
        <span class="history-session__workout-name">${escapeHtml(session.workoutName)}</span>
        <span class="history-session__time">${formatTime(session.completedAt)}</span>
        <span class="history-session__exercise-count">${exerciseLabel}</span>
        <span class="history-session__toggle">▸</span>
      </button>
      <div class="history-session__details" id="session-details-${escapeHtml(session.workoutSessionId)}" style="display:none;">
        ${exercisesHtml}
      </div>
    </div>`;
}

function toggleSession(btn: HTMLButtonElement): void {
  const expanded = btn.getAttribute("aria-expanded") === "true";
  const controlsId = btn.getAttribute("aria-controls");
  if (!controlsId) return;

  const details = document.getElementById(controlsId) as HTMLElement | null;
  if (!details) return;

  const sessionEl = btn.closest(".history-session") as HTMLElement | null;

  if (expanded) {
    btn.setAttribute("aria-expanded", "false");
    details.style.display = "none";
    sessionEl?.classList.remove("history-session--expanded");
  } else {
    btn.setAttribute("aria-expanded", "true");
    details.style.display = "";
    sessionEl?.classList.add("history-session--expanded");
  }
}

function escapeHtml(text: string): string {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

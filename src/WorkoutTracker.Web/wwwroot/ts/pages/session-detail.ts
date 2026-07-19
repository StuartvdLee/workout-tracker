import { navigate } from "../router.js";
import { getEffortLabel, normaliseValue, buildYTicks, buildXLabels } from "../utils.js";

let isDeleting = false;
let isSavingSessionEdit = false;
let currentSession: SessionDetailWithPrevious | null = null;
let originalEditSnapshot: SessionEditSnapshot | null = null;
let pendingDiscardAction: "back" | "cancel" | null = null;

interface SessionExerciseWithPrevious {
  readonly loggedExerciseId: string;
  readonly exerciseId: string;
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
  readonly overallEffort: number | null;
  readonly previousOverallEffort: number | null;
  readonly exercises: SessionExerciseWithPrevious[];
}

interface SessionTrendsExercise {
  readonly exerciseId: string;
  readonly exerciseName: string;
  readonly loggedWeight: string | null;
  readonly effort: number | null;
}

interface SessionTrendsDataPoint {
  readonly completedAt: string;
  readonly overallEffort: number | null;
  readonly exercises: SessionTrendsExercise[];
}

interface SessionTrends {
  readonly dataPoints: SessionTrendsDataPoint[];
}

interface SessionEditSnapshot {
  readonly overallEffort: number | null;
  readonly loggedExercises: SessionEditExerciseSnapshot[];
}

interface SessionEditExerciseSnapshot {
  readonly loggedExerciseId: string;
  readonly loggedWeight: string | null;
  readonly effort: number | null;
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
      <div class="session-detail__header">
        <button class="session-detail__back" id="session-detail-back" type="button" aria-label="Back to history">← Back</button>
        <div class="session-detail__title-group" id="session-detail-title-group" style="display:none;">
          <h1 class="session-detail__title" id="session-detail-title"></h1>
          <span class="session-detail__date" id="session-detail-date"></span>
        </div>
      </div>
      <div class="session-detail__loading" id="session-detail-loading">Loading...</div>
      <div id="session-detail-content" style="display:none;"></div>
      <div class="session-detail__error" id="session-detail-error" style="display:none;"></div>
    </div>
  `;

  document.getElementById("session-detail-back")?.addEventListener("click", () => {
    handleBackNavigation();
  });

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
  const errorEl = document.getElementById("session-detail-error") as HTMLElement | null;
  const titleGroupEl = document.getElementById("session-detail-title-group") as HTMLElement | null;
  const titleEl = document.getElementById("session-detail-title") as HTMLElement | null;
  const dateEl = document.getElementById("session-detail-date") as HTMLElement | null;

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
    if (titleGroupEl) titleGroupEl.style.display = "";
    if (titleEl) {
      titleEl.textContent = session.workoutName ?? "Workout";
    }
    if (dateEl) {
      dateEl.textContent = formatDate(session.completedAt);
    }
    currentSession = session;
    renderSessionContent(session, false);
  } catch {
    if (loadingEl) loadingEl.style.display = "none";
    if (errorEl) {
      errorEl.textContent = "Failed to load session details.";
      errorEl.style.display = "";
    }
  }
}

function renderSessionContent(session: SessionDetailWithPrevious, editMode: boolean): void {
  const contentEl = document.getElementById("session-detail-content") as HTMLElement | null;
  if (!contentEl) return;

  contentEl.innerHTML = editMode ? renderEditTable(session) : renderDetailTable(session);
  contentEl.style.display = "";

  if (editMode) {
    originalEditSnapshot = createSnapshot(session);
    wireEditHandlers(session.workoutSessionId);
    return;
  }

  originalEditSnapshot = null;
  void initChartSection(session, contentEl).catch(() => {
    // Chart loading is non-critical; keep the session details visible even if it fails.
  });
  appendDeleteSection(contentEl, session.workoutSessionId);
}

function formatDate(isoDate: string): string {
  const date = new Date(isoDate);
  return `${detailDateFormatter.format(date)} · ${detailTimeFormatter.format(date)}`;
}

function renderDetailTable(session: SessionDetailWithPrevious): string {
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
    <div class="session-detail__table-wrapper">
      <table class="session-detail__table" aria-label="Session exercises">
        <thead>
          <tr class="session-detail__head-row">
            <th class="session-detail__th" scope="col">Exercise</th>
            <th class="session-detail__th" scope="col">Weight (kg)</th>
            <th class="session-detail__th session-detail__th--prev" scope="col">Prev. Weight (kg)</th>
            <th class="session-detail__th" scope="col">Effort</th>
            <th class="session-detail__th session-detail__th--prev" scope="col">Prev. Effort</th>
          </tr>
        </thead>
        <tbody>
          ${rows}
        </tbody>
      </table>
    </div>
    ${buildOverallEffortRow(session)}`;
}

function renderEditTable(session: SessionDetailWithPrevious): string {
  const rows =
    session.exercises.length === 0
      ? `<tr><td class="session-detail__empty-cell" colspan="5">No exercises logged</td></tr>`
      : session.exercises
          .map((ex) => {
            const prevWeight = ex.previousWeight !== null ? escapeHtml(ex.previousWeight) : `<span class="session-detail__no-data">—</span>`;
            const prevEffort = ex.previousEffort !== null ? `${ex.previousEffort}` : `<span class="session-detail__no-data">—</span>`;
            return `
            <tr class="session-detail__row" data-logged-exercise-id="${escapeHtml(ex.loggedExerciseId)}">
              <td class="session-detail__cell session-detail__cell--exercise">${escapeHtml(ex.exerciseName)}</td>
              <td class="session-detail__cell">
                <input class="session-detail__input" data-session-edit-weight="${escapeHtml(ex.loggedExerciseId)}"
                  type="text" maxlength="100" value="${escapeHtml(ex.loggedWeight ?? "")}"
                  aria-label="Weight for ${escapeHtml(ex.exerciseName)}" />
              </td>
              <td class="session-detail__cell session-detail__cell--prev">${prevWeight}</td>
              <td class="session-detail__cell">
                ${buildEffortSelect(`session-edit-effort-${ex.loggedExerciseId}`, ex.effort, `Effort for ${ex.exerciseName}`, "session-detail__select", `data-session-edit-effort="${escapeHtml(ex.loggedExerciseId)}"`)}
              </td>
              <td class="session-detail__cell session-detail__cell--prev">${prevEffort}</td>
            </tr>`;
          })
          .join("");

  return `
    <div class="session-detail__edit-error" id="session-detail-edit-error" role="alert" aria-live="polite" style="display:none;"></div>
    <div class="session-detail__table-wrapper">
      <table class="session-detail__table" aria-label="Edit session exercises">
        <thead>
          <tr class="session-detail__head-row">
            <th class="session-detail__th" scope="col">Exercise</th>
            <th class="session-detail__th" scope="col">Weight (kg)</th>
            <th class="session-detail__th session-detail__th--prev" scope="col">Prev. Weight (kg)</th>
            <th class="session-detail__th" scope="col">Effort</th>
            <th class="session-detail__th session-detail__th--prev" scope="col">Prev. Effort</th>
          </tr>
        </thead>
        <tbody>
          ${rows}
        </tbody>
      </table>
    </div>
    ${buildEditOverallEffortRow(session)}
    <div class="session-detail__toolbar workout-form__actions">
      <button class="session-detail__save workout-form__submit" id="session-detail-save" type="button">Save changes</button>
      <button class="session-detail__cancel workout-form__cancel" id="session-detail-cancel" type="button">Cancel</button>
    </div>
    ${renderDiscardModal()}`;
}

function buildOverallEffortRow(session: SessionDetailWithPrevious): string {
  const overallEffortValue = session.overallEffort != null
    ? `${session.overallEffort} · ${escapeHtml(getEffortLabel(session.overallEffort))}`
    : `<span class="session-detail__no-data">—</span>`;
  const previousOverallEffortValue = session.previousOverallEffort != null
    ? `${session.previousOverallEffort} · ${escapeHtml(getEffortLabel(session.previousOverallEffort))}`
    : `<span class="session-detail__no-data">—</span>`;

  return `
    <div class="session-detail__overall-effort-row">
      <span class="session-detail__overall-effort-label">Overall Effort</span>
      <span class="session-detail__overall-effort-value">${overallEffortValue}</span>
      <span class="session-detail__overall-effort-prev-label">Previous</span>
      <span class="session-detail__overall-effort-prev-value">${previousOverallEffortValue}</span>
    </div>`;
}

function buildEditOverallEffortRow(session: SessionDetailWithPrevious): string {
  const previousOverallEffortValue = session.previousOverallEffort != null
    ? `${session.previousOverallEffort} · ${escapeHtml(getEffortLabel(session.previousOverallEffort))}`
    : `<span class="session-detail__no-data">—</span>`;

  return `
    <div class="session-detail__overall-effort-row">
      <label class="session-detail__overall-effort-label" for="session-edit-overall-effort">Overall Effort</label>
      <span class="session-detail__overall-effort-value">
        ${buildEffortSelect("session-edit-overall-effort", session.overallEffort, "Overall workout effort", "session-detail__select session-detail__select--overall")}
      </span>
      <span class="session-detail__overall-effort-prev-label">Previous</span>
      <span class="session-detail__overall-effort-prev-value">${previousOverallEffortValue}</span>
    </div>`;
}

function buildEffortSelect(id: string, value: number | null, ariaLabel: string, className: string, extraAttributes = ""): string {
  const options = [`<option value="">Not rated</option>`];
  for (let effort = 1; effort <= 10; effort++) {
    const selected = value === effort ? " selected" : "";
    options.push(`<option value="${effort}"${selected}>${effort} · ${escapeHtml(getEffortLabel(effort))}</option>`);
  }

  return `<select class="${className}" id="${escapeHtml(id)}" aria-label="${escapeHtml(ariaLabel)}" ${extraAttributes}>${options.join("")}</select>`;
}

function renderDiscardModal(): string {
  return `
    <div class="discard-modal-backdrop" id="session-edit-discard-backdrop" style="display:none;">
      <div class="discard-modal" role="alertdialog" aria-modal="true"
           aria-labelledby="session-edit-discard-title"
           aria-describedby="session-edit-discard-desc">
        <h2 class="discard-modal__title" id="session-edit-discard-title">Discard changes?</h2>
        <p class="discard-modal__desc" id="session-edit-discard-desc">
          You have unsaved session edits. Are you sure you want to discard them?
        </p>
        <div class="discard-modal__actions">
          <button class="discard-modal__discard" type="button" id="session-edit-discard-confirm">Discard</button>
          <button class="discard-modal__continue" type="button" id="session-edit-discard-cancel">Keep editing</button>
        </div>
      </div>
    </div>`;
}

function wireEditHandlers(sessionId: string): void {
  document.getElementById("session-detail-save")?.addEventListener("click", () => {
    void saveSessionEdits(sessionId);
  });

  document.getElementById("session-detail-cancel")?.addEventListener("click", () => {
    handleEditCancel();
  });

  document.getElementById("session-edit-discard-confirm")?.addEventListener("click", () => {
    confirmDiscardEdits();
  });

  document.getElementById("session-edit-discard-cancel")?.addEventListener("click", () => {
    closeEditDiscardModal();
  });

  document.getElementById("session-edit-discard-backdrop")?.addEventListener("keydown", (event: KeyboardEvent) => {
    if (event.key === "Escape") {
      closeEditDiscardModal();
      return;
    }

    if (event.key === "Tab") {
      const confirmBtn = document.getElementById("session-edit-discard-confirm") as HTMLButtonElement | null;
      const cancelBtn = document.getElementById("session-edit-discard-cancel") as HTMLButtonElement | null;
      const focusable = [confirmBtn, cancelBtn].filter((el): el is HTMLButtonElement => el !== null);
      if (focusable.length < 2) return;

      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    }
  });
}

function createSnapshot(session: SessionDetailWithPrevious): SessionEditSnapshot {
  return {
    overallEffort: session.overallEffort,
    loggedExercises: session.exercises.map(ex => ({
      loggedExerciseId: ex.loggedExerciseId,
      loggedWeight: ex.loggedWeight,
      effort: ex.effort,
    })),
  };
}

function readEditSnapshot(): SessionEditSnapshot {
  const overallSelect = document.getElementById("session-edit-overall-effort") as HTMLSelectElement | null;
  const exerciseRows = Array.from(document.querySelectorAll<HTMLTableRowElement>("[data-logged-exercise-id]"));

  return {
    overallEffort: parseNullableEffort(overallSelect?.value ?? ""),
    loggedExercises: exerciseRows.map(row => {
      const loggedExerciseId = row.dataset.loggedExerciseId ?? "";
      const weightInput = row.querySelector<HTMLInputElement>("[data-session-edit-weight]");
      const effortSelect = row.querySelector<HTMLSelectElement>("[data-session-edit-effort]");
      const normalizedWeight = weightInput?.value.trim() ?? "";
      return {
        loggedExerciseId,
        loggedWeight: normalizedWeight.length === 0 ? null : normalizedWeight,
        effort: parseNullableEffort(effortSelect?.value ?? ""),
      };
    }),
  };
}

function parseNullableEffort(value: string): number | null {
  if (value === "") return null;
  const parsed = Number(value);
  return Number.isInteger(parsed) ? parsed : null;
}

function hasUnsavedSessionEditChanges(): boolean {
  if (originalEditSnapshot === null) return false;
  return JSON.stringify(readEditSnapshot()) !== JSON.stringify(originalEditSnapshot);
}

function handleBackNavigation(): void {
  if (originalEditSnapshot !== null && hasUnsavedSessionEditChanges()) {
    pendingDiscardAction = "back";
    openEditDiscardModal();
    return;
  }

  navigate("/history");
}

function handleEditCancel(): void {
  if (hasUnsavedSessionEditChanges()) {
    pendingDiscardAction = "cancel";
    openEditDiscardModal();
    return;
  }

  if (currentSession) renderSessionContent(currentSession, false);
}

function openEditDiscardModal(): void {
  const backdrop = document.getElementById("session-edit-discard-backdrop") as HTMLElement | null;
  if (backdrop) {
    backdrop.style.display = "";
    document.getElementById("session-edit-discard-confirm")?.focus();
  }
}

function closeEditDiscardModal(): void {
  const backdrop = document.getElementById("session-edit-discard-backdrop") as HTMLElement | null;
  if (backdrop) backdrop.style.display = "none";
  pendingDiscardAction = null;
  document.getElementById("session-detail-cancel")?.focus();
}

function confirmDiscardEdits(): void {
  const action = pendingDiscardAction;
  pendingDiscardAction = null;
  if (action === "back") {
    navigate("/history");
    return;
  }

  if (currentSession) renderSessionContent(currentSession, false);
}

async function saveSessionEdits(sessionId: string): Promise<void> {
  if (isSavingSessionEdit) return;
  const snapshot = readEditSnapshot();
  const errorEl = document.getElementById("session-detail-edit-error") as HTMLElement | null;
  const saveBtn = document.getElementById("session-detail-save") as HTMLButtonElement | null;
  const cancelBtn = document.getElementById("session-detail-cancel") as HTMLButtonElement | null;

  if (snapshot.loggedExercises.some(ex => ex.loggedWeight !== null && ex.loggedWeight.length > 100)) {
    showEditError("Logged weight must not exceed 100 characters.");
    return;
  }

  isSavingSessionEdit = true;
  if (errorEl) errorEl.style.display = "none";
  if (saveBtn) {
    saveBtn.disabled = true;
    saveBtn.textContent = "Saving...";
  }
  if (cancelBtn) cancelBtn.disabled = true;

  try {
    const response = await fetch(`/api/sessions/${encodeURIComponent(sessionId)}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(snapshot),
    });

    if (!response.ok) {
      let message = "Failed to save session changes. Please try again.";
      try {
        const error = await response.json();
        if (typeof error?.error === "string") message = error.error;
      } catch {
        // Keep generic save message when the API does not return a structured error body.
      }
      showEditError(message);
      return;
    }

    const updatedSession: SessionDetailWithPrevious = await response.json();
    currentSession = updatedSession;
    renderSessionContent(updatedSession, false);
  } catch {
    showEditError("Failed to save session changes. Please try again.");
  } finally {
    isSavingSessionEdit = false;
    if (saveBtn) {
      saveBtn.disabled = false;
      saveBtn.textContent = "Save changes";
    }
    if (cancelBtn) cancelBtn.disabled = false;
  }
}

function showEditError(message: string): void {
  const errorEl = document.getElementById("session-detail-edit-error") as HTMLElement | null;
  if (errorEl) {
    errorEl.textContent = message;
    errorEl.style.display = "";
  }
}

function escapeHtml(text: string): string {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

async function initChartSection(session: SessionDetailWithPrevious, contentEl: HTMLElement): Promise<void> {
  if (session.plannedWorkoutId === null) return;

  const chartSection = document.createElement("div");
  chartSection.className = "session-chart";
  chartSection.id = "session-chart";
  chartSection.innerHTML = `
    <div class="session-chart__header">
      <label class="session-chart__label" for="session-chart-select">Show:</label>
      <select class="session-chart__select" id="session-chart-select" disabled></select>
    </div>
    <div id="session-chart-body">
      <div class="session-chart__loading" aria-live="polite">Loading chart data…</div>
    </div>
  `;
  contentEl.appendChild(chartSection);

  const selectEl = document.getElementById("session-chart-select") as HTMLSelectElement | null;
  const bodyEl = document.getElementById("session-chart-body") as HTMLElement | null;
  if (!selectEl || !bodyEl) return;

  const fallbackTrends: SessionTrends = {
    dataPoints: [
      {
        completedAt: session.completedAt,
        overallEffort: session.overallEffort,
        exercises: session.exercises.map(ex => ({
          exerciseId: ex.exerciseId,
          exerciseName: ex.exerciseName,
          loggedWeight: ex.loggedWeight,
          effort: ex.effort,
        })),
      },
    ],
  };

  const overallOption = document.createElement("option");
  overallOption.value = "overall";
  overallOption.textContent = "Overall Session Effort";
  selectEl.appendChild(overallOption);

  const seenExerciseIds = new Set<string>();
  for (const ex of session.exercises) {
    const exerciseToken = ex.exerciseId && ex.exerciseId.length > 0
      ? ex.exerciseId
      : `name:${encodeURIComponent(ex.exerciseName)}`;
    if (seenExerciseIds.has(exerciseToken)) continue;
    seenExerciseIds.add(exerciseToken);

    const exerciseOpt = document.createElement("option");
    exerciseOpt.value = `exercise:${exerciseToken}`;
    exerciseOpt.textContent = ex.exerciseName;
    selectEl.appendChild(exerciseOpt);
  }

  selectEl.disabled = false;
  renderChartForSelection(selectEl.value, fallbackTrends, bodyEl);
  let currentTrends: SessionTrends = fallbackTrends;

  selectEl.addEventListener("change", () => {
    renderChartForSelection(selectEl.value, currentTrends, bodyEl);
  });

  try {
    const response = await fetch(`/api/workouts/${encodeURIComponent(session.plannedWorkoutId)}/session-trends`);
    if (!response.ok) throw new Error(`Trends fetch failed: ${response.status}`);
    const trends: SessionTrends = await response.json();
    if (!selectEl.isConnected || !bodyEl.isConnected) return;
    currentTrends = trends;
    renderChartForSelection(selectEl.value, currentTrends, bodyEl);
  } catch {
    // Keep showing fallback chart data from the current session when trends are unavailable.
  }
}

function renderChartForSelection(selection: string, trends: SessionTrends, bodyEl: HTMLElement): void {
  const dp = trends.dataPoints;

  if (dp.length === 0) {
    bodyEl.innerHTML = `<p class="session-chart__empty">No session data available.</p>`;
    return;
  }

  const dates = dp.map(d => d.completedAt);
  let values: (number | null)[];
  let yMin: number;
  let yMax: number;

  if (selection === "overall") {
    values = dp.map(d => d.overallEffort);
    yMin = 0;
    yMax = 10;
    if (values.every(v => v === null)) {
      bodyEl.innerHTML = `<p class="session-chart__empty">No data recorded for this selection.</p>`;
      return;
    }

    bodyEl.innerHTML = renderLineSvg(
      dates,
      values,
      yMin,
      yMax,
      "session-chart__line--effort",
      "session-chart__point--effort",
      "Overall session effort"
    );
    return;
  }

  const exercisePrefix = "exercise:";
  if (!selection.startsWith(exercisePrefix)) {
    bodyEl.innerHTML = `<p class="session-chart__empty">No data recorded for this selection.</p>`;
    return;
  }

  const exerciseToken = selection.slice(exercisePrefix.length);
  const nameTokenPrefix = "name:";
  const isNameToken = exerciseToken.startsWith(nameTokenPrefix);
  const selectedExerciseName = isNameToken
    ? decodeURIComponent(exerciseToken.slice(nameTokenPrefix.length))
    : null;
  const matchesSelection = (ex: SessionTrendsExercise): boolean =>
    isNameToken
      ? ex.exerciseName === selectedExerciseName
      : ex.exerciseId === exerciseToken;
  const weightValues = dp.map(d => {
    const ex = d.exercises.find(matchesSelection);
    if (!ex || ex.loggedWeight === null) return null;
    const n = Number(ex.loggedWeight);
    return Number.isNaN(n) ? null : n;
  });
  const effortValues = dp.map(d => {
    const ex = d.exercises.find(matchesSelection);
    return ex?.effort ?? null;
  });

  const hasWeight = weightValues.some(v => v !== null);
  const hasEffort = effortValues.some(v => v !== null);
  if (!hasWeight && !hasEffort) {
    bodyEl.innerHTML = `<p class="session-chart__empty">No weight or effort data recorded for this exercise.</p>`;
    return;
  }

  const resolvedExerciseName = selectedExerciseName
    ?? dp.flatMap(d => d.exercises).find(matchesSelection)?.exerciseName
    ?? "exercise";
  bodyEl.innerHTML = renderCombinedExerciseSvg(dates, weightValues, effortValues, resolvedExerciseName);
}

function renderLineSvg(
  dates: readonly string[],
  values: readonly (number | null)[],
  yMin: number,
  yMax: number,
  lineModifierClass: string,
  pointModifierClass: string,
  ariaLabel: string
): string {
  const n = dates.length;
  const xOf = (i: number): number => n > 1 ? 50 + (i / (n - 1)) * 530 : 315;

  const polylines = buildPolylineSegments(values, yMin, yMax, xOf);

  const circles = values
    .map((v, i) =>
      v !== null
        ? `<circle class="session-chart__point ${pointModifierClass}" cx="${xOf(i).toFixed(1)}" cy="${normaliseValue(v, yMin, yMax).toFixed(1)}" r="3"/>`
        : "")
    .join("");

  const tickCount = yMin === 0 && yMax === 10 ? 6 : 5;
  const yTicks = buildYTicks(yMin, yMax, tickCount);
  const yTickLines = yTicks.map(t => {
    const y = normaliseValue(t, yMin, yMax).toFixed(1);
    const label = Number.isInteger(t) ? `${t}` : t.toFixed(1);
    return `<line class="session-chart__gridline" x1="50" y1="${y}" x2="580" y2="${y}"/>` +
      `<text class="session-chart__tick-label" x="45" y="${y}" text-anchor="end" dominant-baseline="middle">${escapeHtml(label)}</text>`;
  }).join("");

  const maxLabels = Math.min(n, 6);
  const xLabels = buildXLabels(dates, maxLabels);
  const xLabelEls = xLabels
    .map((label, i) => {
      if (label === null) return "";
      const x = xOf(i).toFixed(1);
      // Right-align the last label to prevent clipping at the SVG boundary
      const anchor = i === n - 1 ? "end" : "middle";
      return `<text class="session-chart__date-label" x="${x}" y="240" text-anchor="${anchor}">${escapeHtml(label)}</text>`;
    })
    .join("");

  const polylineEls = polylines
    .map(pts => `<polyline class="session-chart__line ${lineModifierClass}" points="${pts}"/>`)
    .join("");

  return `<div class="session-chart__container">
    <svg class="session-chart__svg" viewBox="0 0 600 260" role="img" aria-label="${escapeHtml(ariaLabel)}">
      <line class="session-chart__axis-line" x1="50" y1="20" x2="50" y2="220"/>
      <line class="session-chart__axis-line" x1="50" y1="220" x2="580" y2="220"/>
      ${yTickLines}
      ${xLabelEls}
      ${polylineEls}
      ${circles}
    </svg>
  </div>`;
}

function renderCombinedExerciseSvg(
  dates: readonly string[],
  weightValues: readonly (number | null)[],
  effortValues: readonly (number | null)[],
  exerciseName: string
): string {
  const n = dates.length;
  const xOf = (i: number): number => n > 1 ? 50 + (i / (n - 1)) * 530 : 315;

  const numericWeightValues = weightValues.filter((v): v is number => v !== null);
  const rawWeightMin = numericWeightValues.length > 0 ? Math.min(...numericWeightValues) : 0;
  const rawWeightMax = numericWeightValues.length > 0 ? Math.max(...numericWeightValues) : 1;
  const weightMin = rawWeightMin === rawWeightMax ? rawWeightMin - 1 : rawWeightMin;
  const weightMax = rawWeightMin === rawWeightMax ? rawWeightMax + 1 : rawWeightMax;
  const effortMin = 0;
  const effortMax = 10;

  const weightPolylines = buildPolylineSegments(weightValues, weightMin, weightMax, xOf);
  const effortPolylines = buildPolylineSegments(effortValues, effortMin, effortMax, xOf);
  const weightCircles = buildPointCircles(weightValues, weightMin, weightMax, xOf, "session-chart__point--weight");
  const effortCircles = buildPointCircles(effortValues, effortMin, effortMax, xOf, "session-chart__point--effort");

  const leftTicks = numericWeightValues.length > 0 ? buildYTicks(weightMin, weightMax, 5) : [];
  const leftTickEls = leftTicks.map(t => {
    const y = normaliseValue(t, weightMin, weightMax).toFixed(1);
    const label = Number.isInteger(t) ? `${t}` : t.toFixed(1);
    return `<line class="session-chart__gridline" x1="50" y1="${y}" x2="580" y2="${y}"/>` +
      `<text class="session-chart__tick-label" x="45" y="${y}" text-anchor="end" dominant-baseline="middle">${escapeHtml(label)}</text>`;
  }).join("");

  const rightTicks = buildYTicks(effortMin, effortMax, 6);
  const rightTickEls = rightTicks.map(t => {
    const y = normaliseValue(t, effortMin, effortMax).toFixed(1);
    const label = Number.isInteger(t) ? `${t}` : t.toFixed(1);
    return `<text class="session-chart__tick-label" x="585" y="${y}" text-anchor="start" dominant-baseline="middle">${escapeHtml(label)}</text>`;
  }).join("");

  const maxLabels = Math.min(n, 6);
  const xLabels = buildXLabels(dates, maxLabels);
  const xLabelEls = xLabels
    .map((label, i) => {
      if (label === null) return "";
      const x = xOf(i).toFixed(1);
      const anchor = i === n - 1 ? "end" : "middle";
      return `<text class="session-chart__date-label" x="${x}" y="240" text-anchor="${anchor}">${escapeHtml(label)}</text>`;
    })
    .join("");

  const weightLineEls = weightPolylines
    .map(pts => `<polyline class="session-chart__line session-chart__line--weight" points="${pts}"/>`)
    .join("");
  const effortLineEls = effortPolylines
    .map(pts => `<polyline class="session-chart__line session-chart__line--effort" points="${pts}"/>`)
    .join("");

  return `<div class="session-chart__container">
    <div class="session-chart__legend" aria-hidden="true">
      <span class="session-chart__legend-item"><span class="session-chart__legend-swatch session-chart__legend-swatch--weight"></span>Weight</span>
      <span class="session-chart__legend-item"><span class="session-chart__legend-swatch session-chart__legend-swatch--effort"></span>Effort</span>
    </div>
    <svg class="session-chart__svg" viewBox="0 0 600 260" role="img" aria-label="${escapeHtml(`Weight and effort for ${exerciseName}`)}">
      <line class="session-chart__axis-line" x1="50" y1="20" x2="50" y2="220"/>
      <line class="session-chart__axis-line" x1="580" y1="20" x2="580" y2="220"/>
      <line class="session-chart__axis-line" x1="50" y1="220" x2="580" y2="220"/>
      ${leftTickEls}
      ${rightTickEls}
      ${xLabelEls}
      ${weightLineEls}
      ${effortLineEls}
      ${weightCircles}
      ${effortCircles}
    </svg>
  </div>`;
}

function buildPolylineSegments(
  values: readonly (number | null)[],
  yMin: number,
  yMax: number,
  xOf: (index: number) => number
): string[] {
  const polylines: string[] = [];
  let segment: string[] = [];
  for (let i = 0; i < values.length; i++) {
    const v = values[i];
    if (v !== null) {
      segment.push(`${xOf(i).toFixed(1)},${normaliseValue(v, yMin, yMax).toFixed(1)}`);
    } else {
      if (segment.length >= 2) polylines.push(segment.join(" "));
      segment = [];
    }
  }
  if (segment.length >= 2) polylines.push(segment.join(" "));
  return polylines;
}

function buildPointCircles(
  values: readonly (number | null)[],
  yMin: number,
  yMax: number,
  xOf: (index: number) => number,
  pointModifierClass: string
): string {
  return values
    .map((v, i) =>
      v !== null
        ? `<circle class="session-chart__point ${pointModifierClass}" cx="${xOf(i).toFixed(1)}" cy="${normaliseValue(v, yMin, yMax).toFixed(1)}" r="3"/>`
        : "")
    .join("");
}

let pendingDeleteSessionId: string | null = null;

function appendDeleteSection(contentEl: HTMLElement, sessionId: string): void {
  const deleteSection = document.createElement("div");
  deleteSection.className = "session-detail__delete-section";
  deleteSection.innerHTML = `
    <button class="session-detail__edit" id="session-detail-edit" type="button">Edit session</button>
    <button class="session-detail__delete" id="session-detail-delete" type="button">Delete session</button>
    <div class="session-detail__delete-error" id="session-detail-delete-error"
         role="alert" aria-live="polite" style="display:none;"></div>
  `;
  contentEl.appendChild(deleteSection);

  const editBtn = deleteSection.querySelector<HTMLButtonElement>("#session-detail-edit")!;
  editBtn.addEventListener("click", () => {
    if (currentSession) renderSessionContent(currentSession, true);
  });

  // deleteBtn is a fresh element on every load — safe to add listener directly
  const deleteBtn = deleteSection.querySelector<HTMLButtonElement>("#session-detail-delete")!;
  deleteBtn.addEventListener("click", () => {
    pendingDeleteSessionId = sessionId;
    openDeleteModal();
  });

  if (!document.getElementById("session-delete-confirm-backdrop")) {
    const backdrop = document.createElement("div");
    backdrop.className = "discard-modal-backdrop";
    backdrop.id = "session-delete-confirm-backdrop";
    backdrop.style.display = "none";
    backdrop.innerHTML = `
      <div class="discard-modal" role="alertdialog" aria-modal="true"
           aria-labelledby="session-delete-confirm-title"
           aria-describedby="session-delete-confirm-desc">
        <h2 class="discard-modal__title" id="session-delete-confirm-title">Delete session?</h2>
        <p class="discard-modal__desc" id="session-delete-confirm-desc">
          This will permanently remove this session and cannot be undone.
        </p>
        <div class="discard-modal__actions">
          <button class="discard-modal__discard" type="button" id="session-delete-confirm-ok">Delete</button>
          <button class="discard-modal__continue" type="button" id="session-delete-confirm-cancel">Keep session</button>
        </div>
      </div>
    `;
    document.body.appendChild(backdrop);
    // Wire modal handlers exactly once — they use pendingDeleteSessionId
    wireModalHandlers();
  }
}

function openDeleteModal(): void {
  const backdrop = document.getElementById("session-delete-confirm-backdrop");
  if (backdrop) {
    backdrop.style.display = "";
    document.getElementById("session-delete-confirm-ok")?.focus();
  }
}

function closeDeleteModal(): void {
  const backdrop = document.getElementById("session-delete-confirm-backdrop");
  if (backdrop) {
    backdrop.style.display = "none";
  }
  document.getElementById("session-detail-delete")?.focus();
}

function wireModalHandlers(): void {
  const confirmOk = document.getElementById("session-delete-confirm-ok") as HTMLButtonElement | null;
  const confirmCancel = document.getElementById("session-delete-confirm-cancel") as HTMLButtonElement | null;
  const backdrop = document.getElementById("session-delete-confirm-backdrop");

  confirmCancel?.addEventListener("click", () => {
    closeDeleteModal();
  });

  confirmOk?.addEventListener("click", () => {
    closeDeleteModal();
    if (pendingDeleteSessionId !== null) {
      void handleDeleteConfirmed(pendingDeleteSessionId);
    }
  });

  backdrop?.addEventListener("keydown", (e: KeyboardEvent) => {
    if (e.key === "Escape") {
      closeDeleteModal();
      return;
    }
    if (e.key === "Tab") {
      const focusable = [confirmOk, confirmCancel].filter((el): el is HTMLButtonElement => el !== null);
      if (focusable.length < 2) return;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (e.shiftKey) {
        if (document.activeElement === first) {
          e.preventDefault();
          last.focus();
        }
      } else {
        if (document.activeElement === last) {
          e.preventDefault();
          first.focus();
        }
      }
    }
  });
}

async function handleDeleteConfirmed(sessionId: string): Promise<void> {
  if (isDeleting) return;
  isDeleting = true;

  const deleteBtn = document.getElementById("session-detail-delete") as HTMLButtonElement | null;
  const errorEl = document.getElementById("session-detail-delete-error") as HTMLElement | null;

  if (deleteBtn) deleteBtn.disabled = true;
  if (errorEl) errorEl.style.display = "none";

  try {
    const response = await fetch(`/api/sessions/${encodeURIComponent(sessionId)}`, { method: "DELETE" });
    if (response.ok || response.status === 404) {
      navigate("/history?deleted=1");
      return;
    }
    throw new Error(`Delete failed: ${response.status}`);
  } catch {
    if (errorEl) {
      errorEl.textContent = "Failed to delete session. Please try again.";
      errorEl.style.display = "";
    }
    if (deleteBtn) deleteBtn.disabled = false;
  } finally {
    isDeleting = false;
  }
}

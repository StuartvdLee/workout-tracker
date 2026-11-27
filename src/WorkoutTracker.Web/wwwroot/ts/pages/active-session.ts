import { navigate, onRouteCleanup } from "../router.js";
import { initSortableList } from "../sortable-list.js";
import { getEffortLabel, getEffortColour, applyOrder } from "../utils.js";
import { acquireDraftLock, readDraft, removeDraft, writeDraft } from "../active-workout-draft.js";
import type { ActiveWorkoutDraft, CompletionPayload } from "../active-workout-draft.js";

interface WorkoutExercise {
  readonly exerciseId: string;
  readonly name: string;
  readonly targetReps: string | null;
  readonly targetWeight: string | null;
}

interface WorkoutDetail {
  readonly plannedWorkoutId: string;
  readonly name: string;
  readonly exercises: WorkoutExercise[];
}

interface LogEntry {
  loggedWeight: string;
  loggedEffort: number | null;
}

interface PreviousExerciseData {
  readonly exerciseId: string;
  readonly loggedWeight: string | null;
  readonly effort: number | null;
  readonly sequence: number | null;
  readonly sets?: number | null;
  readonly completedAt?: string;
}

interface PreviousPerformance {
  readonly hasPreviousSession: boolean;
  readonly completedAt: string | null;
  readonly exercises: PreviousExerciseData[];
}

interface SessionCreateResponse {
  readonly workoutSessionId: string;
}

let workout: WorkoutDetail | null = null;
let logEntries: Map<string, LogEntry> = new Map();
let isSaving = false;
let exerciseOrder: string[] | null = null;
let pendingOverallEffort: number | null = null;
let isOrderEditing = false;
let orderBeforeEditing: WorkoutExercise[] | null = null;
let hasOrderChanges = false;
let currentPreviousData: Map<string, PreviousExerciseData> | "error" | null = null;
let sessionSets: 3 | 5 | null = null;
let draft: ActiveWorkoutDraft | null = null;
let renderGeneration = 0;
const MODAL_FOCUSABLE_SELECTOR =
  'button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), a[href], [contenteditable="true"], [tabindex]:not([tabindex="-1"])';

export async function render(container: HTMLElement): Promise<void> {
  const generation = ++renderGeneration;
  let active = true;
  let releaseLock: (() => void) | null = null;
  onRouteCleanup(() => {
    active = false;
    if (renderGeneration === generation) renderGeneration++;
    releaseLock?.();
  });
  const params = new URLSearchParams(window.location.search);
  const workoutId = params.get("id");
  const setsParam = params.get("sets");

  if (!workoutId) {
    container.innerHTML = `
      <div class="active-session">
        <p>No workout selected. <a href="/">Return to Let's go!</a></p>
      </div>
    `;
    return;
  }
  container.textContent = "Checking saved workout...";
  const lock = await acquireDraftLock();
  if (!active) {
    if (lock.status === "acquired") lock.release();
    return;
  }
  if (lock.status !== "acquired") {
    showRecovery(container, lock.status === "busy"
      ? "This workout is open in another tab. Close it there and retry."
      : "Workout recovery requires browser lock support. Enable it and retry.", false);
    return;
  }
  releaseLock = lock.release;
  const saved = readDraft();
  if (saved.status === "invalid" || saved.status === "unavailable") {
    showRecovery(container, saved.status === "invalid"
      ? "The saved workout is invalid or too large. Clear it explicitly to start again."
      : "Saved workout storage is unavailable. Check your browser settings and retry.", saved.status === "invalid");
    return;
  }
  if (saved.status === "found" && saved.draft.workoutId !== workoutId) {
    showRecovery(container, "Another workout is already in progress. Resume it or explicitly discard it.", true, saved.draft);
    return;
  }
  draft = saved.status === "found" ? saved.draft : null;
  if (draft?.completion) {
    renderPendingScreen(container, draft, generation);
    return;
  }
  sessionSets = setsParam === "3" || setsParam === "5" ? Number(setsParam) as 3 | 5 : null;
  if (draft) sessionSets = draft.sets;
  if (sessionSets === null) {
    container.innerHTML = `
      <div class="active-session">
        <p role="alert">Sets must be 3 or 5. <a href="/">Return to Let's go! and select sets.</a></p>
      </div>
    `;
    return;
  }

  // Reset state
  workout = null;
  logEntries = new Map();
  isSaving = false;
  exerciseOrder = null;
  pendingOverallEffort = null;
  isOrderEditing = false;
  orderBeforeEditing = null;
  hasOrderChanges = false;
  currentPreviousData = null;

  const orderParam = params.get("order");
  exerciseOrder = orderParam ? orderParam.split(",").map(id => id.trim()).filter(id => id.length > 0) : null;
  if (draft) {
    exerciseOrder = draft.exerciseOrder;
    logEntries = new Map(Object.entries(draft.entries).map(([id, entry]) => [id, { ...entry }]));
  }

  function renderPendingScreen(container: HTMLElement, pending: ActiveWorkoutDraft, generation: number): void {
    container.innerHTML = `
      <div class="active-session">
        <p id="pending-status" role="status" aria-live="polite">A previous completion may have succeeded. Retry to check its result.</p>
        <div id="pending-error" role="alert"></div>
        <button class="active-session__save-btn" type="button" id="pending-retry">Retry completion</button>
      </div>`;
    const retry = container.querySelector<HTMLButtonElement>("#pending-retry")!;
    const status = container.querySelector<HTMLElement>("#pending-status")!;
    const error = container.querySelector<HTMLElement>("#pending-error")!;
    let readyToEdit = false;
    retry.addEventListener("click", async () => {
      if (readyToEdit) {
        window.location.reload();
        return;
      }
      retry.disabled = true;
      status.textContent = "Checking completion...";
      error.textContent = "";
      try {
        const response = await fetch(`/api/workouts/${pending.workoutId}/sessions`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(pending.completion),
        });
        if (generation !== renderGeneration) return;
        const data: { workoutSessionId?: string; error?: string; safeToEdit?: boolean } = await response.json();
        if (generation !== renderGeneration) return;
        if (response.ok && typeof data.workoutSessionId === "string" && data.workoutSessionId) {
          try {
            removeDraft();
            navigate(`/history/session?id=${encodeURIComponent(data.workoutSessionId)}`);
          } catch {
            status.textContent = "Workout completed, but its saved draft could not be removed. Retry cleanup.";
          }
        } else if ((response.status === 400 || response.status === 404) && data.safeToEdit === true) {
          try {
            writeDraft({ ...pending, completion: null });
            error.textContent = data.error ?? "Completion was rejected.";
            status.textContent = "The rejected attempt is safely blocked. You can edit or discard the workout.";
            retry.textContent = "Edit workout";
            readyToEdit = true;
          } catch {
            status.textContent = "The rejection was confirmed, but the draft could not be updated. Retry.";
          }
        } else {
          status.textContent = response.status === 400
            ? "Completion was rejected, but an earlier attempt may still be in flight. Your saved progress remains locked."
            : response.status === 409 || response.status === 410
            ? "This completion cannot be retried as a new workout. Your saved progress remains intact."
            : "Completion may have succeeded. Retry to check its result.";
          error.textContent = data.error ?? "The completion could not be confirmed.";
          if (response.status === 410) offerDeletedCleanup(error);
          if (response.status === 409 && data.workoutSessionId) {
            const link = document.createElement("a");
            link.href = `/history/session?id=${encodeURIComponent(data.workoutSessionId)}`;
            link.textContent = " View completed workout";
            error.appendChild(link);
          }
        }
      } catch {
        if (generation === renderGeneration) status.textContent = "Completion may have succeeded. Retry to check its result.";
      } finally {
        if (generation === renderGeneration) retry.disabled = false;
      }
    });
  }

  container.innerHTML = `
    <div class="active-session">
      <div class="active-session__layout">
        <div class="active-session__content">
          <div class="active-session__header">
            <h1 class="active-session__title" id="session-title">Loading...</h1>
          </div>
          <div class="active-session__exercises" id="session-exercises" role="form" aria-label="Log workout exercises"></div>
          <div class="active-session__error" id="session-error" role="alert" aria-live="polite"></div>
          <div class="active-session__api-error" id="session-api-error" role="alert" aria-live="polite"></div>
          <div id="draft-status" role="status" aria-live="polite"></div>
          <button class="active-session__cancel-btn" type="button" id="draft-retry" style="display:none;">Retry saving progress</button>
          <div class="active-session__actions">
            <button class="active-session__save-btn" type="button" id="session-save">Save Workout</button>
            <button class="active-session__cancel-btn" type="button" id="session-cancel">Cancel</button>
          </div>
        </div>
        <div class="active-session__order-rail">
          <button class="active-session__edit-order-btn" type="button" id="session-edit-order" aria-label="Edit order" title="Edit order" style="display:none;">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
              <path d="M3 6h10M3 12h10M3 18h10M18 3v18M15 6l3-3 3 3M15 18l3 3 3-3"/>
            </svg>
          </button>
        </div>
      </div>
      <div class="discard-modal-backdrop" id="discard-backdrop" style="display:none;">
        <div class="discard-modal" role="alertdialog" aria-modal="true" aria-labelledby="discard-title" aria-describedby="discard-desc">
          <h2 class="discard-modal__title" id="discard-title">Discard Workout?</h2>
          <p class="discard-modal__desc" id="discard-desc">You have unsaved changes. Are you sure you want to discard this workout?</p>
          <div class="discard-modal__actions">
            <button class="discard-modal__discard" type="button" id="discard-confirm">Discard</button>
            <button class="discard-modal__continue" type="button" id="discard-cancel">Continue workout</button>
          </div>
        </div>
      </div>
      <div class="effort-modal-backdrop" id="effort-backdrop" style="display:none;">
        <div class="effort-modal" role="alertdialog" aria-modal="true" aria-labelledby="effort-modal-title" aria-describedby="effort-modal-desc">
          <h2 class="effort-modal__title" id="effort-modal-title">Overall Workout Effort</h2>
          <p class="effort-modal__desc" id="effort-modal-desc">How hard was this workout overall?</p>
          <div class="effort-modal__slider-group">
            <div class="effort-modal__label-row">
              <label class="effort-modal__label" for="overall-effort-slider">Effort</label>
              <span class="effort-modal__value" id="overall-effort-value">Not rated</span>
            </div>
            <input class="effort-modal__slider" type="range" id="overall-effort-slider"
              min="1" max="10" step="1" value="1"
              data-touched="false"
              aria-label="Overall workout effort"
              aria-valuemin="1" aria-valuemax="10" aria-valuetext="Not rated" />
            <span class="effort-modal__band" id="overall-effort-band"></span>
          </div>
          <div class="effort-modal__actions">
            <button class="effort-modal__save" type="button" id="effort-modal-save">Save</button>
            <button class="effort-modal__skip" type="button" id="effort-modal-skip">Skip</button>
          </div>
          <button class="edit-modal__close" id="effort-modal-close" type="button" aria-label="Close">&#x2715;</button>
        </div>
      </div>
    </div>
  `;

  initEventListeners();
  initDiscardModal();
  initEffortModal();
  document.getElementById("draft-retry")?.addEventListener("click", () => {
    if (draft?.completion) void handleSave(draft.completion.overallEffort);
    else saveDraft();
  });
  await loadWorkout(workoutId, () => active, container);
}

function offerDeletedCleanup(target: HTMLElement): void {
  const clear = document.createElement("button");
  clear.type = "button";
  clear.className = "active-session__cancel-btn";
  clear.textContent = "Clear deleted completion";
  clear.addEventListener("click", () => {
    if (!window.confirm("Clear this saved completion? Its history entry was already deleted.")) return;
    try {
      removeDraft();
      navigate("/");
    } catch {
      target.textContent = "Could not clear the saved completion. Check storage and retry.";
      offerDeletedCleanup(target);
    }
  });
  target.appendChild(clear);
}

function showRecovery(container: HTMLElement, message: string, canClear: boolean, existing?: ActiveWorkoutDraft): void {
  container.innerHTML = "";
  const section = document.createElement("div");
  section.className = "active-session";
  const text = document.createElement("p");
  text.setAttribute("role", "alert");
  text.textContent = message;
  section.appendChild(text);
  if (existing && existing.workoutId !== new URLSearchParams(window.location.search).get("id")) {
    const resume = document.createElement("button");
    resume.type = "button";
    resume.className = "active-session__save-btn";
    resume.textContent = "Resume saved workout";
    resume.addEventListener("click", () => navigate(`/active-session?id=${encodeURIComponent(existing.workoutId)}&sets=${existing.sets}`));
    section.appendChild(resume);
  }
  if (canClear && !existing?.completion) {
    const clear = document.createElement("button");
    clear.type = "button";
    clear.className = "active-session__cancel-btn";
    clear.textContent = "Discard saved workout";
    clear.addEventListener("click", () => {
      if (!window.confirm("Discard the saved workout? This cannot be undone.")) return;
      try {
        removeDraft();
        window.location.reload();
      } catch {
        text.textContent = "Could not discard the saved workout. Check storage and try again.";
      }
    });
    section.appendChild(clear);
  }
  const retry = document.createElement("button");
  retry.type = "button";
  retry.className = "active-session__cancel-btn";
  retry.textContent = "Retry";
  retry.addEventListener("click", () => window.location.reload());
  section.appendChild(retry);
  container.appendChild(section);
}

function saveDraft(): boolean {
  if (!draft || !workout || draft.completion) return false;
  const entries = Object.fromEntries([...logEntries].map(([id, entry]) => [id, { ...entry }]));
  const updated: ActiveWorkoutDraft = {
    ...draft,
    exerciseOrder: (orderBeforeEditing ?? workout.exercises).map(exercise => exercise.exerciseId),
    entries,
  };
  const status = document.getElementById("draft-status");
  const retry = document.getElementById("draft-retry") as HTMLButtonElement | null;
  try {
    writeDraft(updated);
    draft = updated;
    if (status) status.textContent = "Progress saved on this browser.";
    if (retry) retry.style.display = "none";
    return true;
  } catch {
    if (status) status.textContent = "Progress could not be saved and may be lost on refresh.";
    if (retry) retry.style.display = "";
    return false;
  }
}

function initEventListeners(): void {
  const saveBtn = document.getElementById("session-save") as HTMLButtonElement | null;
  const cancelBtn = document.getElementById("session-cancel") as HTMLButtonElement | null;
  const editOrderBtn = document.getElementById("session-edit-order") as HTMLButtonElement | null;

  saveBtn?.addEventListener("click", () => {
    if (isOrderEditing) {
      finishOrderEditing();
    } else {
      openEffortModal();
    }
  });

  cancelBtn?.addEventListener("click", () => {
    handleCancel();
  });

  editOrderBtn?.addEventListener("click", () => {
    toggleOrderEditing();
  });
}

function initDiscardModal(): void {
  const backdrop = document.getElementById("discard-backdrop") as HTMLElement | null;
  const confirmBtn = document.getElementById("discard-confirm") as HTMLButtonElement | null;
  const cancelBtn = document.getElementById("discard-cancel") as HTMLButtonElement | null;

  if (!backdrop) return;

  confirmBtn?.addEventListener("click", () => {
    if (isOrderEditing) {
      discardOrderChanges();
    } else {
      try {
        removeDraft();
        navigate("/workouts");
      } catch {
        const error = document.getElementById("session-error");
        if (error) error.textContent = "Could not discard the workout. Check storage and try again.";
        closeDiscardModal();
      }
    }
  });

  cancelBtn?.addEventListener("click", () => {
    closeDiscardModal();
  });

  backdrop.addEventListener("click", (event: Event) => {
    if (event.target === backdrop) {
      closeDiscardModal();
    }
  });

  backdrop.addEventListener("keydown", (event: KeyboardEvent) => {
    if (event.key === "Escape") {
      closeDiscardModal();
      return;
    }

    // Focus trapping
    if (event.key === "Tab") {
      const modal = backdrop.querySelector(".discard-modal") as HTMLElement | null;
      if (!modal) return;

      const focusable = modal.querySelectorAll<HTMLElement>(MODAL_FOCUSABLE_SELECTOR);
      if (focusable.length === 0) return;

      const first = focusable[0];
      const last = focusable[focusable.length - 1];

      if (event.shiftKey) {
        if (document.activeElement === first) {
          event.preventDefault();
          last.focus();
        }
      } else {
        if (document.activeElement === last) {
          event.preventDefault();
          first.focus();
        }
      }
    }
  });
}

function initEffortModal(): void {
  const backdrop = document.getElementById("effort-backdrop") as HTMLElement | null;
  const saveBtn = document.getElementById("effort-modal-save") as HTMLButtonElement | null;
  const skipBtn = document.getElementById("effort-modal-skip") as HTMLButtonElement | null;
  const closeBtn = document.getElementById("effort-modal-close") as HTMLButtonElement | null;
  const slider = document.getElementById("overall-effort-slider") as HTMLInputElement | null;

  if (!backdrop) return;

  saveBtn?.addEventListener("click", () => {
    handleEffortSave();
  });

  skipBtn?.addEventListener("click", () => {
    handleEffortSkip();
  });

  closeBtn?.addEventListener("click", () => {
    closeEffortModal();
  });

  slider?.addEventListener("input", () => {
    handleEffortSliderInput();
  });

  backdrop.addEventListener("click", (event: Event) => {
    if (event.target === backdrop) {
      closeEffortModal();
    }
  });

  backdrop.addEventListener("keydown", (event: KeyboardEvent) => {
    if (event.key === "Escape") {
      handleEffortSkip();
      return;
    }

    if (event.key === "Tab") {
      const modal = backdrop.querySelector(".effort-modal") as HTMLElement | null;
      if (!modal) return;

      const focusable = modal.querySelectorAll<HTMLElement>(MODAL_FOCUSABLE_SELECTOR);
      if (focusable.length === 0) return;

      const first = focusable[0];
      const last = focusable[focusable.length - 1];

      if (event.shiftKey) {
        if (document.activeElement === first) {
          event.preventDefault();
          last.focus();
        }
      } else {
        if (document.activeElement === last) {
          event.preventDefault();
          first.focus();
        }
      }
    }
  });
}

function openEffortModal(): void {
  const backdrop = document.getElementById("effort-backdrop") as HTMLElement | null;
  const slider = document.getElementById("overall-effort-slider") as HTMLInputElement | null;
  const valueEl = document.getElementById("overall-effort-value") as HTMLElement | null;
  const bandEl = document.getElementById("overall-effort-band") as HTMLElement | null;
  const saveBtn = document.getElementById("effort-modal-save") as HTMLButtonElement | null;

  if (!backdrop) return;

  // Reset slider state
  pendingOverallEffort = null;
  if (slider) {
    slider.setAttribute("data-touched", "false");
    slider.value = "1";
    slider.removeAttribute("aria-valuenow");
    slider.setAttribute("aria-valuetext", "Not rated");
    slider.style.accentColor = "";
  }
  if (valueEl) {
    valueEl.textContent = "Not rated";
    valueEl.style.color = "";
  }
  if (bandEl) {
    bandEl.textContent = "";
    bandEl.style.color = "";
  }

  backdrop.style.display = "";
  saveBtn?.focus();
}

function closeEffortModal(): void {
  const backdrop = document.getElementById("effort-backdrop") as HTMLElement | null;
  if (backdrop) backdrop.style.display = "none";
}

function handleEffortSliderInput(): void {
  const slider = document.getElementById("overall-effort-slider") as HTMLInputElement | null;
  const valueEl = document.getElementById("overall-effort-value") as HTMLElement | null;
  const bandEl = document.getElementById("overall-effort-band") as HTMLElement | null;

  if (!slider) return;

  const value = parseInt(slider.value, 10);
  pendingOverallEffort = value;

  slider.setAttribute("data-touched", "true");
  slider.setAttribute("aria-valuenow", String(value));
  const label = getEffortLabel(value);
  slider.setAttribute("aria-valuetext", `${value}, ${label}`);

  if (valueEl) valueEl.textContent = String(value);
  if (bandEl) bandEl.textContent = label;
  const colour = getEffortColour(value);
  slider.style.accentColor = colour;
  if (valueEl) valueEl.style.color = colour;
  if (bandEl) bandEl.style.color = colour;
}

function handleEffortSave(): void {
  const slider = document.getElementById("overall-effort-slider") as HTMLInputElement | null;
  const effort = slider?.getAttribute("data-touched") === "true" ? pendingOverallEffort : null;
  closeEffortModal();
  void handleSave(effort);
}

function handleEffortSkip(): void {
  closeEffortModal();
  void handleSave(null);
}

function openDiscardModal(): void {
  const backdrop = document.getElementById("discard-backdrop") as HTMLElement | null;
  const confirmBtn = document.getElementById("discard-confirm") as HTMLButtonElement | null;
  const titleEl = document.getElementById("discard-title") as HTMLElement | null;
  const descEl = document.getElementById("discard-desc") as HTMLElement | null;
  const continueBtn = document.getElementById("discard-cancel") as HTMLButtonElement | null;
  if (!backdrop) return;

  if (isOrderEditing) {
    if (titleEl) titleEl.textContent = "Discard changes?";
    if (descEl) descEl.textContent = "You have unsaved order changes. Are you sure you want to discard them?";
    if (continueBtn) continueBtn.textContent = "Keep editing";
  } else {
    if (titleEl) titleEl.textContent = "Discard Workout?";
    if (descEl) descEl.textContent = "Discard this workout and its saved progress?";
    if (continueBtn) continueBtn.textContent = "Continue workout";
  }

  backdrop.style.display = "";
  confirmBtn?.focus();
}

function closeDiscardModal(): void {
  const backdrop = document.getElementById("discard-backdrop") as HTMLElement | null;
  if (backdrop) backdrop.style.display = "none";
}

async function loadWorkout(workoutId: string, isActive: () => boolean, container: HTMLElement): Promise<void> {
  const titleEl = document.getElementById("session-title") as HTMLElement | null;
  const errorEl = document.getElementById("session-error") as HTMLElement | null;

  const [workoutResult, prevResult] = await Promise.allSettled([
    fetch(`/api/workouts/${workoutId}`),
    fetch(`/api/workouts/${workoutId}/previous-performance`),
  ]);
  if (!isActive()) return;

  // Handle workout fetch failure (blocks the whole page)
  if (workoutResult.status === "rejected" || !workoutResult.value.ok) {
    if (draft) {
      showRecovery(container, "The saved workout could not be loaded. Retry or explicitly discard it.", true, draft);
      return;
    }
    if (titleEl) titleEl.textContent = "Workout";
    if (errorEl) errorEl.textContent = "Failed to load workout. Please try again.";
    return;
  }

  try {
    workout = await workoutResult.value.json();
  } catch {
    if (titleEl) titleEl.textContent = "Workout";
    if (errorEl) errorEl.textContent = "Failed to load workout. Please try again.";
    return;
  }
  if (!isActive()) return;
  if (workout && draft) {
    const ids = new Set(workout.exercises.map(exercise => exercise.exerciseId));
    if (ids.size !== draft.exerciseOrder.length || !draft.exerciseOrder.every(id => ids.has(id))) {
      showRecovery(container, "The workout exercises have changed. The saved progress cannot be restored safely.", true, draft);
      return;
    }
  }

  // Apply shuffled order if provided via ?order= URL param (T011).
  // This reorders workout.exercises in memory only — PlannedWorkoutExercise.Sequence is never modified.
  if (workout !== null && exerciseOrder !== null) {
    workout = { ...workout, exercises: applyOrder(workout.exercises, exerciseOrder) };
  }
  if (titleEl && workout) {
    titleEl.textContent = workout.name;
  }
  updateEditOrderButton();

  // Determine previous-performance data to pass to renderer
  let previousData: Map<string, PreviousExerciseData> | "error" | null = null;

  if (prevResult.status === "fulfilled" && prevResult.value.ok) {
    try {
      const perf: PreviousPerformance = await prevResult.value.json();
      if (perf.hasPreviousSession) {
        previousData = new Map(perf.exercises.map((e) => [e.exerciseId, e]));
      }
      // hasPreviousSession === false → previousData stays null (first-session state)
    } catch {
      previousData = "error";
    }
  } else {
    previousData = "error";
  }

  if (!isActive()) return;
  currentPreviousData = previousData;
  renderExerciseInputs();
  if (!draft && workout && sessionSets !== null) {
    draft = {
      version: 1, draftId: crypto.randomUUID(), workoutId, sets: sessionSets,
      exerciseOrder: workout.exercises.map(exercise => exercise.exerciseId),
      entries: Object.fromEntries([...logEntries].map(([id, entry]) => [id, { ...entry }])),
      completion: null,
    };
    saveDraft();
  } else if (draft?.completion) {
    showPendingCompletion("A previous completion may have succeeded. Retry to reconcile it.");
  }
}

function renderExerciseInputs(): void {
  const exercisesEl = document.getElementById("session-exercises") as HTMLElement | null;
  if (!exercisesEl || !workout) return;

  exercisesEl.innerHTML = "";
  const previousLogEntries = logEntries;
  logEntries = new Map();

  for (const exercise of workout.exercises) {
    const existingEntry = previousLogEntries.get(exercise.exerciseId);
    logEntries.set(exercise.exerciseId, {
      loggedWeight: existingEntry?.loggedWeight ?? "",
      loggedEffort: existingEntry?.loggedEffort ?? null,
    });

    const item = document.createElement("div");
    item.className = "active-session__exercise-item";
    item.setAttribute("data-exercise-id", exercise.exerciseId);

    const nameDiv = document.createElement("div");
    nameDiv.className = "active-session__exercise-name";
    nameDiv.textContent = exercise.name;
    item.appendChild(nameDiv);

    const targets: string[] = [];
    if (exercise.targetWeight) targets.push(`@ ${exercise.targetWeight} KG`);

    if (targets.length > 0) {
      const targetsDiv = document.createElement("div");
      targetsDiv.className = "active-session__exercise-targets";
      targetsDiv.textContent = targets.join(" ");
      item.appendChild(targetsDiv);
    }

    // Previous performance display (inserted before inputs)
    const previousDiv = document.createElement("div");
    previousDiv.className = "active-session__exercise-previous";
    previousDiv.id = `previous-${exercise.exerciseId}`;

    if (currentPreviousData === "error") {
      const errorSpan = document.createElement("span");
      errorSpan.className = "active-session__previous-error";
      errorSpan.textContent = "Could not load previous data";
      previousDiv.appendChild(errorSpan);
    } else {
      const entry = currentPreviousData !== null ? currentPreviousData.get(exercise.exerciseId) : undefined;

      if (entry !== undefined) {
        // Build value string from non-null fields
        const parts: string[] = [];
        if (entry.loggedWeight !== null) parts.push(`${entry.loggedWeight} KG`);
        if (entry.sets === 3 || entry.sets === 5) parts.push(`${entry.sets} sets`);
        if (entry.effort !== null) parts.push(`${entry.effort} — ${getEffortLabel(entry.effort)}`);
        if (parts.length > 0 && entry.sequence !== null) parts.unshift(`#${entry.sequence + 1}`);

        if (parts.length > 0) {
          const labelSpan = document.createElement("span");
          labelSpan.className = "active-session__previous-label";
          labelSpan.textContent = "Last time:";

          const valueSpan = document.createElement("span");
          valueSpan.className = "active-session__previous-value";
          valueSpan.textContent = parts.join(" · ");

          previousDiv.appendChild(labelSpan);
          previousDiv.appendChild(valueSpan);
        } else {
          // All fields null (sequence, weight, and effort) — treat same as first-session
          const emptySpan = document.createElement("span");
          emptySpan.className = "active-session__previous-empty";
          emptySpan.textContent = "First session — no previous data";
          previousDiv.appendChild(emptySpan);
        }
      } else {
        // State 1: no session, or no map entry for this exercise
        const emptySpan = document.createElement("span");
        emptySpan.className = "active-session__previous-empty";
        emptySpan.textContent = "First session — no previous data";
        previousDiv.appendChild(emptySpan);
      }
    }

    item.appendChild(previousDiv);

    const inputsDiv = document.createElement("div");
    inputsDiv.className = "active-session__exercise-inputs";

    // Weight input (KG)
    const weightGroup = document.createElement("div");
    weightGroup.className = "active-session__input-group";
    const weightLabel = document.createElement("label");
    weightLabel.className = "active-session__input-label";
    weightLabel.setAttribute("for", `weight-${exercise.exerciseId}`);
    weightLabel.textContent = "Weight (KG)";
    const weightInput = document.createElement("input");
    weightInput.className = "active-session__input";
    weightInput.type = "number";
    weightInput.id = `weight-${exercise.exerciseId}`;
    weightInput.placeholder = "KG";
    weightInput.min = "0";
    weightInput.step = "0.5";
    weightInput.value = existingEntry?.loggedWeight ?? "";
    weightInput.setAttribute("aria-label", `Weight in KG for ${exercise.name}`);
    weightInput.addEventListener("input", () => {
      const entry = logEntries.get(exercise.exerciseId);
      if (entry) entry.loggedWeight = weightInput.value;
      saveDraft();
    });
    weightGroup.appendChild(weightLabel);
    weightGroup.appendChild(weightInput);
    inputsDiv.appendChild(weightGroup);

    // Notes input removed — not in use

    // Effort slider
    const effortGroup = document.createElement("div");
    effortGroup.className = "active-session__input-group active-session__effort-group";

    const effortLabel = document.createElement("label");
    effortLabel.className = "active-session__input-label";
    effortLabel.setAttribute("for", `effort-${exercise.exerciseId}`);
    effortLabel.textContent = "Effort";

    const effortValueEl = document.createElement("span");
    effortValueEl.className = "active-session__effort-value";
    effortValueEl.id = `effort-value-${exercise.exerciseId}`;
    effortValueEl.textContent = "Not rated";

    const effortSlider = document.createElement("input");
    effortSlider.className = "active-session__effort-slider";
    effortSlider.type = "range";
    effortSlider.id = `effort-${exercise.exerciseId}`;
    effortSlider.min = "1";
    effortSlider.max = "10";
    effortSlider.step = "1";
    effortSlider.setAttribute("data-touched", "false");
    effortSlider.setAttribute("aria-label", `Effort for ${exercise.name}`);
    effortSlider.setAttribute("aria-valuemin", "1");
    effortSlider.setAttribute("aria-valuemax", "10");
    effortSlider.setAttribute("aria-valuetext", "Not rated");
    // Set value=1 so slider renders at the left; aria-valuenow stays absent until touched
    effortSlider.value = "1";
    effortSlider.removeAttribute("aria-valuenow");

    const effortBandEl = document.createElement("span");
    effortBandEl.className = "active-session__effort-band";
    effortBandEl.id = `effort-band-${exercise.exerciseId}`;

    if (existingEntry && existingEntry.loggedEffort !== null) {
      const restored = existingEntry.loggedEffort;
      effortSlider.value = String(restored);
      effortSlider.setAttribute("data-touched", "true");
      effortSlider.setAttribute("aria-valuenow", String(restored));
      const restoredLabel = getEffortLabel(restored);
      effortSlider.setAttribute("aria-valuetext", `${restored}, ${restoredLabel}`);
      effortValueEl.textContent = String(restored);
      effortBandEl.textContent = restoredLabel;
      const restoredColour = getEffortColour(restored);
      effortSlider.style.accentColor = restoredColour;
      effortValueEl.style.color = restoredColour;
      effortBandEl.style.color = restoredColour;
    }

    effortSlider.addEventListener("input", () => {
      const value = parseInt(effortSlider.value, 10);
      const entry = logEntries.get(exercise.exerciseId);
      if (entry) entry.loggedEffort = value;

      if (effortSlider.getAttribute("data-touched") === "false") {
        effortSlider.setAttribute("data-touched", "true");
      }
      effortSlider.setAttribute("aria-valuenow", String(value));
      const label = getEffortLabel(value);
      effortSlider.setAttribute("aria-valuetext", `${value}, ${label}`);

      effortValueEl.textContent = String(value);
      effortBandEl.textContent = label;
      const colour = getEffortColour(value);
      effortSlider.style.accentColor = colour;
      effortValueEl.style.color = colour;
      effortBandEl.style.color = colour;
      saveDraft();
    });

    effortGroup.appendChild(effortLabel);
    effortGroup.appendChild(effortValueEl);
    effortGroup.appendChild(effortSlider);
    effortGroup.appendChild(effortBandEl);
    inputsDiv.appendChild(effortGroup);

    item.appendChild(inputsDiv);
    exercisesEl.appendChild(item);
  }
}

function toggleOrderEditing(): void {
  if (!workout || workout.exercises.length === 0) return;
  if (isOrderEditing) return;

  orderBeforeEditing = [...workout.exercises];
  hasOrderChanges = false;
  isOrderEditing = true;
  updateOrderEditingControls();
  renderOrderEditor();
  const firstHandle = document.querySelector<HTMLButtonElement>("#session-order-list .workout-selected__drag-handle");
  const doneBtn = document.getElementById("session-save");
  (firstHandle ?? doneBtn)?.focus();
}

function finishOrderEditing(commit = true): void {
  if (!isOrderEditing) return;

  isOrderEditing = false;
  orderBeforeEditing = null;
  hasOrderChanges = false;
  updateOrderEditingControls();
  renderExerciseInputs();
  if (commit) saveDraft();
}

function discardOrderChanges(): void {
  if (!workout || !orderBeforeEditing) return;

  workout = { ...workout, exercises: [...orderBeforeEditing] };
  closeDiscardModal();
  finishOrderEditing(false);
}

function hasCurrentOrderChanged(): boolean {
  if (!workout || !orderBeforeEditing) return false;
  const originalOrder = orderBeforeEditing;
  return workout.exercises.some((exercise, index) => exercise.exerciseId !== originalOrder[index]?.exerciseId);
}

function updateEditOrderButton(): void {
  const btn = document.getElementById("session-edit-order") as HTMLButtonElement | null;
  if (!btn || !workout) return;

  btn.style.display = workout.exercises.length > 0 && !isOrderEditing ? "" : "none";
  btn.disabled = workout.exercises.length === 0 || isOrderEditing;
}

function updateOrderEditingControls(): void {
  const saveBtn = document.getElementById("session-save") as HTMLButtonElement | null;
  const cancelBtn = document.getElementById("session-cancel") as HTMLButtonElement | null;
  updateEditOrderButton();

  if (saveBtn) {
    saveBtn.textContent = isOrderEditing ? "Done" : "Save Workout";
  }

  if (cancelBtn) {
    cancelBtn.textContent = "Cancel";
  }
}

function renderOrderEditor(): void {
  const exercisesEl = document.getElementById("session-exercises") as HTMLElement | null;
  if (!exercisesEl || !workout) return;

  exercisesEl.innerHTML = "";

  const announce = document.createElement("div");
  announce.className = "sr-only";
  announce.id = "session-reorder-announce";
  announce.setAttribute("aria-live", "polite");
  announce.setAttribute("aria-atomic", "true");
  exercisesEl.appendChild(announce);

  const list = document.createElement("ul");
  list.className = "workout-selected__list active-session__order-list";
  list.id = "session-order-list";
  exercisesEl.appendChild(list);

  for (const [index, exercise] of workout.exercises.entries()) {
    list.appendChild(buildOrderExerciseItem(exercise, index, workout.exercises.length));
  }

  initSortableList({
    listId: "session-order-list",
    announceId: "session-reorder-announce",
    getArray: () => workout?.exercises ?? [],
    onReorder: () => {
      hasOrderChanges = hasCurrentOrderChanged();
      renderOrderEditor();
    },
  });
}

function buildOrderExerciseItem(exercise: WorkoutExercise, index: number, listLength: number): HTMLLIElement {
  const li = document.createElement("li");
  li.className = "workout-selected__item active-session__order-item";
  li.setAttribute("data-exercise-id", exercise.exerciseId);
  li.setAttribute("data-index", String(index));

  if (listLength >= 2) {
    li.setAttribute("draggable", "true");
    li.setAttribute("aria-roledescription", "sortable item");

    const handle = document.createElement("button");
    handle.type = "button";
    handle.className = "workout-selected__drag-handle";
    handle.setAttribute("aria-label", `Drag to reorder ${exercise.name}`);
    handle.setAttribute("aria-pressed", "false");
    handle.innerHTML = `<svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><circle cx="5" cy="3" r="1.5"/><circle cx="5" cy="8" r="1.5"/><circle cx="5" cy="13" r="1.5"/><circle cx="11" cy="3" r="1.5"/><circle cx="11" cy="8" r="1.5"/><circle cx="11" cy="13" r="1.5"/></svg>`;
    li.appendChild(handle);
  }

  const name = document.createElement("span");
  name.className = "workout-selected__name active-session__order-name";
  name.textContent = exercise.name;
  li.appendChild(name);

  return li;
}

async function handleSave(overallEffort: number | null): Promise<void> {
  if (isSaving || !workout) return;
  const generation = renderGeneration;

  const saveBtn = document.getElementById("session-save") as HTMLButtonElement | null;
  const apiErrorEl = document.getElementById("session-api-error") as HTMLElement | null;

  if (!saveBtn) return;

  if (apiErrorEl) apiErrorEl.textContent = "";
  if (sessionSets === null) {
    if (apiErrorEl) apiErrorEl.textContent = "Sets must be 3 or 5. Return to Let's go! and select sets.";
    return;
  }
  if (!draft) {
    if (apiErrorEl) apiErrorEl.textContent = "The workout draft is not ready. Retry loading this workout.";
    return;
  }
  if (!draft.completion && !saveDraft()) return;

  // Client-side weight validation
  const exercisesEl = document.getElementById("session-exercises");
  if (exercisesEl && workout) {
    for (const exercise of workout.exercises) {
      const weightInput = document.getElementById(`weight-${exercise.exerciseId}`) as HTMLInputElement | null;
      if (weightInput && weightInput.value !== "" && weightInput.validity.badInput) {
        const errorEl = document.getElementById("session-error");
        if (errorEl) errorEl.textContent = `Weight for ${exercise.name} must be a valid number.`;
        return;
      }
    }
  }

  isSaving = true;
  saveBtn.setAttribute("aria-disabled", "true");
  const originalText = saveBtn.textContent;
  saveBtn.textContent = "Saving...";

  try {
    // Records the actual display position (sequence) for each exercise.
    // Note: this POST targets /sessions only — no PUT/PATCH to any workout template
    // endpoint is made here or anywhere in the session flow (T021, US3).
    const loggedExercises = workout.exercises.map((exercise, index) => {
      const entry = logEntries.get(exercise.exerciseId);
      const weightStr = entry?.loggedWeight ?? "";
      return {
        exerciseId: exercise.exerciseId,
        loggedWeight: weightStr !== "" ? weightStr : null,
        effort: entry?.loggedEffort ?? null,
        sequence: index,
      };
    });

    let payload: CompletionPayload = draft.completion ?? {
      completionOperationId: draft.draftId,
      sets: sessionSets,
      loggedExercises,
      overallEffort,
    };
    if (!draft.completion) {
      const frozen = { ...draft, completion: payload };
      try {
        writeDraft(frozen);
        draft = frozen;
      } catch {
        if (apiErrorEl) apiErrorEl.textContent = "Could not save completion safely. Check browser storage and retry.";
        return;
      }
    } else {
      payload = draft.completion;
    }
    showPendingCompletion("Completing workout...");
    const response = await fetch(`/api/workouts/${workout.plannedWorkoutId}/sessions`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });
    if (generation !== renderGeneration) return;

    if (response.ok) {
      const session: SessionCreateResponse = await response.json();
      if (generation !== renderGeneration) return;
      if (typeof session.workoutSessionId !== "string" || !session.workoutSessionId) {
        showPendingCompletion("The completion response was invalid. Retry to check its result.");
        return;
      }
      try {
        removeDraft();
      } catch {
        showPendingCompletion("Workout completed, but its saved draft could not be removed. Retry cleanup.");
        return;
      }
      navigate(`/history/session?id=${encodeURIComponent(session.workoutSessionId)}`);
    } else {
      const data = await response.json();
      if (generation !== renderGeneration) return;
      if (apiErrorEl) {
        apiErrorEl.textContent = data.error || "An unexpected error occurred. Please try again.";
      }
      if ((response.status === 400 || response.status === 404) && data.safeToEdit === true && draft) {
        try {
          const editable = { ...draft, completion: null };
          writeDraft(editable);
          draft = editable;
          enableEditing();
          const status = document.getElementById("draft-status");
          if (status) status.textContent = "The rejected attempt is safely blocked. You can edit or discard the workout.";
        } catch {
          showPendingCompletion("The rejection was confirmed, but the draft could not be updated. Retry.");
        }
      } else {
        showPendingCompletion(response.status === 400
          ? "Completion was rejected, but an earlier attempt may still be in flight. Your saved progress remains locked."
          : response.status === 409 || response.status === 410
          ? "This completion cannot be retried as a new workout. Your saved progress remains intact."
          : "Completion may have succeeded. Retry to check its result.");
        if (response.status === 410 && apiErrorEl) offerDeletedCleanup(apiErrorEl);
        if (response.status === 409 && typeof data.workoutSessionId === "string" && apiErrorEl) {
          const link = document.createElement("a");
          link.href = `/history/session?id=${encodeURIComponent(data.workoutSessionId)}`;
          link.textContent = " View completed workout";
          apiErrorEl.appendChild(link);
        }
      }
    }
  } catch {
    if (generation === renderGeneration) showPendingCompletion("Completion may have succeeded. Retry to check its result.");
  } finally {
    isSaving = false;
    saveBtn.removeAttribute("aria-disabled");
    saveBtn.textContent = originalText ?? "Save Workout";
  }
}

function showPendingCompletion(message: string): void {
  const status = document.getElementById("draft-status");
  if (status) status.textContent = message;
  const retry = document.getElementById("draft-retry") as HTMLButtonElement | null;
  if (retry) {
    retry.style.display = "";
    retry.textContent = "Retry completion";
  }
  document.querySelectorAll<HTMLInputElement | HTMLButtonElement>(
    "#session-exercises input, #session-save, #session-cancel, #session-edit-order",
  ).forEach(element => { element.disabled = true; });
}

function enableEditing(): void {
  document.querySelectorAll<HTMLInputElement | HTMLButtonElement>(
    "#session-exercises input, #session-save, #session-cancel, #session-edit-order",
  ).forEach(element => { element.disabled = false; });
  const retry = document.getElementById("draft-retry");
  if (retry) retry.style.display = "none";
}

function handleCancel(): void {
  if (isOrderEditing) {
    if (hasOrderChanges) {
      openDiscardModal();
    } else {
      finishOrderEditing();
    }
    return;
  }

  if (draft) {
    openDiscardModal();
  } else {
    navigate("/workouts");
  }
}

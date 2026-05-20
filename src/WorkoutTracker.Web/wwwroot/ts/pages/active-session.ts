import { navigate } from "../router.js";
import { getEffortLabel, getEffortColour, applyOrder } from "../utils.js";

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
}

interface PreviousPerformance {
  readonly hasPreviousSession: boolean;
  readonly completedAt: string | null;
  readonly exercises: PreviousExerciseData[];
}

let workout: WorkoutDetail | null = null;
let logEntries: Map<string, LogEntry> = new Map();
let isSaving = false;
let hasChanges = false;
let exerciseOrder: string[] | null = null;
let pendingOverallEffort: number | null = null;
const MODAL_FOCUSABLE_SELECTOR =
  'button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), a[href], [contenteditable="true"], [tabindex]:not([tabindex="-1"])';

export async function render(container: HTMLElement): Promise<void> {
  const params = new URLSearchParams(window.location.search);
  const workoutId = params.get("id");

  if (!workoutId) {
    container.innerHTML = `
      <div class="active-session">
        <p>No workout selected. <a href="/workouts">Go to Workouts</a></p>
      </div>
    `;
    return;
  }

  // Reset state
  workout = null;
  logEntries = new Map();
  isSaving = false;
  hasChanges = false;
  exerciseOrder = null;
  pendingOverallEffort = null;

  const orderParam = params.get("order");
  exerciseOrder = orderParam ? orderParam.split(",").map(id => id.trim()).filter(id => id.length > 0) : null;

  container.innerHTML = `
    <div class="active-session">
      <div class="active-session__header">
        <h1 class="active-session__title" id="session-title">Loading...</h1>
      </div>
      <div class="active-session__exercises" id="session-exercises" role="form" aria-label="Log workout exercises"></div>
      <div class="active-session__error" id="session-error" role="alert" aria-live="polite"></div>
      <div class="active-session__api-error" id="session-api-error" role="alert" aria-live="polite"></div>
      <div class="active-session__actions">
        <button class="active-session__save-btn" type="button" id="session-save">Save Workout</button>
        <button class="active-session__cancel-btn" type="button" id="session-cancel">Cancel</button>
      </div>
      <div class="discard-modal-backdrop" id="discard-backdrop" style="display:none;">
        <div class="discard-modal" role="alertdialog" aria-modal="true" aria-labelledby="discard-title" aria-describedby="discard-desc">
          <h2 class="discard-modal__title" id="discard-title">Discard Workout?</h2>
          <p class="discard-modal__desc" id="discard-desc">You have unsaved changes. Are you sure you want to discard this workout?</p>
          <div class="discard-modal__actions">
            <button class="discard-modal__discard" type="button" id="discard-confirm">Discard</button>
            <button class="discard-modal__continue" type="button" id="discard-cancel">Continue</button>
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
        </div>
      </div>
    </div>
  `;

  initEventListeners();
  initDiscardModal();
  initEffortModal();
  await loadWorkout(workoutId);
}

function initEventListeners(): void {
  const saveBtn = document.getElementById("session-save") as HTMLButtonElement | null;
  const cancelBtn = document.getElementById("session-cancel") as HTMLButtonElement | null;

  saveBtn?.addEventListener("click", () => {
    openEffortModal();
  });

  cancelBtn?.addEventListener("click", () => {
    handleCancel();
  });
}

function initDiscardModal(): void {
  const backdrop = document.getElementById("discard-backdrop") as HTMLElement | null;
  const confirmBtn = document.getElementById("discard-confirm") as HTMLButtonElement | null;
  const cancelBtn = document.getElementById("discard-cancel") as HTMLButtonElement | null;

  if (!backdrop) return;

  confirmBtn?.addEventListener("click", () => {
    navigate("/workouts");
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
  const slider = document.getElementById("overall-effort-slider") as HTMLInputElement | null;

  if (!backdrop) return;

  saveBtn?.addEventListener("click", () => {
    handleEffortSave();
  });

  skipBtn?.addEventListener("click", () => {
    handleEffortSkip();
  });

  slider?.addEventListener("input", () => {
    handleEffortSliderInput();
  });

  backdrop.addEventListener("click", (event: Event) => {
    if (event.target === backdrop) {
      handleEffortSkip();
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
  if (!backdrop) return;

  backdrop.style.display = "";
  confirmBtn?.focus();
}

function closeDiscardModal(): void {
  const backdrop = document.getElementById("discard-backdrop") as HTMLElement | null;
  if (backdrop) backdrop.style.display = "none";
}

async function loadWorkout(workoutId: string): Promise<void> {
  const titleEl = document.getElementById("session-title") as HTMLElement | null;
  const errorEl = document.getElementById("session-error") as HTMLElement | null;

  const [workoutResult, prevResult] = await Promise.allSettled([
    fetch(`/api/workouts/${workoutId}`),
    fetch(`/api/workouts/${workoutId}/previous-performance`),
  ]);

  // Handle workout fetch failure (blocks the whole page)
  if (workoutResult.status === "rejected" || !workoutResult.value.ok) {
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

  // Apply shuffled order if provided via ?order= URL param (T011).
  // This reorders workout.exercises in memory only — PlannedWorkoutExercise.Sequence is never modified.
  if (workout !== null && exerciseOrder !== null) {
    workout = { ...workout, exercises: applyOrder(workout.exercises, exerciseOrder) };
  }
  if (titleEl && workout) {
    titleEl.textContent = workout.name;
  }

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

  renderExerciseInputs(previousData);
}

function renderExerciseInputs(previousData: Map<string, PreviousExerciseData> | "error" | null): void {
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

    if (previousData === "error") {
      const errorSpan = document.createElement("span");
      errorSpan.className = "active-session__previous-error";
      errorSpan.textContent = "Could not load previous data";
      previousDiv.appendChild(errorSpan);
    } else {
      const entry = previousData !== null ? previousData.get(exercise.exerciseId) : undefined;

      if (entry !== undefined) {
        // Build value string from non-null fields
        const parts: string[] = [];
        if (entry.sequence !== null) parts.push(`#${entry.sequence + 1}`);
        if (entry.loggedWeight !== null) parts.push(`${entry.loggedWeight} KG`);
        if (entry.effort !== null) parts.push(`${entry.effort} — ${getEffortLabel(entry.effort)}`);

        if (parts.length > 0) {
          // State 2/3/4/5: sequence, weight, effort, or any combination available
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
    weightInput.setAttribute("aria-label", `Weight in KG for ${exercise.name}`);
    weightInput.addEventListener("input", () => {
      hasChanges = true;
      const entry = logEntries.get(exercise.exerciseId);
      if (entry) entry.loggedWeight = weightInput.value;
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
      hasChanges = true;
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

async function handleSave(overallEffort: number | null): Promise<void> {
  if (isSaving || !workout) return;

  const saveBtn = document.getElementById("session-save") as HTMLButtonElement | null;
  const apiErrorEl = document.getElementById("session-api-error") as HTMLElement | null;

  if (!saveBtn) return;

  if (apiErrorEl) apiErrorEl.textContent = "";

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

    const response = await fetch(`/api/workouts/${workout.plannedWorkoutId}/sessions`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ loggedExercises, overallEffort }),
    });

    if (response.ok) {
      hasChanges = false;
      navigate("/history");
    } else {
      const data = await response.json();
      if (apiErrorEl) {
        apiErrorEl.textContent = data.error || "An unexpected error occurred. Please try again.";
      }
    }
  } catch {
    if (apiErrorEl) {
      apiErrorEl.textContent = "An unexpected error occurred. Please try again.";
    }
  } finally {
    isSaving = false;
    saveBtn.removeAttribute("aria-disabled");
    saveBtn.textContent = originalText ?? "Save Workout";
  }
}

function handleCancel(): void {
  if (hasChanges) {
    openDiscardModal();
  } else {
    navigate("/workouts");
  }
}

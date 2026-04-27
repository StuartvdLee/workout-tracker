import { navigate } from "../router.js";
import { getEffortLabel } from "../utils.js";

function effortColor(value: number): string {
  // Interpolate hue from 120 (green) at 1 to 0 (red) at 10
  const hue = Math.round(120 - ((value - 1) / 9) * 120);
  return `hsl(${hue}, 70%, 40%)`;
}

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

let workout: WorkoutDetail | null = null;
let logEntries: Map<string, LogEntry> = new Map();
let isSaving = false;
let hasChanges = false;

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
    </div>
  `;

  initEventListeners();
  initDiscardModal();
  await loadWorkout(workoutId);
}

function initEventListeners(): void {
  const saveBtn = document.getElementById("session-save") as HTMLButtonElement | null;
  const cancelBtn = document.getElementById("session-cancel") as HTMLButtonElement | null;

  saveBtn?.addEventListener("click", () => {
    void handleSave();
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

      const focusable = modal.querySelectorAll<HTMLElement>(
        'button:not([disabled]), [tabindex]:not([tabindex="-1"])'
      );
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

  try {
    const response = await fetch(`/api/workouts/${workoutId}`);

    if (response.ok) {
      workout = await response.json();
      if (titleEl && workout) {
        titleEl.textContent = workout.name;
      }
      renderExerciseInputs();
    } else {
      if (titleEl) titleEl.textContent = "Workout";
      if (errorEl) errorEl.textContent = "Failed to load workout. Please try again.";
    }
  } catch {
    if (titleEl) titleEl.textContent = "Workout";
    if (errorEl) errorEl.textContent = "An unexpected error occurred. Please try again.";
  }
}

function renderExerciseInputs(): void {
  const exercisesEl = document.getElementById("session-exercises") as HTMLElement | null;
  if (!exercisesEl || !workout) return;

  exercisesEl.innerHTML = "";
  logEntries = new Map();

  for (const exercise of workout.exercises) {
    logEntries.set(exercise.exerciseId, { loggedWeight: "", loggedEffort: null });

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
    effortValueEl.textContent = "—";

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
    // Set value=1 so slider renders at the left; aria-valuenow stays absent until touched
    effortSlider.value = "1";
    effortSlider.style.setProperty("--effort-color", effortColor(1));
    effortSlider.removeAttribute("aria-valuenow");

    const effortBandEl = document.createElement("span");
    effortBandEl.className = "active-session__effort-band";
    effortBandEl.id = `effort-band-${exercise.exerciseId}`;

    effortSlider.addEventListener("input", () => {
      hasChanges = true;
      const value = parseInt(effortSlider.value, 10);
      const entry = logEntries.get(exercise.exerciseId);
      if (entry) entry.loggedEffort = value;

      if (effortSlider.getAttribute("data-touched") === "false") {
        effortSlider.setAttribute("data-touched", "true");
      }
      effortSlider.setAttribute("aria-valuenow", String(value));
      effortSlider.style.setProperty("--effort-color", effortColor(value));

      effortValueEl.textContent = String(value);
      effortBandEl.textContent = getEffortLabel(value);
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

async function handleSave(): Promise<void> {
  if (isSaving || !workout) return;

  const saveBtn = document.getElementById("session-save") as HTMLButtonElement | null;
  const apiErrorEl = document.getElementById("session-api-error") as HTMLElement | null;

  if (!saveBtn) return;

  if (apiErrorEl) apiErrorEl.textContent = "";

  isSaving = true;
  saveBtn.setAttribute("aria-disabled", "true");
  const originalText = saveBtn.textContent;
  saveBtn.textContent = "Saving...";

  try {
    const loggedExercises = workout.exercises.map((exercise) => {
      const entry = logEntries.get(exercise.exerciseId);
      const weightStr = entry?.loggedWeight ?? "";
      return {
        exerciseId: exercise.exerciseId,
        loggedWeight: weightStr !== "" ? weightStr : null,
        effort: entry?.loggedEffort ?? null,
      };
    });

    const response = await fetch(`/api/workouts/${workout.plannedWorkoutId}/sessions`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ loggedExercises }),
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

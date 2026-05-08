import { navigate } from "../router.js";
import { renderPrestartExercisePreview, trapModalTabKey } from "../prestart-modal.js";
import { shuffle } from "../utils.js";

interface PlannedWorkout {
  readonly plannedWorkoutId: string;
  readonly name: string;
}

interface WorkoutExercise {
  readonly exerciseId: string;
  readonly name: string;
}

interface WorkoutDetail {
  readonly plannedWorkoutId: string;
  readonly name: string;
  readonly exercises: readonly WorkoutExercise[];
}

interface LastWorkoutDto {
  readonly hasSession: boolean;
  readonly workoutName?: string;
  readonly completedAt?: string;
}

let loadedWorkoutIds: Set<string> = new Set();

// Pre-start modal state
let prestartWorkout: WorkoutDetail | null = null;
let prestartCurrentOrder: WorkoutExercise[] = [];
let prestartIsShuffled = false;

export function render(container: HTMLElement): void {
  container.innerHTML = `
    <div class="app">
      <h1 class="app__title">Home</h1>
      <form class="workout-form" id="workout-form" novalidate>
        <div class="workout-form__group">
          <label class="workout-form__label" for="workout-select">
            Select your workout
          </label>
          <select
            class="workout-form__select"
            id="workout-select"
            name="workout"
            aria-describedby="workout-error"
            required
          >
            <option value="" disabled selected>Select a workout</option>
          </select>
        </div>
        <div
          class="workout-form__error"
          id="workout-error"
          role="alert"
          aria-live="polite"
          hidden
        ></div>
        <button class="workout-form__button" type="submit">
          Start Workout
        </button>
      </form>
      <div class="prestart-modal-backdrop" id="workout-prestart-backdrop" style="display:none;">
        <div class="prestart-modal" role="dialog" aria-modal="true" aria-labelledby="prestart-modal-title">
          <h2 class="prestart-modal__title" id="prestart-modal-title">Start Workout</h2>
          <div class="prestart-modal__shuffle" id="prestart-shuffle-row">
            <label class="prestart-modal__shuffle-label" for="prestart-shuffle-toggle">Randomise order</label>
            <button
              class="prestart-modal__shuffle-btn"
              type="button"
              id="prestart-shuffle-toggle"
              role="switch"
              aria-checked="false"
            ><span class="sr-only">Randomise order</span></button>
          </div>
          <ol class="prestart-modal__exercise-list" id="prestart-exercise-list" aria-label="Exercise order preview"></ol>
          <button class="prestart-modal__reshuffle-btn" type="button" id="prestart-reshuffle" style="display:none;">Re-shuffle</button>
          <div class="prestart-modal__actions">
            <button class="prestart-modal__start-btn" type="button" id="prestart-start">Start Workout</button>
            <button class="prestart-modal__cancel-btn" type="button" id="prestart-cancel">Cancel</button>
          </div>
        </div>
      </div>
    </div>
  `;

  loadedWorkoutIds = new Set();
  prestartWorkout = null;
  prestartCurrentOrder = [];
  prestartIsShuffled = false;

  initForm();
  initPreStartModal();
  const formEl = document.getElementById("workout-form");
  if (formEl) {
    void loadLastWorkoutHint(formEl);
  }
}

async function loadLastWorkoutHint(formEl: HTMLElement): Promise<void> {
  try {
    const response = await fetch("/api/sessions/latest");
    if (!response.ok) {
      return;
    }
    const dto: LastWorkoutDto = await response.json();
    if (!dto.hasSession || !dto.workoutName || !dto.completedAt) {
      return;
    }
    if (!document.contains(formEl)) {
      return;
    }
    const date = new Date(dto.completedAt).toLocaleDateString("en-GB", {
      day: "numeric",
      month: "long",
      year: "numeric",
    });
    const hint = document.createElement("p");
    hint.className = "workout-form__last-workout";
    hint.textContent = `Last workout: ${dto.workoutName} \u2014 ${date}`;
    formEl.appendChild(hint);
  } catch {
    // API unavailable — hint remains absent
  }
}

function initForm(): void {
  const form = document.getElementById("workout-form") as HTMLFormElement | null;
  const select = document.getElementById("workout-select") as HTMLSelectElement | null;
  const errorEl = document.getElementById("workout-error") as HTMLElement | null;

  if (!form || !select || !errorEl) {
    return;
  }

  populateWorkoutOptions(select);

  form.addEventListener("submit", (event: Event) => {
    event.preventDefault();
    void handleStartWorkout(select, errorEl);
  });

  select.addEventListener("change", () => {
    if (select.value && isValidWorkoutValue(select.value)) {
      clearError(select, errorEl);
    }
  });
}

async function populateWorkoutOptions(select: HTMLSelectElement): Promise<void> {
  try {
    const response = await fetch("/api/workouts");
    if (!response.ok) {
      return;
    }

    const workouts: PlannedWorkout[] = await response.json();
    loadedWorkoutIds = new Set(workouts.map((w) => w.plannedWorkoutId));

    for (const w of workouts) {
      const optionEl = document.createElement("option");
      optionEl.value = w.plannedWorkoutId;
      optionEl.textContent = w.name;
      select.appendChild(optionEl);
    }
  } catch {
    // API unavailable — dropdown remains empty
  }
}

async function handleStartWorkout(select: HTMLSelectElement, errorEl: HTMLElement): Promise<void> {
  const selectedValue = select.value;

  if (!selectedValue || !isValidWorkoutValue(selectedValue)) {
    showError(select, errorEl, "Please select a workout");
    return;
  }

  clearError(select, errorEl);

  try {
    const response = await fetch(`/api/workouts/${selectedValue}`);
    if (!response.ok) {
      navigate(`/active-session?id=${selectedValue}`);
      return;
    }
    const workout: WorkoutDetail = await response.json();
    openPreStartModal(workout);
  } catch {
    navigate(`/active-session?id=${selectedValue}`);
  }
}

// === Pre-start Modal =========================================================

function initPreStartModal(): void {
  const backdrop = document.getElementById("workout-prestart-backdrop") as HTMLElement | null;
  if (!backdrop) return;

  const cancelBtn = document.getElementById("prestart-cancel") as HTMLButtonElement | null;
  const startBtn = document.getElementById("prestart-start") as HTMLButtonElement | null;
  const shuffleToggle = document.getElementById("prestart-shuffle-toggle") as HTMLButtonElement | null;
  const reshuffleBtn = document.getElementById("prestart-reshuffle") as HTMLButtonElement | null;

  cancelBtn?.addEventListener("click", () => { closePreStartModal(); });
  startBtn?.addEventListener("click", () => { handleConfirmStart(); });
  shuffleToggle?.addEventListener("click", () => { handleShuffleToggle(); });
  reshuffleBtn?.addEventListener("click", () => { handleReshuffle(); });

  backdrop.addEventListener("click", (event: Event) => {
    if (event.target === backdrop) closePreStartModal();
  });

  backdrop.addEventListener("keydown", (event: KeyboardEvent) => {
    if (event.key === "Escape") {
      closePreStartModal();
      return;
    }

    const modal = backdrop.querySelector(".prestart-modal") as HTMLElement | null;
    if (!modal) return;

    trapModalTabKey(event, modal);
  });
}

function openPreStartModal(workout: WorkoutDetail): void {
  const backdrop = document.getElementById("workout-prestart-backdrop") as HTMLElement | null;
  const shuffleRow = document.getElementById("prestart-shuffle-row") as HTMLElement | null;
  const startBtn = document.getElementById("prestart-start") as HTMLButtonElement | null;
  const cancelBtn = document.getElementById("prestart-cancel") as HTMLButtonElement | null;
  const shuffleToggle = document.getElementById("prestart-shuffle-toggle") as HTMLButtonElement | null;
  const reshuffleBtn = document.getElementById("prestart-reshuffle") as HTMLButtonElement | null;

  if (!backdrop) return;

  prestartWorkout = workout;
  prestartCurrentOrder = [...workout.exercises];
  prestartIsShuffled = false;

  if (shuffleToggle) shuffleToggle.setAttribute("aria-checked", "false");
  if (reshuffleBtn) reshuffleBtn.style.display = "none";

  if (shuffleRow) {
    shuffleRow.style.display = workout.exercises.length >= 2 ? "" : "none";
  }

  if (startBtn) {
    startBtn.disabled = workout.exercises.length === 0;
  }

  renderExercisePreview(prestartCurrentOrder);

  backdrop.style.display = "";

  if (workout.exercises.length > 0) {
    startBtn?.focus();
  } else {
    cancelBtn?.focus();
  }
}

function closePreStartModal(): void {
  const backdrop = document.getElementById("workout-prestart-backdrop") as HTMLElement | null;
  if (backdrop) backdrop.style.display = "none";

  prestartWorkout = null;
  prestartCurrentOrder = [];
  prestartIsShuffled = false;

  const submitBtn = document.querySelector<HTMLButtonElement>(".workout-form__button");
  submitBtn?.focus();
}

function renderExercisePreview(exercises: readonly WorkoutExercise[]): void {
  const list = document.getElementById("prestart-exercise-list") as HTMLOListElement | null;
  if (!list) return;
  renderPrestartExercisePreview(list, exercises);
}

function handleShuffleToggle(): void {
  const toggleBtn = document.getElementById("prestart-shuffle-toggle") as HTMLButtonElement | null;
  const reshuffleBtn = document.getElementById("prestart-reshuffle") as HTMLButtonElement | null;
  if (!toggleBtn || !prestartWorkout) return;

  const newChecked = toggleBtn.getAttribute("aria-checked") !== "true";
  toggleBtn.setAttribute("aria-checked", String(newChecked));

  if (newChecked) {
    prestartCurrentOrder = shuffle(prestartWorkout.exercises);
    if (reshuffleBtn) reshuffleBtn.style.display = "";
  } else {
    prestartCurrentOrder = [...prestartWorkout.exercises];
    if (reshuffleBtn) reshuffleBtn.style.display = "none";
  }

  prestartIsShuffled = newChecked;
  renderExercisePreview(prestartCurrentOrder);
}

function handleReshuffle(): void {
  if (!prestartWorkout) return;
  prestartCurrentOrder = shuffle(prestartWorkout.exercises);
  renderExercisePreview(prestartCurrentOrder);
}

function handleConfirmStart(): void {
  if (!prestartWorkout) return;
  const workoutId = prestartWorkout.plannedWorkoutId;

  if (prestartIsShuffled) {
    const order = prestartCurrentOrder.map(ex => ex.exerciseId).join(",");
    navigate(`/active-session?id=${workoutId}&order=${encodeURIComponent(order)}`);
  } else {
    navigate(`/active-session?id=${workoutId}`);
  }
}

// =============================================================================

function isValidWorkoutValue(value: string): boolean {
  return loadedWorkoutIds.has(value);
}

function showError(select: HTMLSelectElement, errorEl: HTMLElement, message: string): void {
  errorEl.textContent = message;
  errorEl.hidden = false;
  select.classList.add("workout-form__select--error");
  select.setAttribute("aria-invalid", "true");
}

function clearError(select: HTMLSelectElement, errorEl: HTMLElement): void {
  errorEl.textContent = "";
  errorEl.hidden = true;
  select.classList.remove("workout-form__select--error");
  select.removeAttribute("aria-invalid");
}

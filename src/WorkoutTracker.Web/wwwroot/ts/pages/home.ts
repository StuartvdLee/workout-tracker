import { navigate } from "../router.js";
import { shuffle } from "../utils.js";

interface PlannedWorkout {
  readonly plannedWorkoutId: string;
  readonly name: string;
  readonly exerciseCount: number;
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

let loadedWorkouts: Map<string, PlannedWorkout> = new Map();

export function render(container: HTMLElement): void {
  container.innerHTML = `
    <div class="home-page">
      <h1 class="home-page__title">Let's go!</h1>
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
        <div class="workout-form__group">
          <label class="workout-form__label" for="sets-select">Sets</label>
          <select
            class="workout-form__select"
            id="sets-select"
            name="sets"
            aria-describedby="sets-error"
            required
          >
            <option value="3" selected>3</option>
            <option value="5">5</option>
          </select>
        </div>
        <div class="workout-form__randomise" id="home-randomise-row" style="display:none;">
          <label class="workout-form__randomise-label" for="home-randomise-toggle">Randomise exercise order</label>
          <button
            class="workout-form__randomise-btn"
            type="button"
            id="home-randomise-toggle"
            role="switch"
            aria-checked="false"
          ><span class="sr-only">Randomise exercise order</span></button>
        </div>
        <div
          class="workout-form__error"
          id="workout-error"
          role="alert"
          aria-live="polite"
          hidden
        ></div>
        <div
          class="workout-form__error"
          id="sets-error"
          role="alert"
          aria-live="polite"
          hidden
        ></div>
        <button class="workout-form__button" type="submit">
          Start Workout
        </button>
      </form>
    </div>
  `;

  loadedWorkouts = new Map();

  initForm();
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
  const setsSelect = document.getElementById("sets-select") as HTMLSelectElement | null;
  const setsErrorEl = document.getElementById("sets-error") as HTMLElement | null;
  const toggleBtn = document.getElementById("home-randomise-toggle") as HTMLButtonElement | null;

  if (!form || !select || !errorEl || !setsSelect || !setsErrorEl) {
    return;
  }

  select.disabled = true;
  setsSelect.disabled = true;
  void populateWorkoutOptions(select, setsSelect);

  form.addEventListener("submit", (event: Event) => {
    event.preventDefault();
    void handleStartWorkout(select, errorEl, setsSelect, setsErrorEl);
  });

  select.addEventListener("change", () => {
    if (select.value && isValidWorkoutValue(select.value)) {
      clearError(select, errorEl);
    }
    updateRandomiseRowVisibility(select.value);
  });

  setsSelect.addEventListener("change", () => {
    if (isValidSetsValue(setsSelect.value)) {
      clearError(setsSelect, setsErrorEl);
    }
  });

  toggleBtn?.addEventListener("click", () => {
    const current = toggleBtn.getAttribute("aria-checked") === "true";
    toggleBtn.setAttribute("aria-checked", String(!current));
  });
}

async function populateWorkoutOptions(
  select: HTMLSelectElement,
  setsSelect: HTMLSelectElement,
): Promise<void> {
  try {
    const response = await fetch("/api/workouts");
    if (!response.ok) {
      return;
    }

    const workouts: PlannedWorkout[] = await response.json();
    loadedWorkouts = new Map(workouts.map((w) => [w.plannedWorkoutId, w]));

    for (const w of workouts) {
      const optionEl = document.createElement("option");
      optionEl.value = w.plannedWorkoutId;
      optionEl.textContent = w.name;
      select.appendChild(optionEl);
    }
  } catch {
    // API unavailable — dropdown remains empty
  } finally {
    select.disabled = false;
    setsSelect.disabled = false;
  }
}

function updateRandomiseRowVisibility(selectedValue: string): void {
  const row = document.getElementById("home-randomise-row") as HTMLElement | null;
  const toggleBtn = document.getElementById("home-randomise-toggle") as HTMLButtonElement | null;
  if (!row) return;

  const workout = loadedWorkouts.get(selectedValue);
  const show = workout !== undefined && workout.exerciseCount >= 2;
  row.style.display = show ? "" : "none";
  if (toggleBtn) {
    toggleBtn.setAttribute("aria-checked", "false");
  }
}

async function handleStartWorkout(
  select: HTMLSelectElement,
  errorEl: HTMLElement,
  setsSelect: HTMLSelectElement,
  setsErrorEl: HTMLElement,
): Promise<void> {
  const selectedValue = select.value;

  if (!selectedValue || !isValidWorkoutValue(selectedValue)) {
    showError(select, errorEl, "Please select a workout");
    select.focus();
    return;
  }

  clearError(select, errorEl);
  const sets = setsSelect.value;
  if (!isValidSetsValue(sets)) {
    showError(setsSelect, setsErrorEl, "Please select sets");
    setsSelect.focus();
    return;
  }
  clearError(setsSelect, setsErrorEl);

  const toggleBtn = document.getElementById("home-randomise-toggle") as HTMLButtonElement | null;
  const isRandomise = toggleBtn?.getAttribute("aria-checked") === "true";

  if (!isRandomise) {
    navigate(`/active-session?id=${selectedValue}&sets=${sets}`);
    return;
  }

  try {
    const response = await fetch(`/api/workouts/${selectedValue}`);
    if (!response.ok) {
      navigate(`/active-session?id=${selectedValue}&sets=${sets}`);
      return;
    }
    const workout: WorkoutDetail = await response.json();
    const order = shuffle(workout.exercises).map((ex) => ex.exerciseId).join(",");
    navigate(`/active-session?id=${selectedValue}&sets=${sets}&order=${order}`);
  } catch {
    navigate(`/active-session?id=${selectedValue}&sets=${sets}`);
  }
}

// =============================================================================

function isValidWorkoutValue(value: string): boolean {
  return loadedWorkouts.has(value);
}

function isValidSetsValue(value: string): value is "3" | "5" {
  return value === "3" || value === "5";
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

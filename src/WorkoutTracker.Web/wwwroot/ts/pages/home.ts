import { navigate } from "../router.js";

interface PlannedWorkout {
  readonly plannedWorkoutId: string;
  readonly name: string;
}

interface LastWorkoutDto {
  readonly hasSession: boolean;
  readonly workoutName?: string;
  readonly completedAt?: string;
}

let loadedWorkoutIds: Set<string> = new Set();

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
    </div>
  `;

  loadedWorkoutIds = new Set();
  initForm();
  void loadLastWorkoutHint();
}

async function loadLastWorkoutHint(): Promise<void> {
  try {
    const response = await fetch("/api/sessions/latest");
    if (!response.ok) {
      return;
    }
    const dto: LastWorkoutDto = await response.json();
    if (!dto.hasSession || !dto.workoutName || !dto.completedAt) {
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
    const form = document.getElementById("workout-form");
    form?.appendChild(hint);
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
    handleStartWorkout(select, errorEl);
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

function handleStartWorkout(select: HTMLSelectElement, errorEl: HTMLElement): void {
  const selectedValue = select.value;

  if (!selectedValue || !isValidWorkoutValue(selectedValue)) {
    showError(select, errorEl, "Please select a workout");
    return;
  }

  clearError(select, errorEl);
  navigate(`/active-session?id=${selectedValue}`);
}

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

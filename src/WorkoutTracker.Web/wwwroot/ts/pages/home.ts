interface WorkoutType {
  readonly workoutTypeId: string;
  readonly name: string;
}

let loadedWorkoutTypeIds: Set<string> = new Set();

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

  loadedWorkoutTypeIds = new Set();
  initForm();
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
    const response = await fetch("/api/workout-types");
    if (!response.ok) {
      return;
    }

    const workoutTypes: WorkoutType[] = await response.json();
    loadedWorkoutTypeIds = new Set(workoutTypes.map((wt) => wt.workoutTypeId));

    for (const wt of workoutTypes) {
      const optionEl = document.createElement("option");
      optionEl.value = wt.workoutTypeId;
      optionEl.textContent = wt.name;
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
}

function isValidWorkoutValue(value: string): boolean {
  return loadedWorkoutTypeIds.has(value);
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

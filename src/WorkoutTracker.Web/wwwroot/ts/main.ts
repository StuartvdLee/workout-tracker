interface WorkoutType {
  readonly workoutTypeId: string;
  readonly name: string;
}

let loadedWorkoutTypeIds: Set<string> = new Set();

async function initializeApp(): Promise<void> {
  const form = document.getElementById("workout-form") as HTMLFormElement | null;
  const select = document.getElementById(
    "workout-select",
  ) as HTMLSelectElement | null;
  const errorEl = document.getElementById(
    "workout-error",
  ) as HTMLElement | null;

  if (!form || !select || !errorEl) {
    return;
  }

  await populateWorkoutOptions(select);

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

async function populateWorkoutOptions(
  select: HTMLSelectElement,
): Promise<void> {
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

function handleStartWorkout(
  select: HTMLSelectElement,
  errorEl: HTMLElement,
): void {
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

function showError(
  select: HTMLSelectElement,
  errorEl: HTMLElement,
  message: string,
): void {
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

document.addEventListener("DOMContentLoaded", initializeApp);

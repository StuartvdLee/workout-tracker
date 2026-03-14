interface WorkoutOption {
  readonly value: string;
  readonly label: string;
}

const WORKOUT_OPTIONS: ReadonlyArray<WorkoutOption> = [
  { value: "push", label: "Push" },
  { value: "pull", label: "Pull" },
  { value: "legs", label: "Legs" },
] as const;

const VALID_WORKOUT_VALUES = new Set(WORKOUT_OPTIONS.map((o) => o.value));

function initializeApp(): void {
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
}

function populateWorkoutOptions(select: HTMLSelectElement): void {
  for (const option of WORKOUT_OPTIONS) {
    const optionEl = document.createElement("option");
    optionEl.value = option.value;
    optionEl.textContent = option.label;
    select.appendChild(optionEl);
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
  return VALID_WORKOUT_VALUES.has(value);
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

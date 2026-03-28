interface ExerciseMuscle {
  readonly muscleId: string;
  readonly name: string;
}

interface Exercise {
  readonly exerciseId: string;
  readonly name: string;
  readonly muscles: ExerciseMuscle[];
}

interface Muscle {
  readonly muscleId: string;
  readonly name: string;
}

let exercises: Exercise[] = [];
let muscles: Muscle[] = [];
let editingExerciseId: string | null = null;
let selectedMuscleIds: Set<string> = new Set();
let isSubmitting = false;

export async function render(container: HTMLElement): Promise<void> {
  container.innerHTML = `
    <div class="exercises-page">
      <h1 class="exercises-page__title">Exercises</h1>
      <form class="exercise-form" id="exercise-form" novalidate>
        <div class="exercise-form__group">
          <label class="exercise-form__label" for="exercise-name">Exercise name</label>
          <input
            class="exercise-form__input"
            type="text"
            id="exercise-name"
            name="exercise-name"
            maxlength="150"
            aria-describedby="exercise-error"
            autocomplete="off"
          />
        </div>
        <div class="exercise-form__group">
          <label class="exercise-form__label">Targeted muscles (optional)</label>
          <div class="exercise-form__muscles" id="exercise-muscles" role="group" aria-label="Targeted muscles">
          </div>
        </div>
        <div class="exercise-form__error" id="exercise-error" role="alert" aria-live="polite"></div>
        <div class="exercise-form__actions">
          <button class="exercise-form__submit" type="submit">Add Exercise</button>
          <button class="exercise-form__cancel" type="button" id="exercise-cancel" style="display:none;">Cancel</button>
        </div>
        <div class="exercise-form__api-error" id="exercise-api-error" role="alert" aria-live="polite"></div>
      </form>
      <section class="exercise-list">
        <h2 class="exercise-list__heading">Your Exercises</h2>
        <ul class="exercise-list__items" id="exercise-list"></ul>
      </section>
      <div class="exercise-list__empty" id="exercise-empty" style="display:none;">
        No exercises yet. Add your first exercise above!
      </div>
    </div>
  `;

  editingExerciseId = null;
  selectedMuscleIds = new Set();
  isSubmitting = false;

  initForm();
  await loadData();
}

function initForm(): void {
  const form = document.getElementById("exercise-form") as HTMLFormElement | null;
  const cancelBtn = document.getElementById("exercise-cancel") as HTMLButtonElement | null;

  if (!form) return;

  form.addEventListener("submit", (event: Event) => {
    event.preventDefault();
    void handleSubmit();
  });

  cancelBtn?.addEventListener("click", () => {
    resetToCreateMode();
  });
}

async function loadData(): Promise<void> {
  try {
    const [exercisesRes, musclesRes] = await Promise.all([
      fetch("/api/exercises"),
      fetch("/api/muscles"),
    ]);

    if (exercisesRes.ok) {
      exercises = await exercisesRes.json();
    }

    if (musclesRes.ok) {
      muscles = await musclesRes.json();
    }
  } catch {
    // API unavailable
  }

  renderMuscleToggles();
  renderExerciseList();
}

function renderMuscleToggles(): void {
  const musclesContainer = document.getElementById("exercise-muscles");
  if (!musclesContainer) return;

  musclesContainer.innerHTML = "";

  for (const muscle of muscles) {
    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "muscle-toggle";
    btn.textContent = muscle.name;
    btn.setAttribute("role", "checkbox");
    btn.setAttribute("aria-checked", "false");
    btn.setAttribute("data-muscle-id", muscle.muscleId);

    btn.addEventListener("click", () => {
      toggleMuscle(muscle.muscleId, btn);
    });

    musclesContainer.appendChild(btn);
  }
}

function toggleMuscle(muscleId: string, btn: HTMLButtonElement): void {
  if (selectedMuscleIds.has(muscleId)) {
    selectedMuscleIds.delete(muscleId);
    btn.classList.remove("muscle-toggle--active");
    btn.setAttribute("aria-checked", "false");
  } else {
    selectedMuscleIds.add(muscleId);
    btn.classList.add("muscle-toggle--active");
    btn.setAttribute("aria-checked", "true");
  }
}

function renderExerciseList(): void {
  const listEl = document.getElementById("exercise-list");
  const emptyEl = document.getElementById("exercise-empty");

  if (!listEl || !emptyEl) return;

  listEl.innerHTML = "";

  if (exercises.length === 0) {
    emptyEl.style.display = "";
    return;
  }

  emptyEl.style.display = "none";

  for (const exercise of exercises) {
    const li = document.createElement("li");
    li.className = "exercise-list__item";
    li.setAttribute("data-exercise-id", exercise.exerciseId);

    const details = document.createElement("div");
    details.className = "exercise-list__details";

    const nameSpan = document.createElement("span");
    nameSpan.className = "exercise-list__name";
    nameSpan.textContent = exercise.name;
    details.appendChild(nameSpan);

    if (exercise.muscles.length > 0) {
      const musclesDiv = document.createElement("div");
      musclesDiv.className = "exercise-list__muscles";

      for (const muscle of exercise.muscles) {
        const chip = document.createElement("span");
        chip.className = "exercise-list__muscle-chip";
        chip.textContent = muscle.name;
        musclesDiv.appendChild(chip);
      }

      details.appendChild(musclesDiv);
    }

    li.appendChild(details);

    const editBtn = document.createElement("button");
    editBtn.className = "exercise-list__edit-btn";
    editBtn.textContent = "Edit";
    editBtn.setAttribute("aria-label", `Edit ${exercise.name}`);
    editBtn.setAttribute("data-exercise-id", exercise.exerciseId);
    editBtn.addEventListener("click", () => {
      enterEditMode(exercise);
    });

    li.appendChild(editBtn);
    listEl.appendChild(li);
  }
}

async function handleSubmit(): Promise<void> {
  if (isSubmitting) return;

  const nameInput = document.getElementById("exercise-name") as HTMLInputElement | null;
  const errorEl = document.getElementById("exercise-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("exercise-api-error") as HTMLElement | null;
  const submitBtn = document.querySelector(".exercise-form__submit") as HTMLButtonElement | null;

  if (!nameInput || !errorEl || !apiErrorEl || !submitBtn) return;

  // Clear previous errors
  clearValidationError(nameInput, errorEl);
  apiErrorEl.textContent = "";

  const name = nameInput.value.trim();

  // Client-side validation
  if (!name) {
    showValidationError(nameInput, errorEl, "Exercise name is required.");
    return;
  }

  if (name.length > 150) {
    showValidationError(nameInput, errorEl, "Exercise name must be 150 characters or fewer.");
    return;
  }

  // Set loading state
  isSubmitting = true;
  submitBtn.setAttribute("aria-disabled", "true");
  const originalText = submitBtn.textContent;
  submitBtn.textContent = "Saving...";
  submitBtn.classList.add("exercise-form__submit--loading");

  try {
    const muscleIds = Array.from(selectedMuscleIds);
    const isEdit = editingExerciseId !== null;
    const url = isEdit ? `/api/exercises/${editingExerciseId}` : "/api/exercises";
    const method = isEdit ? "PUT" : "POST";

    const response = await fetch(url, {
      method,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name, muscleIds }),
    });

    if (response.ok) {
      // Reload list
      const exercisesRes = await fetch("/api/exercises");
      if (exercisesRes.ok) {
        exercises = await exercisesRes.json();
      }

      resetToCreateMode();
      renderExerciseList();
    } else {
      const data = await response.json();
      apiErrorEl.textContent = data.error || "An unexpected error occurred. Please try again.";
    }
  } catch {
    apiErrorEl.textContent = "An unexpected error occurred. Please try again.";
  } finally {
    isSubmitting = false;
    submitBtn.removeAttribute("aria-disabled");
    submitBtn.textContent = originalText;
    submitBtn.classList.remove("exercise-form__submit--loading");

    // In edit mode, restore proper button text
    if (editingExerciseId !== null) {
      submitBtn.textContent = "Update Exercise";
    } else {
      submitBtn.textContent = "Add Exercise";
    }
  }
}

function enterEditMode(exercise: Exercise): void {
  const nameInput = document.getElementById("exercise-name") as HTMLInputElement | null;
  const submitBtn = document.querySelector(".exercise-form__submit") as HTMLButtonElement | null;
  const cancelBtn = document.getElementById("exercise-cancel") as HTMLButtonElement | null;
  const errorEl = document.getElementById("exercise-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("exercise-api-error") as HTMLElement | null;

  if (!nameInput || !submitBtn || !cancelBtn) return;

  // Clear errors
  if (errorEl) {
    errorEl.textContent = "";
    nameInput.classList.remove("exercise-form__input--error");
    nameInput.removeAttribute("aria-invalid");
  }
  if (apiErrorEl) apiErrorEl.textContent = "";

  editingExerciseId = exercise.exerciseId;
  nameInput.value = exercise.name;
  submitBtn.textContent = "Update Exercise";
  cancelBtn.style.display = "block";

  // Set muscle toggle states
  selectedMuscleIds = new Set(exercise.muscles.map(m => m.muscleId));
  const toggleBtns = document.querySelectorAll<HTMLButtonElement>(".muscle-toggle");
  for (const btn of toggleBtns) {
    const muscleId = btn.getAttribute("data-muscle-id") ?? "";
    if (selectedMuscleIds.has(muscleId)) {
      btn.classList.add("muscle-toggle--active");
      btn.setAttribute("aria-checked", "true");
    } else {
      btn.classList.remove("muscle-toggle--active");
      btn.setAttribute("aria-checked", "false");
    }
  }

  nameInput.focus();
}

function resetToCreateMode(): void {
  const nameInput = document.getElementById("exercise-name") as HTMLInputElement | null;
  const submitBtn = document.querySelector(".exercise-form__submit") as HTMLButtonElement | null;
  const cancelBtn = document.getElementById("exercise-cancel") as HTMLButtonElement | null;
  const errorEl = document.getElementById("exercise-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("exercise-api-error") as HTMLElement | null;

  editingExerciseId = null;
  selectedMuscleIds = new Set();

  if (nameInput) {
    nameInput.value = "";
    nameInput.classList.remove("exercise-form__input--error");
    nameInput.removeAttribute("aria-invalid");
  }
  if (submitBtn) submitBtn.textContent = "Add Exercise";
  if (cancelBtn) cancelBtn.style.display = "none";
  if (errorEl) errorEl.textContent = "";
  if (apiErrorEl) apiErrorEl.textContent = "";

  // Reset all muscle toggles
  const toggleBtns = document.querySelectorAll<HTMLButtonElement>(".muscle-toggle");
  for (const btn of toggleBtns) {
    btn.classList.remove("muscle-toggle--active");
    btn.setAttribute("aria-checked", "false");
  }
}

function showValidationError(input: HTMLInputElement, errorEl: HTMLElement, message: string): void {
  errorEl.textContent = message;
  input.classList.add("exercise-form__input--error");
  input.setAttribute("aria-invalid", "true");
}

function clearValidationError(input: HTMLInputElement, errorEl: HTMLElement): void {
  errorEl.textContent = "";
  input.classList.remove("exercise-form__input--error");
  input.removeAttribute("aria-invalid");
}

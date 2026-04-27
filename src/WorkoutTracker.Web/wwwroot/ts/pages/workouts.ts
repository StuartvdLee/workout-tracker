import { navigate } from "../router.js";

interface WorkoutExercise {
  readonly exerciseId: string;
  readonly name: string;
  readonly targetReps: string | null;
  readonly targetWeight: string | null;
}

interface Workout {
  readonly plannedWorkoutId: string;
  readonly name: string;
  readonly exerciseCount: number;
  readonly exercises: WorkoutExercise[];
}

interface Exercise {
  readonly exerciseId: string;
  readonly name: string;
}

let workouts: Workout[] = [];
let availableExercises: Exercise[] = [];
let selectedExercises: Set<string> = new Set();
let isSubmitting = false;

// Edit modal state
let editingWorkoutId: string | null = null;
let editSelectedExercises: Set<string> = new Set();
let isEditSubmitting = false;

// Delete confirmation state
let deletingWorkoutId: string | null = null;
let isDeleting = false;

export async function render(container: HTMLElement): Promise<void> {
  container.innerHTML = `
    <div class="workouts-page">
      <h1 class="workouts-page__title">Workouts</h1>
      <form class="workout-form" id="workout-form" novalidate>
        <div class="workout-form__group">
          <label class="workout-form__label" for="workout-name">Workout name</label>
          <input
            class="workout-form__input"
            type="text"
            id="workout-name"
            maxlength="150"
            aria-describedby="workout-error"
            autocomplete="off"
          />
        </div>
        <div class="workout-form__group">
          <label class="workout-form__label" for="workout-exercise-select">Add exercise</label>
          <select class="workout-form__select" id="workout-exercise-select">
            <option value="" disabled selected>Select an exercise</option>
          </select>
        </div>
        <div class="workout-form__exercises" id="workout-exercises" aria-label="Available exercises"></div>
        <div id="workout-selected-section" style="display:none;">
          <h3 class="workout-selected__heading">Selected exercises</h3>
          <ul class="workout-selected__list" id="workout-selected-list"></ul>
        </div>
        <div class="workout-form__error" id="workout-error" role="alert" aria-live="polite"></div>
        <div class="workout-form__actions">
          <button class="workout-form__submit" type="submit">Create Workout</button>
        </div>
        <div class="workout-form__api-error" id="workout-api-error" role="alert" aria-live="polite"></div>
      </form>
      <section class="workout-list">
        <h2 class="workout-list__heading">Your Workouts</h2>
        <ul class="workout-list__items" id="workout-list"></ul>
      </section>
      <div class="workout-list__empty" id="workout-empty" style="display:none;">
        No workouts yet. Create your first workout above!
      </div>
      <div class="edit-modal-backdrop" id="workout-edit-backdrop" style="display:none;">
        <div class="edit-modal" role="dialog" aria-modal="true" aria-labelledby="workout-edit-title">
          <h2 class="edit-modal__title" id="workout-edit-title">Edit Workout</h2>
          <form class="edit-modal__form" id="workout-edit-form" novalidate>
            <div class="workout-form__group">
              <label class="workout-form__label" for="edit-workout-name">Workout name</label>
              <input class="workout-form__input" type="text" id="edit-workout-name" maxlength="150" autocomplete="off" aria-describedby="edit-workout-error" />
            </div>
            <div class="workout-form__group">
              <label class="workout-form__label" for="edit-exercise-select">Add exercise</label>
              <select class="workout-form__select" id="edit-exercise-select">
                <option value="" disabled selected>Select an exercise</option>
              </select>
            </div>
            <div class="workout-form__exercises" id="edit-workout-exercises" aria-label="Available exercises"></div>
            <div id="edit-selected-section" style="display:none;">
              <h3 class="workout-selected__heading">Selected exercises</h3>
              <ul class="workout-selected__list" id="edit-selected-list"></ul>
            </div>
            <div class="workout-form__error" id="edit-workout-error" role="alert" aria-live="polite"></div>
            <div class="edit-modal__actions">
              <button class="workout-form__submit" type="submit">Save Changes</button>
              <button class="workout-form__cancel edit-modal__cancel" type="button" id="workout-edit-cancel">Cancel</button>
            </div>
            <div class="workout-form__api-error" id="edit-workout-api-error" role="alert" aria-live="polite"></div>
          </form>
        </div>
      </div>
      <div class="delete-modal-backdrop" id="workout-delete-backdrop" style="display:none;">
        <div class="delete-modal" role="alertdialog" aria-modal="true" aria-labelledby="workout-delete-title" aria-describedby="workout-delete-desc">
          <h2 class="delete-modal__title" id="workout-delete-title">Delete Workout</h2>
          <p class="delete-modal__desc" id="workout-delete-desc"></p>
          <div class="delete-modal__actions">
            <button class="delete-modal__delete" type="button" id="workout-delete-confirm">Delete</button>
            <button class="delete-modal__cancel" type="button" id="workout-delete-cancel">Cancel</button>
          </div>
          <div class="delete-modal__error" id="workout-delete-error" role="alert" aria-live="polite"></div>
        </div>
      </div>
    </div>
  `;

  editingWorkoutId = null;
  selectedExercises = new Set();
  editSelectedExercises = new Set();
  isSubmitting = false;
  isEditSubmitting = false;
  deletingWorkoutId = null;
  isDeleting = false;

  initForm();
  initEditModal();
  initDeleteModal();
  await loadData();
}

function initForm(): void {
  const form = document.getElementById("workout-form") as HTMLFormElement | null;
  const exerciseSelect = document.getElementById("workout-exercise-select") as HTMLSelectElement | null;
  if (!form) return;

  form.addEventListener("submit", (event: Event) => {
    event.preventDefault();
    void handleSubmit();
  });

  exerciseSelect?.addEventListener("change", () => {
    const exerciseId = exerciseSelect.value;
    if (exerciseId) {
      selectedExercises.add(exerciseId);
      renderExerciseDropdown();
    }
  });
}

function initEditModal(): void {
  const form = document.getElementById("workout-edit-form") as HTMLFormElement | null;
  const cancelBtn = document.getElementById("workout-edit-cancel") as HTMLButtonElement | null;
  const backdrop = document.getElementById("workout-edit-backdrop") as HTMLElement | null;

  if (!form || !backdrop) return;

  form.addEventListener("submit", (event: Event) => {
    event.preventDefault();
    void handleEditSubmit();
  });

  cancelBtn?.addEventListener("click", () => {
    closeEditModal();
  });

  backdrop.addEventListener("click", (event: Event) => {
    if (event.target === backdrop) {
      closeEditModal();
    }
  });

  backdrop.addEventListener("keydown", (event: KeyboardEvent) => {
    if (event.key === "Escape") {
      closeEditModal();
      return;
    }

    // Focus trap
    if (event.key === "Tab") {
      const modal = backdrop.querySelector(".edit-modal") as HTMLElement | null;
      if (!modal) return;

      const focusable = modal.querySelectorAll<HTMLElement>(
        'input, button, [tabindex]:not([tabindex="-1"])'
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

function initDeleteModal(): void {
  const backdrop = document.getElementById("workout-delete-backdrop");

  if (!backdrop) {
    return;
  }

  const backdropEl = backdrop;
  const cancelBtn = document.getElementById("workout-delete-cancel");
  const confirmBtn = document.getElementById("workout-delete-confirm");

  backdropEl.addEventListener("click", (event: Event) => {
    if (event.target === backdropEl) {
      closeDeleteModal();
    }
  });

  backdropEl.addEventListener("keydown", (event: KeyboardEvent) => {
    if (event.key === "Escape") {
      closeDeleteModal();
    }

    // Focus trapping
    if (event.key === "Tab") {
      const focusable = backdropEl.querySelectorAll<HTMLElement>(
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

  cancelBtn?.addEventListener("click", () => {
    closeDeleteModal();
  });

  confirmBtn?.addEventListener("click", () => {
    void handleDelete();
  });
}

async function loadData(): Promise<void> {
  try {
    const [exercisesRes, workoutsRes] = await Promise.all([
      fetch("/api/exercises"),
      fetch("/api/workouts"),
    ]);

    availableExercises = exercisesRes.ok ? await exercisesRes.json() : [];
    workouts = workoutsRes.ok ? await workoutsRes.json() : [];
  } catch {
    availableExercises = [];
    workouts = [];
  }

  renderExerciseDropdown();
  renderWorkoutList();
}

function renderExerciseDropdown(): void {
  const select = document.getElementById("workout-exercise-select") as HTMLSelectElement | null;
  if (!select) return;

  while (select.options.length > 1) select.remove(1);

  for (const exercise of availableExercises) {
    if (!selectedExercises.has(exercise.exerciseId)) {
      const option = document.createElement("option");
      option.value = exercise.exerciseId;
      option.textContent = exercise.name;
      select.appendChild(option);
    }
  }

  select.value = "";
  renderSelectedExercisesList();
  renderExerciseToggles();
}

function renderSelectedExercisesList(): void {
  const list = document.getElementById("workout-selected-list");
  const section = document.getElementById("workout-selected-section");
  if (!list || !section) return;

  list.innerHTML = "";
  section.style.display = selectedExercises.size > 0 ? "" : "none";

  for (const exerciseId of selectedExercises) {
    const exercise = availableExercises.find(e => e.exerciseId === exerciseId);
    if (!exercise) continue;
    list.appendChild(buildSelectedExerciseItem(exercise.name, () => {
      selectedExercises.delete(exerciseId);
      renderExerciseDropdown();
    }));
  }
}

function renderExerciseToggles(): void {
  const container = document.getElementById("workout-exercises") as HTMLElement | null;
  if (!container) return;

  container.innerHTML = "";

  for (const exercise of availableExercises) {
    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "workout-form__exercise-item";
    btn.setAttribute("role", "checkbox");
    btn.setAttribute("data-exercise-id", exercise.exerciseId);
    btn.setAttribute("aria-checked", "false");
    btn.textContent = exercise.name;

    btn.addEventListener("click", () => {
      if (selectedExercises.has(exercise.exerciseId)) {
        selectedExercises.delete(exercise.exerciseId);
        btn.setAttribute("aria-checked", "false");
      } else {
        selectedExercises.add(exercise.exerciseId);
        btn.setAttribute("aria-checked", "true");
      }

      // Keep other UI in sync
      renderExerciseDropdown();
      renderSelectedExercisesList();
    });

    container.appendChild(btn);
  }
}

function renderEditExerciseToggles(): void {
  const container = document.getElementById("edit-workout-exercises") as HTMLElement | null;
  if (!container) return;

  container.innerHTML = "";

  for (const exercise of availableExercises) {
    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "workout-form__exercise-item";
    btn.setAttribute("role", "checkbox");
    btn.setAttribute("data-exercise-id", exercise.exerciseId);
    btn.setAttribute("aria-checked", editSelectedExercises.has(exercise.exerciseId) ? "true" : "false");
    btn.textContent = exercise.name;

    btn.addEventListener("click", () => {
      if (editSelectedExercises.has(exercise.exerciseId)) {
        editSelectedExercises.delete(exercise.exerciseId);
        btn.setAttribute("aria-checked", "false");
      } else {
        editSelectedExercises.add(exercise.exerciseId);
        btn.setAttribute("aria-checked", "true");
      }

      renderEditExerciseDropdown();
      renderEditSelectedExercisesList();
    });

    container.appendChild(btn);
  }
}

function renderWorkoutList(): void {
  const listEl = document.getElementById("workout-list");
  const emptyEl = document.getElementById("workout-empty");

  if (!listEl || !emptyEl) return;

  listEl.innerHTML = "";

  if (workouts.length === 0) {
    emptyEl.style.display = "";
    return;
  }

  emptyEl.style.display = "none";

  for (const workout of workouts) {
    const li = document.createElement("li");
    li.className = "workout-list__item";
    li.setAttribute("data-workout-id", workout.plannedWorkoutId);

    const details = document.createElement("div");
    details.className = "workout-list__details";

    const nameSpan = document.createElement("span");
    nameSpan.className = "workout-list__name";
    nameSpan.textContent = workout.name;
    details.appendChild(nameSpan);

    const countSpan = document.createElement("span");
    countSpan.className = "workout-list__exercise-count";
    countSpan.textContent = workout.exerciseCount === 1 ? "1 exercise" : `${workout.exerciseCount} exercises`;
    details.appendChild(countSpan);

    li.appendChild(details);

    const actionsDiv = document.createElement("div");
    actionsDiv.className = "workout-list__actions";

    const editBtn = document.createElement("button");
    editBtn.type = "button";
    editBtn.className = "workout-list__edit-btn";
    editBtn.setAttribute("aria-label", `Edit ${workout.name}`);
    editBtn.setAttribute("data-workout-id", workout.plannedWorkoutId);
    editBtn.innerHTML = `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3.5L17.5 4l3.5 3.5L7.5 21H4z"/><path d="M14.5 5.5l3 3"/></svg>`;
    editBtn.addEventListener("click", () => {
      openEditModal(workout);
    });

    const deleteBtn = document.createElement("button");
    deleteBtn.className = "workout-list__delete-btn";
    deleteBtn.setAttribute("aria-label", `Delete ${workout.name}`);
    deleteBtn.setAttribute("data-workout-id", workout.plannedWorkoutId);
    deleteBtn.innerHTML = `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/><path d="M10 11v6"/><path d="M14 11v6"/><path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/></svg>`;
    deleteBtn.addEventListener("click", () => {
      openDeleteModal(workout);
    });

    const startBtn = document.createElement("button");
    startBtn.className = "workout-list__start-btn";
    startBtn.setAttribute("data-workout-id", workout.plannedWorkoutId);
    startBtn.textContent = "Start";
    startBtn.addEventListener("click", () => {
      navigate(`/active-session?id=${workout.plannedWorkoutId}`);
    });

    actionsDiv.appendChild(editBtn);
    actionsDiv.appendChild(deleteBtn);
    actionsDiv.appendChild(startBtn);
    li.appendChild(actionsDiv);
    listEl.appendChild(li);
  }
}

async function handleSubmit(): Promise<void> {
  if (isSubmitting) return;

  const nameInput = document.getElementById("workout-name") as HTMLInputElement | null;
  const errorEl = document.getElementById("workout-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("workout-api-error") as HTMLElement | null;
  const submitBtn = document.querySelector("#workout-form .workout-form__submit") as HTMLButtonElement | null;

  if (!nameInput || !errorEl || !apiErrorEl || !submitBtn) return;

  // Clear previous errors
  clearValidationError(nameInput, errorEl);
  apiErrorEl.textContent = "";

  const name = nameInput.value.trim();

  // Client-side validation
  if (!name) {
    showValidationError(nameInput, errorEl, "Workout name is required.");
    return;
  }

  if (name.length > 150) {
    showValidationError(nameInput, errorEl, "Workout name must be 150 characters or fewer.");
    return;
  }

  if (selectedExercises.size === 0) {
    errorEl.textContent = "At least one exercise is required.";
    return;
  }

  // Set loading state
  isSubmitting = true;
  submitBtn.setAttribute("aria-disabled", "true");
  const originalText = submitBtn.textContent;
  submitBtn.textContent = "Saving...";
  submitBtn.classList.add("workout-form__submit--loading");

  try {
    const exercises = Array.from(selectedExercises).map(exerciseId => ({
      exerciseId,
      targetReps: null as string | null,
      targetWeight: null as string | null,
    }));

    const response = await fetch("/api/workouts", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name, exercises }),
    });

    if (response.ok) {
      const workoutsRes = await fetch("/api/workouts");
      if (workoutsRes.ok) {
        workouts = await workoutsRes.json();
      }

      resetCreateForm();
      renderWorkoutList();
    } else {
      const data = await response.json();
      apiErrorEl.textContent = data.error || "An unexpected error occurred. Please try again.";
    }
  } catch {
    apiErrorEl.textContent = "An unexpected error occurred. Please try again.";
  } finally {
    isSubmitting = false;
    submitBtn.removeAttribute("aria-disabled");
    submitBtn.textContent = originalText ?? "Create Workout";
    submitBtn.classList.remove("workout-form__submit--loading");
  }
}

function openEditModal(workout: Workout): void {
  const backdrop = document.getElementById("workout-edit-backdrop") as HTMLElement | null;
  const nameInput = document.getElementById("edit-workout-name") as HTMLInputElement | null;
  const errorEl = document.getElementById("edit-workout-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("edit-workout-api-error") as HTMLElement | null;

  if (!backdrop || !nameInput) return;

  editingWorkoutId = workout.plannedWorkoutId;

  // Clear errors
  if (errorEl) {
    errorEl.textContent = "";
    nameInput.classList.remove("workout-form__input--error");
    nameInput.removeAttribute("aria-invalid");
  }
  if (apiErrorEl) apiErrorEl.textContent = "";

  // Fetch the full workout data to get exercises with targets
  void fetchAndPopulateEditModal(workout.plannedWorkoutId, nameInput, backdrop);
}

async function fetchAndPopulateEditModal(workoutId: string, nameInput: HTMLInputElement, backdrop: HTMLElement): Promise<void> {
  try {
    const response = await fetch(`/api/workouts/${workoutId}`);
    if (!response.ok) return;

    const fullWorkout: Workout = await response.json();

    nameInput.value = fullWorkout.name;

    // Populate selected exercises from the workout
    editSelectedExercises = new Set();
    for (const ex of fullWorkout.exercises) {
      editSelectedExercises.add(ex.exerciseId);
    }

    renderEditExerciseDropdown();

    backdrop.style.display = "";
    nameInput.focus();
  } catch {
    // Failed to load workout details
  }
}

function closeEditModal(): void {
  const backdrop = document.getElementById("workout-edit-backdrop") as HTMLElement | null;
  if (backdrop) backdrop.style.display = "none";
  editingWorkoutId = null;
  editSelectedExercises = new Set();
}

function renderEditExerciseDropdown(): void {
  const select = document.getElementById("edit-exercise-select") as HTMLSelectElement | null;
  if (!select) return;

  // Attach change listener once (remove old one by replacing the element's listener via a flag)
  if (!select.dataset["listenerAttached"]) {
    select.dataset["listenerAttached"] = "1";
    select.addEventListener("change", () => {
      const exerciseId = select.value;
      if (exerciseId) {
        editSelectedExercises.add(exerciseId);
        renderEditExerciseDropdown();
      }
    });
  }

  while (select.options.length > 1) select.remove(1);

  for (const exercise of availableExercises) {
    if (!editSelectedExercises.has(exercise.exerciseId)) {
      const option = document.createElement("option");
      option.value = exercise.exerciseId;
      option.textContent = exercise.name;
      select.appendChild(option);
    }
  }

  select.value = "";
  renderEditSelectedExercisesList();
  renderEditExerciseToggles();
}

function renderEditSelectedExercisesList(): void {
  const list = document.getElementById("edit-selected-list");
  const section = document.getElementById("edit-selected-section");
  if (!list || !section) return;

  list.innerHTML = "";
  section.style.display = editSelectedExercises.size > 0 ? "" : "none";

  for (const exerciseId of editSelectedExercises) {
    const exercise = availableExercises.find(e => e.exerciseId === exerciseId);
    if (!exercise) continue;
    list.appendChild(buildSelectedExerciseItem(exercise.name, () => {
      editSelectedExercises.delete(exerciseId);
      renderEditExerciseDropdown();
    }));
  }
}

function buildSelectedExerciseItem(name: string, onRemove: () => void): HTMLLIElement {
  const li = document.createElement("li");
  li.className = "workout-selected__item";

  const nameSpan = document.createElement("span");
  nameSpan.className = "workout-selected__name";
  nameSpan.textContent = name;
  li.appendChild(nameSpan);

  const removeBtn = document.createElement("button");
  removeBtn.type = "button";
  removeBtn.className = "workout-form__remove-btn";
  removeBtn.setAttribute("aria-label", `Remove ${name}`);
  removeBtn.innerHTML = `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>`;
  removeBtn.addEventListener("click", onRemove);
  li.appendChild(removeBtn);

  return li;
}

async function handleEditSubmit(): Promise<void> {
  if (isEditSubmitting || editingWorkoutId === null) return;

  const nameInput = document.getElementById("edit-workout-name") as HTMLInputElement | null;
  const errorEl = document.getElementById("edit-workout-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("edit-workout-api-error") as HTMLElement | null;
  const submitBtn = document.querySelector("#workout-edit-form .workout-form__submit") as HTMLButtonElement | null;

  if (!nameInput || !errorEl || !apiErrorEl || !submitBtn) return;

  clearValidationError(nameInput, errorEl);
  apiErrorEl.textContent = "";

  const name = nameInput.value.trim();

  if (!name) {
    showValidationError(nameInput, errorEl, "Workout name is required.");
    return;
  }

  if (name.length > 150) {
    showValidationError(nameInput, errorEl, "Workout name must be 150 characters or fewer.");
    return;
  }

  if (editSelectedExercises.size === 0) {
    errorEl.textContent = "At least one exercise is required.";
    return;
  }

  isEditSubmitting = true;
  submitBtn.setAttribute("aria-disabled", "true");
  const originalText = submitBtn.textContent;
  submitBtn.textContent = "Saving...";
  submitBtn.classList.add("workout-form__submit--loading");

  try {
    const exercises = Array.from(editSelectedExercises).map(exerciseId => ({
      exerciseId,
      targetReps: null as string | null,
      targetWeight: null as string | null,
    }));

    const response = await fetch(`/api/workouts/${editingWorkoutId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name, exercises }),
    });

    if (response.ok) {
      const workoutsRes = await fetch("/api/workouts");
      if (workoutsRes.ok) {
        workouts = await workoutsRes.json();
      }

      closeEditModal();
      renderWorkoutList();
    } else {
      const data = await response.json();
      apiErrorEl.textContent = data.error || "An unexpected error occurred. Please try again.";
    }
  } catch {
    apiErrorEl.textContent = "An unexpected error occurred. Please try again.";
  } finally {
    isEditSubmitting = false;
    submitBtn.removeAttribute("aria-disabled");
    submitBtn.textContent = originalText ?? "Save Changes";
    submitBtn.classList.remove("workout-form__submit--loading");
  }
}

function resetCreateForm(): void {
  const nameInput = document.getElementById("workout-name") as HTMLInputElement | null;
  const errorEl = document.getElementById("workout-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("workout-api-error") as HTMLElement | null;

  selectedExercises = new Set();

  if (nameInput) {
    nameInput.value = "";
    nameInput.classList.remove("workout-form__input--error");
    nameInput.removeAttribute("aria-invalid");
  }
  if (errorEl) errorEl.textContent = "";
  if (apiErrorEl) apiErrorEl.textContent = "";

  renderExerciseDropdown();
}

function openDeleteModal(workout: Workout): void {
  const backdrop = document.getElementById("workout-delete-backdrop");
  const descEl = document.getElementById("workout-delete-desc");
  const errorEl = document.getElementById("workout-delete-error");
  const confirmBtn = document.getElementById("workout-delete-confirm") as HTMLButtonElement | null;

  if (!backdrop || !descEl) return;

  deletingWorkoutId = workout.plannedWorkoutId;
  descEl.textContent = `Are you sure you want to delete "${workout.name}"? This action cannot be undone.`;
  if (errorEl) errorEl.textContent = "";
  if (confirmBtn) {
    confirmBtn.textContent = "Delete";
    confirmBtn.removeAttribute("aria-disabled");
    confirmBtn.classList.remove("delete-modal__delete--loading");
  }

  backdrop.style.display = "flex";

  // Focus the cancel button (safer default)
  const cancelBtn = document.getElementById("workout-delete-cancel");
  cancelBtn?.focus();
}

function closeDeleteModal(): void {
  const backdrop = document.getElementById("workout-delete-backdrop");
  if (backdrop) backdrop.style.display = "none";

  deletingWorkoutId = null;
  isDeleting = false;
}

async function handleDelete(): Promise<void> {
  if (isDeleting || deletingWorkoutId === null) return;

  const confirmBtn = document.getElementById("workout-delete-confirm") as HTMLButtonElement | null;
  const errorEl = document.getElementById("workout-delete-error");

  isDeleting = true;
  if (confirmBtn) {
    confirmBtn.setAttribute("aria-disabled", "true");
    confirmBtn.textContent = "Deleting...";
    confirmBtn.classList.add("delete-modal__delete--loading");
  }
  if (errorEl) errorEl.textContent = "";

  try {
    const response = await fetch(`/api/workouts/${deletingWorkoutId}`, {
      method: "DELETE",
    });

    if (response.ok || response.status === 204) {
      const workoutsRes = await fetch("/api/workouts");
      if (workoutsRes.ok) {
        workouts = await workoutsRes.json();
      }

      closeDeleteModal();
      renderWorkoutList();
    } else {
      const data = await response.json();
      if (errorEl) {
        errorEl.textContent = data.error || "An unexpected error occurred. Please try again.";
      }
    }
  } catch {
    if (errorEl) {
      errorEl.textContent = "An unexpected error occurred. Please try again.";
    }
  } finally {
    isDeleting = false;
    if (confirmBtn) {
      confirmBtn.removeAttribute("aria-disabled");
      confirmBtn.textContent = "Delete";
      confirmBtn.classList.remove("delete-modal__delete--loading");
    }
  }
}

function showValidationError(input: HTMLInputElement, errorEl: HTMLElement, message: string): void {
  errorEl.textContent = message;
  input.classList.add("workout-form__input--error");
  input.setAttribute("aria-invalid", "true");
}

function clearValidationError(input: HTMLInputElement, errorEl: HTMLElement): void {
  errorEl.textContent = "";
  input.classList.remove("workout-form__input--error");
  input.removeAttribute("aria-invalid");
}

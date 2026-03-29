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
let selectedMuscleIds: Set<string> = new Set();
let isSubmitting = false;

// Edit modal state
let editingExerciseId: string | null = null;
let selectedEditMuscleIds: Set<string> = new Set();
let isEditSubmitting = false;

// Delete confirmation state
let deletingExerciseId: string | null = null;
let isDeleting = false;

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
      <div class="edit-modal-backdrop" id="edit-modal-backdrop" style="display:none;">
        <div class="edit-modal" role="dialog" aria-modal="true" aria-labelledby="edit-modal-title">
          <h2 class="edit-modal__title" id="edit-modal-title">Edit Exercise</h2>
          <form class="edit-modal__form" id="edit-modal-form" novalidate>
            <div class="exercise-form__group">
              <label class="exercise-form__label" for="edit-exercise-name">Exercise name</label>
              <input class="exercise-form__input" type="text" id="edit-exercise-name" maxlength="150" autocomplete="off" aria-describedby="edit-exercise-error" />
            </div>
            <div class="exercise-form__group">
              <label class="exercise-form__label">Targeted muscles (optional)</label>
              <div class="exercise-form__muscles" id="edit-exercise-muscles" role="group" aria-label="Targeted muscles"></div>
            </div>
            <div class="exercise-form__error" id="edit-exercise-error" role="alert" aria-live="polite"></div>
            <div class="edit-modal__actions">
              <button class="exercise-form__submit" type="submit">Save Changes</button>
              <button class="exercise-form__cancel edit-modal__cancel" type="button" id="edit-modal-cancel">Cancel</button>
            </div>
            <div class="exercise-form__api-error" id="edit-exercise-api-error" role="alert" aria-live="polite"></div>
          </form>
        </div>
      </div>
      <div class="delete-modal-backdrop" id="delete-modal-backdrop" style="display:none;">
        <div class="delete-modal" role="alertdialog" aria-modal="true" aria-labelledby="delete-modal-title" aria-describedby="delete-modal-desc">
          <h2 class="delete-modal__title" id="delete-modal-title">Delete Exercise</h2>
          <p class="delete-modal__desc" id="delete-modal-desc"></p>
          <div class="delete-modal__actions">
            <button class="delete-modal__delete" type="button" id="delete-modal-confirm">Delete</button>
            <button class="delete-modal__cancel" type="button" id="delete-modal-cancel">Cancel</button>
          </div>
          <div class="delete-modal__error" id="delete-modal-error" role="alert" aria-live="polite"></div>
        </div>
      </div>
    </div>
  `;

  editingExerciseId = null;
  selectedMuscleIds = new Set();
  selectedEditMuscleIds = new Set();
  isSubmitting = false;
  isEditSubmitting = false;
  deletingExerciseId = null;
  isDeleting = false;

  initForm();
  initEditModal();
  initDeleteModal();
  await loadData();
}

function initForm(): void {
  const form = document.getElementById("exercise-form") as HTMLFormElement | null;
  if (!form) return;

  form.addEventListener("submit", (event: Event) => {
    event.preventDefault();
    void handleSubmit();
  });
}

function initEditModal(): void {
  const form = document.getElementById("edit-modal-form") as HTMLFormElement | null;
  const cancelBtn = document.getElementById("edit-modal-cancel") as HTMLButtonElement | null;
  const backdrop = document.getElementById("edit-modal-backdrop") as HTMLElement | null;

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

    const actionsDiv = document.createElement("div");
    actionsDiv.className = "exercise-list__actions";

    const editBtn = document.createElement("button");
    editBtn.type = "button";
    editBtn.className = "exercise-list__edit-btn";
    editBtn.setAttribute("aria-label", `Edit ${exercise.name}`);
    editBtn.setAttribute("data-exercise-id", exercise.exerciseId);
    editBtn.innerHTML = `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3.5L17.5 4l3.5 3.5L7.5 21H4z"/><path d="M14.5 5.5l3 3"/></svg>`;
    editBtn.addEventListener("click", () => {
      openEditModal(exercise);
    });

    const deleteBtn = document.createElement("button");
    deleteBtn.className = "exercise-list__delete-btn";
    deleteBtn.setAttribute("aria-label", `Delete ${exercise.name}`);
    deleteBtn.setAttribute("data-exercise-id", exercise.exerciseId);
    deleteBtn.innerHTML = `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/><path d="M10 11v6"/><path d="M14 11v6"/><path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/></svg>`;
    deleteBtn.addEventListener("click", () => {
      openDeleteModal(exercise);
    });

    actionsDiv.appendChild(editBtn);
    actionsDiv.appendChild(deleteBtn);
    li.appendChild(actionsDiv);
    listEl.appendChild(li);
  }
}

async function handleSubmit(): Promise<void> {
  if (isSubmitting) return;

  const nameInput = document.getElementById("exercise-name") as HTMLInputElement | null;
  const errorEl = document.getElementById("exercise-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("exercise-api-error") as HTMLElement | null;
  const submitBtn = document.querySelector("#exercise-form .exercise-form__submit") as HTMLButtonElement | null;

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

    const response = await fetch("/api/exercises", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name, muscleIds }),
    });

    if (response.ok) {
      const exercisesRes = await fetch("/api/exercises");
      if (exercisesRes.ok) {
        exercises = await exercisesRes.json();
      }

      resetCreateForm();
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
    submitBtn.textContent = originalText ?? "Add Exercise";
    submitBtn.classList.remove("exercise-form__submit--loading");
  }
}

function openEditModal(exercise: Exercise): void {
  const backdrop = document.getElementById("edit-modal-backdrop") as HTMLElement | null;
  const nameInput = document.getElementById("edit-exercise-name") as HTMLInputElement | null;
  const errorEl = document.getElementById("edit-exercise-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("edit-exercise-api-error") as HTMLElement | null;

  if (!backdrop || !nameInput) return;

  editingExerciseId = exercise.exerciseId;
  nameInput.value = exercise.name;

  // Clear errors
  if (errorEl) {
    errorEl.textContent = "";
    nameInput.classList.remove("exercise-form__input--error");
    nameInput.removeAttribute("aria-invalid");
  }
  if (apiErrorEl) apiErrorEl.textContent = "";

  // Set muscle toggle states
  selectedEditMuscleIds = new Set(exercise.muscles.map(m => m.muscleId));
  renderEditMuscleToggles();

  backdrop.style.display = "";
  nameInput.focus();
}

function closeEditModal(): void {
  const backdrop = document.getElementById("edit-modal-backdrop") as HTMLElement | null;
  if (backdrop) backdrop.style.display = "none";
  editingExerciseId = null;
  selectedEditMuscleIds = new Set();
}

function renderEditMuscleToggles(): void {
  const container = document.getElementById("edit-exercise-muscles");
  if (!container) return;

  container.innerHTML = "";

  for (const muscle of muscles) {
    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "muscle-toggle";
    btn.textContent = muscle.name;
    btn.setAttribute("role", "checkbox");
    btn.setAttribute("data-muscle-id", muscle.muscleId);

    if (selectedEditMuscleIds.has(muscle.muscleId)) {
      btn.classList.add("muscle-toggle--active");
      btn.setAttribute("aria-checked", "true");
    } else {
      btn.setAttribute("aria-checked", "false");
    }

    btn.addEventListener("click", () => {
      toggleEditMuscle(muscle.muscleId, btn);
    });

    container.appendChild(btn);
  }
}

function toggleEditMuscle(muscleId: string, btn: HTMLButtonElement): void {
  if (selectedEditMuscleIds.has(muscleId)) {
    selectedEditMuscleIds.delete(muscleId);
    btn.classList.remove("muscle-toggle--active");
    btn.setAttribute("aria-checked", "false");
  } else {
    selectedEditMuscleIds.add(muscleId);
    btn.classList.add("muscle-toggle--active");
    btn.setAttribute("aria-checked", "true");
  }
}

async function handleEditSubmit(): Promise<void> {
  if (isEditSubmitting || editingExerciseId === null) return;

  const nameInput = document.getElementById("edit-exercise-name") as HTMLInputElement | null;
  const errorEl = document.getElementById("edit-exercise-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("edit-exercise-api-error") as HTMLElement | null;
  const submitBtn = document.querySelector("#edit-modal-form .exercise-form__submit") as HTMLButtonElement | null;

  if (!nameInput || !errorEl || !apiErrorEl || !submitBtn) return;

  clearValidationError(nameInput, errorEl);
  apiErrorEl.textContent = "";

  const name = nameInput.value.trim();

  if (!name) {
    showValidationError(nameInput, errorEl, "Exercise name is required.");
    return;
  }

  if (name.length > 150) {
    showValidationError(nameInput, errorEl, "Exercise name must be 150 characters or fewer.");
    return;
  }

  isEditSubmitting = true;
  submitBtn.setAttribute("aria-disabled", "true");
  const originalText = submitBtn.textContent;
  submitBtn.textContent = "Saving...";
  submitBtn.classList.add("exercise-form__submit--loading");

  try {
    const muscleIds = Array.from(selectedEditMuscleIds);

    const response = await fetch(`/api/exercises/${editingExerciseId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name, muscleIds }),
    });

    if (response.ok) {
      const exercisesRes = await fetch("/api/exercises");
      if (exercisesRes.ok) {
        exercises = await exercisesRes.json();
      }

      closeEditModal();
      renderExerciseList();
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
    submitBtn.classList.remove("exercise-form__submit--loading");
  }
}

function resetCreateForm(): void {
  const nameInput = document.getElementById("exercise-name") as HTMLInputElement | null;
  const errorEl = document.getElementById("exercise-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("exercise-api-error") as HTMLElement | null;

  selectedMuscleIds = new Set();

  if (nameInput) {
    nameInput.value = "";
    nameInput.classList.remove("exercise-form__input--error");
    nameInput.removeAttribute("aria-invalid");
  }
  if (errorEl) errorEl.textContent = "";
  if (apiErrorEl) apiErrorEl.textContent = "";

  // Reset all muscle toggles in the create form
  const toggleBtns = document.querySelectorAll<HTMLButtonElement>("#exercise-muscles .muscle-toggle");
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

function initDeleteModal(): void {
  const backdrop = document.getElementById("delete-modal-backdrop");
  const cancelBtn = document.getElementById("delete-modal-cancel");
  const confirmBtn = document.getElementById("delete-modal-confirm");

  backdrop?.addEventListener("click", (event: Event) => {
    if (event.target === backdrop) {
      closeDeleteModal();
    }
  });

  backdrop?.addEventListener("keydown", (event: KeyboardEvent) => {
    if (event.key === "Escape") {
      closeDeleteModal();
    }

    // Focus trapping
    if (event.key === "Tab") {
      const focusable = backdrop.querySelectorAll<HTMLElement>(
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

function openDeleteModal(exercise: Exercise): void {
  const backdrop = document.getElementById("delete-modal-backdrop");
  const descEl = document.getElementById("delete-modal-desc");
  const errorEl = document.getElementById("delete-modal-error");
  const confirmBtn = document.getElementById("delete-modal-confirm") as HTMLButtonElement | null;

  if (!backdrop || !descEl) return;

  deletingExerciseId = exercise.exerciseId;
  descEl.textContent = `Are you sure you want to delete "${exercise.name}"? This action cannot be undone.`;
  if (errorEl) errorEl.textContent = "";
  if (confirmBtn) {
    confirmBtn.textContent = "Delete";
    confirmBtn.removeAttribute("aria-disabled");
    confirmBtn.classList.remove("delete-modal__delete--loading");
  }

  backdrop.style.display = "flex";

  // Focus the cancel button (safer default)
  const cancelBtn = document.getElementById("delete-modal-cancel");
  cancelBtn?.focus();
}

function closeDeleteModal(): void {
  const backdrop = document.getElementById("delete-modal-backdrop");
  if (backdrop) backdrop.style.display = "none";

  deletingExerciseId = null;
  isDeleting = false;
}

async function handleDelete(): Promise<void> {
  if (isDeleting || deletingExerciseId === null) return;

  const confirmBtn = document.getElementById("delete-modal-confirm") as HTMLButtonElement | null;
  const errorEl = document.getElementById("delete-modal-error");

  isDeleting = true;
  if (confirmBtn) {
    confirmBtn.setAttribute("aria-disabled", "true");
    confirmBtn.textContent = "Deleting...";
    confirmBtn.classList.add("delete-modal__delete--loading");
  }
  if (errorEl) errorEl.textContent = "";

  try {
    const response = await fetch(`/api/exercises/${deletingExerciseId}`, {
      method: "DELETE",
    });

    if (response.ok || response.status === 204) {
      const exercisesRes = await fetch("/api/exercises");
      if (exercisesRes.ok) {
        exercises = await exercisesRes.json();
      }

      closeDeleteModal();
      renderExerciseList();
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

interface Muscle {
  readonly muscleId: string;
  readonly name: string;
}

let muscles: Muscle[] = [];
let isAddingMuscle = false;
let editingMuscleId: string | null = null;
let isSavingEdit = false;
let deletingMuscleId: string | null = null;
let isDeleting = false;

export async function render(container: HTMLElement): Promise<void> {
  container.innerHTML = `
    <div class="muscles-page">
      <h1 class="muscles-page__title">Targeted Muscles</h1>
      <form class="muscle-form" id="muscle-form" novalidate>
        <div class="muscle-form__group">
          <label class="exercise-form__label" for="muscle-name">Muscle name</label>
          <input class="exercise-form__input muscle-form__input" type="text" id="muscle-name" maxlength="100" autocomplete="off" aria-describedby="muscle-error" />
        </div>
        <div class="exercise-form__error muscle-form__error" id="muscle-error" role="alert" aria-live="polite"></div>
        <div class="exercise-form__actions">
          <button class="exercise-form__submit" type="submit">Add Muscle</button>
        </div>
        <div class="exercise-form__api-error" id="muscle-api-error" role="alert" aria-live="polite"></div>
      </form>

      <section class="muscles-page__list">
        <h2 class="muscles-page__heading">Your Muscles</h2>
        <div class="muscle-grid" id="muscle-grid"></div>
      </section>
      <div class="muscles-page__empty" id="muscles-empty" style="display:none;">
        No muscles yet. Add your first muscle above!
      </div>

      <div class="edit-modal-backdrop" id="muscle-edit-backdrop" style="display:none;">
        <div class="edit-modal muscle-edit-modal" role="dialog" aria-modal="true" aria-labelledby="muscle-edit-title">
          <h2 class="edit-modal__title" id="muscle-edit-title">Edit Muscle</h2>
          <form class="edit-modal__form" id="muscle-edit-form" novalidate>
            <div class="muscle-form__group">
              <label class="exercise-form__label" for="edit-muscle-name">Muscle name</label>
              <input class="exercise-form__input muscle-form__input" type="text" id="edit-muscle-name" maxlength="100" autocomplete="off" aria-describedby="edit-muscle-error" />
            </div>
            <div class="exercise-form__error muscle-form__error" id="edit-muscle-error" role="alert" aria-live="polite"></div>
            <div class="muscle-edit-modal__actions">
              <button class="exercise-form__submit" type="submit">Save Changes</button>
              <button class="muscle-edit-modal__delete" type="button" id="muscle-edit-delete">Delete</button>
              <button class="exercise-form__cancel" type="button" id="muscle-edit-cancel">Cancel</button>
            </div>
            <div class="exercise-form__api-error" id="edit-muscle-api-error" role="alert" aria-live="polite"></div>
          </form>
        </div>
      </div>

      <div class="delete-modal-backdrop" id="muscle-delete-backdrop" style="display:none;">
        <div class="delete-modal" role="alertdialog" aria-modal="true" aria-labelledby="muscle-delete-title" aria-describedby="muscle-delete-desc">
          <h2 class="delete-modal__title" id="muscle-delete-title">Delete Muscle</h2>
          <p class="delete-modal__desc" id="muscle-delete-desc"></p>
          <div class="delete-modal__actions">
            <button class="delete-modal__delete" type="button" id="muscle-delete-confirm">Delete</button>
            <button class="delete-modal__cancel" type="button" id="muscle-delete-cancel">Cancel</button>
          </div>
          <div class="delete-modal__error" id="muscle-delete-error" role="alert" aria-live="polite"></div>
        </div>
      </div>
    </div>
  `;

  isAddingMuscle = false;
  editingMuscleId = null;
  isSavingEdit = false;
  deletingMuscleId = null;
  isDeleting = false;

  initForm();
  initEditModal();
  initDeleteModal();
  await reloadMuscles();
}

function initForm(): void {
  const form = document.getElementById("muscle-form") as HTMLFormElement | null;
  if (!form) return;

  form.addEventListener("submit", (event: Event) => {
    event.preventDefault();
    void handleAddMuscle();
  });
}

function initEditModal(): void {
  const form = document.getElementById("muscle-edit-form") as HTMLFormElement | null;
  const backdrop = document.getElementById("muscle-edit-backdrop") as HTMLElement | null;
  const cancelBtn = document.getElementById("muscle-edit-cancel") as HTMLButtonElement | null;
  const deleteBtn = document.getElementById("muscle-edit-delete") as HTMLButtonElement | null;

  if (!form || !backdrop) return;

  form.addEventListener("submit", (event: Event) => {
    event.preventDefault();
    void handleEditSave();
  });

  cancelBtn?.addEventListener("click", closeEditModal);
  deleteBtn?.addEventListener("click", () => {
    if (editingMuscleId) {
      openDeleteModal(editingMuscleId);
    }
  });

  backdrop.addEventListener("click", (event: Event) => {
    if (event.target === backdrop) {
      closeEditModal();
    }
  });
}

function initDeleteModal(): void {
  const backdrop = document.getElementById("muscle-delete-backdrop") as HTMLElement | null;
  const cancelBtn = document.getElementById("muscle-delete-cancel") as HTMLButtonElement | null;
  const confirmBtn = document.getElementById("muscle-delete-confirm") as HTMLButtonElement | null;

  if (!backdrop) return;

  cancelBtn?.addEventListener("click", closeDeleteModal);
  confirmBtn?.addEventListener("click", () => {
    void handleDelete();
  });

  backdrop.addEventListener("click", (event: Event) => {
    if (event.target === backdrop) {
      closeDeleteModal();
    }
  });
}

async function reloadMuscles(): Promise<void> {
  const apiErrorEl = document.getElementById("muscle-api-error") as HTMLElement | null;

  try {
    const response = await fetch("/api/muscles");
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }

    muscles = await response.json() as Muscle[];
    if (apiErrorEl) apiErrorEl.textContent = "";
    renderMuscleGrid();
  } catch {
    if (apiErrorEl) apiErrorEl.textContent = "Failed to load muscles.";
  }
}

function renderMuscleGrid(): void {
  const grid = document.getElementById("muscle-grid") as HTMLElement | null;
  const empty = document.getElementById("muscles-empty") as HTMLElement | null;
  if (!grid || !empty) return;

  grid.innerHTML = "";

  if (muscles.length === 0) {
    empty.style.display = "";
    return;
  }

  empty.style.display = "none";

  for (const muscle of muscles) {
    const tile = document.createElement("article");
    tile.className = "muscle-tile";

    const name = document.createElement("h3");
    name.className = "muscle-tile__name";
    name.textContent = muscle.name;

    const editBtn = document.createElement("button");
    editBtn.className = "muscle-tile__edit-btn";
    editBtn.type = "button";
    editBtn.textContent = "Edit";
    editBtn.addEventListener("click", () => openEditModal(muscle));

    tile.appendChild(name);
    tile.appendChild(editBtn);
    grid.appendChild(tile);
  }
}

async function handleAddMuscle(): Promise<void> {
  if (isAddingMuscle) return;

  const input = document.getElementById("muscle-name") as HTMLInputElement | null;
  const errorEl = document.getElementById("muscle-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("muscle-api-error") as HTMLElement | null;
  const submitBtn = document.querySelector("#muscle-form .exercise-form__submit") as HTMLButtonElement | null;
  if (!input || !errorEl || !apiErrorEl || !submitBtn) return;

  clearValidationError(input, errorEl);
  apiErrorEl.textContent = "";

  const name = input.value.trim();
  const validationError = validateMuscleName(name);
  if (validationError) {
    showValidationError(input, errorEl, validationError);
    return;
  }

  isAddingMuscle = true;
  submitBtn.setAttribute("aria-disabled", "true");
  submitBtn.textContent = "Adding...";

  try {
    const response = await fetch("/api/muscles", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name }),
    });

    if (response.ok) {
      input.value = "";
      await reloadMuscles();
      input.focus();
      return;
    }

    const data = await response.json() as { error?: string };
    apiErrorEl.textContent = data.error ?? "An unexpected error occurred. Please try again.";
  } catch {
    apiErrorEl.textContent = "Failed to add muscle. Please try again.";
  } finally {
    isAddingMuscle = false;
    submitBtn.removeAttribute("aria-disabled");
    submitBtn.textContent = "Add Muscle";
  }
}

function openEditModal(muscle: Muscle): void {
  const backdrop = document.getElementById("muscle-edit-backdrop") as HTMLElement | null;
  const input = document.getElementById("edit-muscle-name") as HTMLInputElement | null;
  const errorEl = document.getElementById("edit-muscle-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("edit-muscle-api-error") as HTMLElement | null;

  if (!backdrop || !input || !errorEl || !apiErrorEl) return;

  editingMuscleId = muscle.muscleId;
  input.value = muscle.name;
  clearValidationError(input, errorEl);
  apiErrorEl.textContent = "";
  backdrop.style.display = "";
  input.focus();
}

function closeEditModal(): void {
  const backdrop = document.getElementById("muscle-edit-backdrop") as HTMLElement | null;
  if (backdrop) backdrop.style.display = "none";
  editingMuscleId = null;
  isSavingEdit = false;
}

async function handleEditSave(): Promise<void> {
  if (isSavingEdit || editingMuscleId === null) return;

  const input = document.getElementById("edit-muscle-name") as HTMLInputElement | null;
  const errorEl = document.getElementById("edit-muscle-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("edit-muscle-api-error") as HTMLElement | null;
  const submitBtn = document.querySelector("#muscle-edit-form .exercise-form__submit") as HTMLButtonElement | null;
  if (!input || !errorEl || !apiErrorEl || !submitBtn) return;

  clearValidationError(input, errorEl);
  apiErrorEl.textContent = "";

  const name = input.value.trim();
  const validationError = validateMuscleName(name);
  if (validationError) {
    showValidationError(input, errorEl, validationError);
    return;
  }

  isSavingEdit = true;
  submitBtn.setAttribute("aria-disabled", "true");
  submitBtn.textContent = "Saving...";

  try {
    const response = await fetch(`/api/muscles/${editingMuscleId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name }),
    });

    if (response.ok) {
      closeEditModal();
      await reloadMuscles();
      return;
    }

    const data = await response.json() as { error?: string };
    apiErrorEl.textContent = data.error ?? "An unexpected error occurred. Please try again.";
  } catch {
    apiErrorEl.textContent = "Failed to update muscle. Please try again.";
  } finally {
    isSavingEdit = false;
    submitBtn.removeAttribute("aria-disabled");
    submitBtn.textContent = "Save Changes";
  }
}

function openDeleteModal(muscleId: string): void {
  const deleteBackdrop = document.getElementById("muscle-delete-backdrop") as HTMLElement | null;
  const descEl = document.getElementById("muscle-delete-desc") as HTMLElement | null;
  const errorEl = document.getElementById("muscle-delete-error") as HTMLElement | null;
  const editBackdrop = document.getElementById("muscle-edit-backdrop") as HTMLElement | null;
  const muscle = muscles.find((m) => m.muscleId === muscleId);

  if (!deleteBackdrop || !descEl || !muscle) return;

  deletingMuscleId = muscleId;
  descEl.textContent = `Are you sure you want to delete "${muscle.name}"? This action cannot be undone.`;
  if (errorEl) errorEl.textContent = "";
  if (editBackdrop) editBackdrop.style.display = "none";
  deleteBackdrop.style.display = "flex";
}

function closeDeleteModal(): void {
  const deleteBackdrop = document.getElementById("muscle-delete-backdrop") as HTMLElement | null;
  const editBackdrop = document.getElementById("muscle-edit-backdrop") as HTMLElement | null;
  if (deleteBackdrop) deleteBackdrop.style.display = "none";
  if (editBackdrop && editingMuscleId) editBackdrop.style.display = "";
  deletingMuscleId = null;
}

async function handleDelete(): Promise<void> {
  if (isDeleting || deletingMuscleId === null) return;

  const errorEl = document.getElementById("muscle-delete-error") as HTMLElement | null;
  const confirmBtn = document.getElementById("muscle-delete-confirm") as HTMLButtonElement | null;

  isDeleting = true;
  if (errorEl) errorEl.textContent = "";
  if (confirmBtn) {
    confirmBtn.textContent = "Deleting...";
    confirmBtn.setAttribute("aria-disabled", "true");
  }

  try {
    const response = await fetch(`/api/muscles/${deletingMuscleId}`, { method: "DELETE" });
    if (response.ok || response.status === 204) {
      closeDeleteModal();
      closeEditModal();
      await reloadMuscles();
      return;
    }

    const data = await response.json() as { error?: string };
    if (errorEl) errorEl.textContent = data.error ?? "An unexpected error occurred. Please try again.";
  } catch {
    if (errorEl) errorEl.textContent = "Failed to delete muscle. Please try again.";
  } finally {
    isDeleting = false;
    if (confirmBtn) {
      confirmBtn.textContent = "Delete";
      confirmBtn.removeAttribute("aria-disabled");
    }
  }
}

function showValidationError(input: HTMLInputElement, errorEl: HTMLElement, message: string): void {
  errorEl.textContent = message;
  input.classList.add("exercise-form__input--error");
  input.setAttribute("aria-invalid", "true");
}

function validateMuscleName(name: string): string | null {
  if (!name) {
    return "Muscle name is required.";
  }

  if (name.length > 100) {
    return "Muscle name must be 100 characters or fewer.";
  }

  return null;
}

function clearValidationError(input: HTMLInputElement, errorEl: HTMLElement): void {
  errorEl.textContent = "";
  input.classList.remove("exercise-form__input--error");
  input.removeAttribute("aria-invalid");
}

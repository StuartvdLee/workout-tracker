import { getVisibleModalButtons, trapModalTabKey } from "../prestart-modal.js";

interface Muscle {
  readonly muscleId: string;
  readonly name: string;
}

interface ApiError {
  readonly error?: string;
}

let muscles: Muscle[] = [];
let isSubmitting = false;
let editingMuscleId: string | null = null;
let isEditSubmitting = false;
let deletingMuscleId: string | null = null;

export async function render(container: HTMLElement): Promise<void> {
  container.innerHTML = `
    <div class="muscles-page">
      <h1 class="muscles-page__title">Muscles</h1>
      <form class="muscle-form" id="muscle-form" novalidate>
        <div class="muscle-form__group">
          <label class="muscle-form__label" for="muscle-name">Muscle name</label>
          <input
            class="muscle-form__input"
            type="text"
            id="muscle-name"
            name="muscle-name"
            maxlength="100"
            aria-describedby="muscle-error"
            autocomplete="off"
          />
        </div>
        <div class="muscle-form__error" id="muscle-error" role="alert" aria-live="polite"></div>
        <div class="muscle-form__actions">
          <button class="muscle-form__submit" type="submit">Add Muscle</button>
        </div>
        <div class="muscle-form__api-error" id="muscle-api-error" role="alert" aria-live="polite"></div>
      </form>
      <section class="muscle-list">
        <h2 class="muscle-list__heading">Your Targeted Muscles</h2>
        <div class="muscle-list__loading" id="muscle-loading">Loading...</div>
        <div class="muscle-list__empty" id="muscle-empty" style="display:none;">No targeted muscles yet. Add your first targeted muscle above!</div>
        <div class="muscle-list__grid" id="muscle-grid"></div>
      </section>
      <div class="edit-modal-backdrop" id="edit-modal-backdrop" style="display:none;">
        <div class="edit-modal" id="edit-modal" role="dialog" aria-modal="true" aria-labelledby="edit-modal-title">
          <h2 class="edit-modal__title" id="edit-modal-title">Edit Muscle</h2>
          <form class="edit-modal__form" id="edit-modal-form" novalidate>
            <div class="muscle-form__group">
              <label class="muscle-form__label" for="edit-muscle-name">Muscle name</label>
              <input
                class="muscle-form__input"
                type="text"
                id="edit-muscle-name"
                maxlength="100"
                autocomplete="off"
                aria-describedby="edit-muscle-error"
              />
            </div>
            <div class="edit-modal__error" id="edit-muscle-error" role="alert" aria-live="polite"></div>
            <div class="edit-modal__actions">
              <button class="edit-modal__submit-btn" type="submit">Save</button>
              <button class="edit-modal__delete-btn" type="button" id="edit-modal-delete-btn">Delete</button>
            </div>
            <div class="edit-modal__api-error" id="edit-modal-api-error" role="alert" aria-live="polite"></div>
          </form>
        </div>
      </div>
      <div class="delete-modal-backdrop" id="delete-confirm-backdrop" style="display:none;">
        <div class="delete-modal" role="alertdialog" aria-modal="true" aria-labelledby="delete-confirm-title" aria-describedby="delete-confirm-desc">
          <h2 class="delete-modal__title" id="delete-confirm-title">Delete Muscle</h2>
          <p class="delete-modal__desc" id="delete-confirm-desc"></p>
          <div class="delete-modal__actions">
            <button class="delete-modal__delete" type="button" id="delete-confirm-btn">Delete</button>
            <button class="delete-modal__cancel" type="button" id="delete-confirm-cancel">Cancel</button>
          </div>
          <div class="delete-modal__error" id="delete-confirm-error" role="alert" aria-live="polite"></div>
        </div>
      </div>
    </div>
  `;

  muscles = [];
  isSubmitting = false;
  editingMuscleId = null;
  isEditSubmitting = false;
  deletingMuscleId = null;

  initEventListeners();
  await loadMuscles();
}

function initEventListeners(): void {
  const form = document.getElementById("muscle-form") as HTMLFormElement | null;
  const editForm = document.getElementById("edit-modal-form") as HTMLFormElement | null;
  const editBackdrop = document.getElementById("edit-modal-backdrop") as HTMLElement | null;
  const editModal = document.getElementById("edit-modal") as HTMLElement | null;
  const deleteBtn = document.getElementById("edit-modal-delete-btn") as HTMLButtonElement | null;
  const deleteConfirmBackdrop = document.getElementById("delete-confirm-backdrop") as HTMLElement | null;
  const deleteConfirmBtn = document.getElementById("delete-confirm-btn") as HTMLButtonElement | null;
  const deleteConfirmCancel = document.getElementById("delete-confirm-cancel") as HTMLButtonElement | null;

  form?.addEventListener("submit", (event: SubmitEvent) => {
    void handleAddMuscle(event);
  });

  editForm?.addEventListener("submit", (event: SubmitEvent) => {
    void handleEditSave(event);
  });

  deleteBtn?.addEventListener("click", () => {
    if (editingMuscleId !== null) {
      const muscle = muscles.find((m) => m.muscleId === editingMuscleId);
      if (muscle) {
        openDeleteConfirmModal(muscle);
      }
    }
  });

  deleteConfirmBtn?.addEventListener("click", () => {
    void handleConfirmDelete();
  });

  deleteConfirmCancel?.addEventListener("click", () => {
    closeDeleteConfirmModal();
  });

  editModal?.addEventListener("click", (event: Event) => {
    event.stopPropagation();
  });

  editBackdrop?.addEventListener("click", (event: Event) => {
    if (event.target === editBackdrop) {
      closeEditModal();
    }
  });

  editBackdrop?.addEventListener("keydown", (event: KeyboardEvent) => {
    if (event.key === "Escape") {
      closeEditModal();
      return;
    }

    if (!editModal) {
      return;
    }

    trapEditModalTabKey(event, editModal);
  });

  deleteConfirmBackdrop?.addEventListener("keydown", (event: KeyboardEvent) => {
    if (event.key === "Escape") {
      closeDeleteConfirmModal();
    }
  });
}

async function loadMuscles(): Promise<void> {
  const loadingEl = document.getElementById("muscle-loading") as HTMLElement | null;
  const apiErrorEl = document.getElementById("muscle-api-error") as HTMLElement | null;

  if (loadingEl) {
    loadingEl.style.display = "";
  }

  if (apiErrorEl) {
    apiErrorEl.textContent = "";
  }

  try {
    const response = await fetch("/api/muscles");
    if (!response.ok) {
      if (apiErrorEl) {
        apiErrorEl.textContent = await getErrorMessage(response, "Failed to load muscles. Please try again.");
      }
      muscles = [];
      return;
    }

    muscles = await response.json() as Muscle[];
    sortMuscles();
  } catch {
    muscles = [];
    if (apiErrorEl) {
      apiErrorEl.textContent = "Failed to load muscles. Please try again.";
    }
  } finally {
    renderMuscleGrid();
    if (loadingEl) {
      loadingEl.style.display = "none";
    }
  }
}

function renderMuscleGrid(): void {
  const gridEl = document.getElementById("muscle-grid") as HTMLElement | null;
  const emptyEl = document.getElementById("muscle-empty") as HTMLElement | null;

  if (!gridEl || !emptyEl) {
    return;
  }

  gridEl.innerHTML = "";

  if (muscles.length === 0) {
    emptyEl.style.display = "";
    return;
  }

  emptyEl.style.display = "none";

  for (const muscle of muscles) {
    const card = document.createElement("button");
    card.type = "button";
    card.className = "muscle-card";
    card.setAttribute("data-muscle-id", muscle.muscleId);
    card.setAttribute("aria-label", `Edit ${muscle.name}`);
    card.addEventListener("click", () => {
      openEditModal(muscle);
    });

    const name = document.createElement("span");
    name.className = "muscle-card__name";
    name.textContent = muscle.name;
    card.appendChild(name);

    gridEl.appendChild(card);
  }
}

async function handleAddMuscle(event: SubmitEvent): Promise<void> {
  event.preventDefault();

  if (isSubmitting) {
    return;
  }

  const input = document.getElementById("muscle-name") as HTMLInputElement | null;
  const errorEl = document.getElementById("muscle-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("muscle-api-error") as HTMLElement | null;
  const submitBtn = document.querySelector("#muscle-form .muscle-form__submit") as HTMLButtonElement | null;

  if (!input || !errorEl || !apiErrorEl || !submitBtn) {
    return;
  }

  clearValidationError(input, errorEl);
  apiErrorEl.textContent = "";

  const name = input.value.trim();
  if (!name) {
    showValidationError(input, errorEl, "Muscle name is required.");
    return;
  }

  if (name.length > 100) {
    showValidationError(input, errorEl, "Muscle name must be 100 characters or fewer.");
    return;
  }

  isSubmitting = true;
  submitBtn.setAttribute("aria-disabled", "true");
  submitBtn.textContent = "Adding...";

  try {
    const response = await fetch("/api/muscles", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name }),
    });

    if (!response.ok) {
      apiErrorEl.textContent = await getErrorMessage(response, "Failed to add muscle. Please try again.");
      return;
    }

    const createdMuscle = await response.json() as Muscle;
    insertMuscleAlphabetically(createdMuscle);
    renderMuscleGrid();
    input.value = "";
    clearValidationError(input, errorEl);
    apiErrorEl.textContent = "";
    input.focus();
  } catch {
    apiErrorEl.textContent = "Failed to add muscle. Please try again.";
  } finally {
    isSubmitting = false;
    submitBtn.removeAttribute("aria-disabled");
    submitBtn.textContent = "Add Muscle";
  }
}

function openEditModal(muscle: Muscle): void {
  const backdrop = document.getElementById("edit-modal-backdrop") as HTMLElement | null;
  const input = document.getElementById("edit-muscle-name") as HTMLInputElement | null;
  const errorEl = document.getElementById("edit-muscle-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("edit-modal-api-error") as HTMLElement | null;

  if (!backdrop || !input || !errorEl || !apiErrorEl) {
    return;
  }

  editingMuscleId = muscle.muscleId;
  input.value = muscle.name;
  clearValidationError(input, errorEl);
  apiErrorEl.textContent = "";

  backdrop.style.display = "";
  input.focus();
}

function closeEditModal(): void {
  const backdrop = document.getElementById("edit-modal-backdrop") as HTMLElement | null;
  const input = document.getElementById("edit-muscle-name") as HTMLInputElement | null;
  const errorEl = document.getElementById("edit-muscle-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("edit-modal-api-error") as HTMLElement | null;
  const submitBtn = document.querySelector("#edit-modal-form .edit-modal__submit-btn") as HTMLButtonElement | null;

  if (backdrop) {
    backdrop.style.display = "none";
  }

  if (input && errorEl) {
    input.value = "";
    clearValidationError(input, errorEl);
  }

  if (apiErrorEl) {
    apiErrorEl.textContent = "";
  }

  if (submitBtn) {
    submitBtn.textContent = "Save";
    submitBtn.removeAttribute("aria-disabled");
  }

  editingMuscleId = null;
  isEditSubmitting = false;
}

async function handleEditSave(event: SubmitEvent): Promise<void> {
  event.preventDefault();

  if (isEditSubmitting || editingMuscleId === null) {
    return;
  }

  const input = document.getElementById("edit-muscle-name") as HTMLInputElement | null;
  const errorEl = document.getElementById("edit-muscle-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("edit-modal-api-error") as HTMLElement | null;
  const submitBtn = document.querySelector("#edit-modal-form .edit-modal__submit-btn") as HTMLButtonElement | null;

  if (!input || !errorEl || !apiErrorEl || !submitBtn) {
    return;
  }

  clearValidationError(input, errorEl);
  apiErrorEl.textContent = "";

  const name = input.value.trim();
  if (!name) {
    showValidationError(input, errorEl, "Muscle name is required.");
    return;
  }

  if (name.length > 100) {
    showValidationError(input, errorEl, "Muscle name must be 100 characters or fewer.");
    return;
  }

  isEditSubmitting = true;
  submitBtn.setAttribute("aria-disabled", "true");
  submitBtn.textContent = "Saving...";

  try {
    const response = await fetch(`/api/muscles/${editingMuscleId}`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name }),
    });

    if (!response.ok) {
      apiErrorEl.textContent = await getErrorMessage(response, "Failed to update muscle. Please try again.");
      return;
    }

    const updatedMuscle = await response.json() as Muscle;
    updateMuscle(updatedMuscle);
    renderMuscleGrid();
    closeEditModal();
  } catch {
    apiErrorEl.textContent = "Failed to update muscle. Please try again.";
  } finally {
    if (editingMuscleId !== null) {
      isEditSubmitting = false;
      submitBtn.removeAttribute("aria-disabled");
      submitBtn.textContent = "Save";
    }
  }
}

function openDeleteConfirmModal(muscle: Muscle): void {
  closeEditModal();

  const backdrop = document.getElementById("delete-confirm-backdrop") as HTMLElement | null;
  const descEl = document.getElementById("delete-confirm-desc") as HTMLElement | null;
  const confirmBtn = document.getElementById("delete-confirm-btn") as HTMLButtonElement | null;
  const cancelBtn = document.getElementById("delete-confirm-cancel") as HTMLButtonElement | null;
  const errorEl = document.getElementById("delete-confirm-error") as HTMLElement | null;

  if (!backdrop || !descEl) {
    return;
  }

  deletingMuscleId = muscle.muscleId;
  descEl.textContent = `Are you sure you want to delete "${muscle.name}"? This action cannot be undone.`;

  if (confirmBtn) {
    confirmBtn.textContent = "Delete";
    confirmBtn.removeAttribute("aria-disabled");
    confirmBtn.classList.remove("delete-modal__delete--loading");
  }

  if (cancelBtn) {
    cancelBtn.disabled = false;
  }

  if (errorEl) {
    errorEl.textContent = "";
  }

  backdrop.style.display = "";
  confirmBtn?.focus();
}

function closeDeleteConfirmModal(): void {
  const backdrop = document.getElementById("delete-confirm-backdrop") as HTMLElement | null;
  if (backdrop) {
    backdrop.style.display = "none";
  }
  deletingMuscleId = null;
}

async function handleConfirmDelete(): Promise<void> {
  if (deletingMuscleId === null) {
    return;
  }

  const muscleId = deletingMuscleId;
  const confirmBtn = document.getElementById("delete-confirm-btn") as HTMLButtonElement | null;
  const cancelBtn = document.getElementById("delete-confirm-cancel") as HTMLButtonElement | null;
  const errorEl = document.getElementById("delete-confirm-error") as HTMLElement | null;

  if (errorEl) {
    errorEl.textContent = "";
  }

  if (confirmBtn) {
    confirmBtn.setAttribute("aria-disabled", "true");
    confirmBtn.textContent = "Deleting...";
    confirmBtn.classList.add("delete-modal__delete--loading");
  }

  if (cancelBtn) {
    cancelBtn.disabled = true;
  }

  try {
    const response = await fetch(`/api/muscles/${muscleId}`, {
      method: "DELETE",
    });

    if (!response.ok) {
      if (errorEl) {
        errorEl.textContent = await getErrorMessage(response, "Failed to delete muscle. Please try again.");
      }
      if (confirmBtn) {
        confirmBtn.removeAttribute("aria-disabled");
        confirmBtn.textContent = "Delete";
        confirmBtn.classList.remove("delete-modal__delete--loading");
      }
      if (cancelBtn) {
        cancelBtn.disabled = false;
      }
      return;
    }

    muscles = muscles.filter((muscle) => muscle.muscleId !== muscleId);
    closeDeleteConfirmModal();
    renderMuscleGrid();
  } catch {
    if (errorEl) {
      errorEl.textContent = "Failed to delete muscle. Please try again.";
    }
    if (confirmBtn) {
      confirmBtn.removeAttribute("aria-disabled");
      confirmBtn.textContent = "Delete";
      confirmBtn.classList.remove("delete-modal__delete--loading");
    }
    if (cancelBtn) {
      cancelBtn.disabled = false;
    }
  }
}

function trapEditModalTabKey(event: KeyboardEvent, modal: HTMLElement): void {
  if (event.key !== "Tab") {
    return;
  }

  const input = document.getElementById("edit-muscle-name") as HTMLInputElement | null;
  const buttons = getVisibleModalButtons(modal);

  if (!input || buttons.length === 0) {
    trapModalTabKey(event, modal);
    return;
  }

  const firstButton = buttons[0];
  const lastButton = buttons[buttons.length - 1];

  if (event.shiftKey && document.activeElement === input) {
    event.preventDefault();
    lastButton.focus();
    return;
  }

  if (event.shiftKey && document.activeElement === firstButton) {
    return;
  }

  if (!event.shiftKey && document.activeElement === lastButton) {
    event.preventDefault();
    input.focus();
    return;
  }

  trapModalTabKey(event, modal);
}

function insertMuscleAlphabetically(muscle: Muscle): void {
  muscles = [...muscles, muscle];
  sortMuscles();
}

function updateMuscle(updatedMuscle: Muscle): void {
  muscles = muscles.map((muscle) =>
    muscle.muscleId === updatedMuscle.muscleId ? updatedMuscle : muscle
  );
  sortMuscles();
}

function sortMuscles(): void {
  muscles = [...muscles].sort((left, right) => left.name.localeCompare(right.name));
}

function showValidationError(input: HTMLInputElement, errorEl: HTMLElement, message: string): void {
  errorEl.textContent = message;
  input.classList.add("muscle-form__input--error");
  input.setAttribute("aria-invalid", "true");
}

function clearValidationError(input: HTMLInputElement, errorEl: HTMLElement): void {
  errorEl.textContent = "";
  input.classList.remove("muscle-form__input--error");
  input.removeAttribute("aria-invalid");
}

async function getErrorMessage(response: Response, fallback: string): Promise<string> {
  try {
    const data = await response.json() as ApiError;
    return data.error ?? fallback;
  } catch {
    return fallback;
  }
}

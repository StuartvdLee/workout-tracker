import { navigate } from "../router.js";
import { trapModalTabKey } from "../prestart-modal.js";
import { reorder, shuffle } from "../utils.js";

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
let selectedExercises: string[] = [];
let isSubmitting = false;

// Edit modal state
let editingWorkoutId: string | null = null;
let editSelectedExercises: string[] = [];
let isEditSubmitting = false;

// Delete confirmation state
let deletingWorkoutId: string | null = null;
let isDeleting = false;

// Pre-start modal state
let prestartWorkout: Workout | null = null;
let prestartTriggerBtn: HTMLButtonElement | null = null;

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
        <div id="workout-selected-section" style="display:none;">
          <h3 class="workout-selected__heading">Selected exercises</h3>
          <div class="sr-only" aria-live="polite" aria-atomic="true" id="workout-reorder-announce"></div>
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
            <div id="edit-selected-section" style="display:none;">
              <h3 class="workout-selected__heading">Selected exercises</h3>
              <div class="sr-only" aria-live="polite" aria-atomic="true" id="edit-reorder-announce"></div>
              <ul class="workout-selected__list" id="edit-selected-list"></ul>
            </div>
            <div class="workout-form__error" id="edit-workout-error" role="alert" aria-live="polite"></div>
            <div class="edit-modal__actions">
              <button class="workout-form__submit" type="submit">Save Changes</button>
              <button class="workout-form__cancel edit-modal__cancel" type="button" id="workout-edit-cancel">Cancel</button>
            </div>
            <div class="workout-form__api-error" id="edit-workout-api-error" role="alert" aria-live="polite"></div>
          </form>
          <button class="edit-modal__close" id="workout-edit-close" type="button" aria-label="Close">&#x2715;</button>
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
      <div class="prestart-modal-backdrop" id="workout-prestart-backdrop" style="display:none;">
        <div class="prestart-modal" role="dialog" aria-modal="true" aria-labelledby="prestart-modal-title">
          <h2 class="prestart-modal__title" id="prestart-modal-title">Randomise exercise order?</h2>
          <div class="prestart-modal__actions">
            <button class="prestart-modal__no-btn" type="button" id="prestart-no">No</button>
            <button class="prestart-modal__yes-btn" type="button" id="prestart-yes">Yes</button>
          </div>
          <button class="edit-modal__close" id="prestart-close" type="button" aria-label="Close">&#x2715;</button>
        </div>
      </div>
    </div>
  `;

  editingWorkoutId = null;
  selectedExercises = [];
  editSelectedExercises = [];
  isSubmitting = false;
  isEditSubmitting = false;
  deletingWorkoutId = null;
  isDeleting = false;
  prestartWorkout = null;
  prestartTriggerBtn = null;

  initForm();
  initEditModal();
  initDeleteModal();
  initPreStartModal();
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
    if (exerciseId && !selectedExercises.includes(exerciseId)) {
      selectedExercises.push(exerciseId);
      renderExerciseDropdown();
    }
  });

  initDragAndDrop("workout-selected-list", "workout-reorder-announce", () => selectedExercises, renderExerciseDropdown);
}

function initEditModal(): void {
  const form = document.getElementById("workout-edit-form") as HTMLFormElement | null;
  const cancelBtn = document.getElementById("workout-edit-cancel") as HTMLButtonElement | null;
  const closeBtn = document.getElementById("workout-edit-close") as HTMLButtonElement | null;
  const backdrop = document.getElementById("workout-edit-backdrop") as HTMLElement | null;

  if (!form || !backdrop) return;

  form.addEventListener("submit", (event: Event) => {
    event.preventDefault();
    void handleEditSubmit();
  });

  cancelBtn?.addEventListener("click", () => {
    closeEditModal();
  });

  closeBtn?.addEventListener("click", () => {
    if (!isEditSubmitting) {
      closeEditModal();
    }
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
        'input:not([disabled]), button:not([disabled]), [tabindex]:not([tabindex="-1"])'
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
    if (!selectedExercises.includes(exercise.exerciseId)) {
      const option = document.createElement("option");
      option.value = exercise.exerciseId;
      option.textContent = exercise.name;
      select.appendChild(option);
    }
  }

  select.value = "";
  renderSelectedExercisesList();
}

function renderSelectedExercisesList(): void {
  const list = document.getElementById("workout-selected-list");
  const section = document.getElementById("workout-selected-section");
  if (!list || !section) return;

  list.innerHTML = "";
  section.style.display = selectedExercises.length > 0 ? "" : "none";

  for (const exerciseId of selectedExercises) {
    const exercise = availableExercises.find(e => e.exerciseId === exerciseId);
    if (!exercise) continue;
    const index = selectedExercises.indexOf(exerciseId);
    list.appendChild(buildSelectedExerciseItem(exercise.name, exerciseId, index, selectedExercises.length, () => {
      selectedExercises = selectedExercises.filter(id => id !== exerciseId);
      renderExerciseDropdown();
    }));
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
      openPreStartModal(workout, startBtn);
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

  if (selectedExercises.length === 0) {
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
    const exercises = selectedExercises.map(exerciseId => ({
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

    // Populate selected exercises from the workout in persisted sequence order
    editSelectedExercises = fullWorkout.exercises.map(ex => ex.exerciseId);

    initDragAndDrop("edit-selected-list", "edit-reorder-announce", () => editSelectedExercises, renderEditExerciseDropdown);

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
  editSelectedExercises = [];
}

function renderEditExerciseDropdown(): void {
  const select = document.getElementById("edit-exercise-select") as HTMLSelectElement | null;
  if (!select) return;

  // Attach change listener once (remove old one by replacing the element's listener via a flag)
  if (!select.dataset["listenerAttached"]) {
    select.dataset["listenerAttached"] = "1";
    select.addEventListener("change", () => {
      const exerciseId = select.value;
      if (exerciseId && !editSelectedExercises.includes(exerciseId)) {
        editSelectedExercises.push(exerciseId);
        renderEditExerciseDropdown();
      }
    });
  }

  while (select.options.length > 1) select.remove(1);

  for (const exercise of availableExercises) {
    if (!editSelectedExercises.includes(exercise.exerciseId)) {
      const option = document.createElement("option");
      option.value = exercise.exerciseId;
      option.textContent = exercise.name;
      select.appendChild(option);
    }
  }

  select.value = "";
  renderEditSelectedExercisesList();
}

function renderEditSelectedExercisesList(): void {
  const list = document.getElementById("edit-selected-list");
  const section = document.getElementById("edit-selected-section");
  if (!list || !section) return;

  list.innerHTML = "";
  section.style.display = editSelectedExercises.length > 0 ? "" : "none";

  for (const exerciseId of editSelectedExercises) {
    const exercise = availableExercises.find(e => e.exerciseId === exerciseId);
    if (!exercise) continue;
    const index = editSelectedExercises.indexOf(exerciseId);
    list.appendChild(buildSelectedExerciseItem(exercise.name, exerciseId, index, editSelectedExercises.length, () => {
      editSelectedExercises = editSelectedExercises.filter(id => id !== exerciseId);
      renderEditExerciseDropdown();
    }));
  }
}

function buildSelectedExerciseItem(
  name: string,
  exerciseId: string,
  index: number,
  listLength: number,
  onRemove: () => void
): HTMLLIElement {
  const li = document.createElement("li");
  li.className = "workout-selected__item";
  li.setAttribute("data-exercise-id", exerciseId);
  li.setAttribute("data-index", String(index));

  const showHandle = listLength >= 2;

  if (showHandle) {
    li.setAttribute("draggable", "true");
    li.setAttribute("aria-roledescription", "sortable item");

    const handle = document.createElement("button");
    handle.type = "button";
    handle.className = "workout-selected__drag-handle";
    handle.setAttribute("aria-label", `Drag to reorder ${name}`);
    handle.setAttribute("aria-pressed", "false");
    handle.innerHTML = `<svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><circle cx="5" cy="3" r="1.5"/><circle cx="5" cy="8" r="1.5"/><circle cx="5" cy="13" r="1.5"/><circle cx="11" cy="3" r="1.5"/><circle cx="11" cy="8" r="1.5"/><circle cx="11" cy="13" r="1.5"/></svg>`;
    li.appendChild(handle);
  }

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

function initDragAndDrop(
  listId: string,
  announceId: string,
  getArray: () => string[],
  onReorder: () => void
): void {
  const list = document.getElementById(listId);
  if (!list) return;

  // Guard: attach listeners only once per list element lifetime
  if (list.dataset["dndAttached"]) return;
  list.dataset["dndAttached"] = "1";

  let draggingIndex = -1;
  let draggingLi: HTMLElement | null = null;
  let dropHandled = false;
  // Keyboard reorder state
  let originalPickupIndex = -1;
  let snapshotBeforePickup: string[] = [];

  function getAnnounce(): HTMLElement | null {
    return document.getElementById(announceId);
  }

  function announce(msg: string): void {
    const el = getAnnounce();
    if (el) el.textContent = msg;
  }

  function getLiAtIndex(idx: number): HTMLElement | null {
    return list!.querySelector(`li[data-index="${idx}"]`);
  }

  /** Returns the current 0-based position of li among its siblings in the list. */
  function getLiveDomIndex(li: HTMLElement): number {
    return Array.from(list!.children).indexOf(li);
  }

  // ── HTML5 DnD ──────────────────────────────────────────────────────────────

  list.addEventListener("dragstart", (e: Event) => {
    const ev = e as DragEvent;
    const li = (ev.target as HTMLElement).closest("li[data-index]") as HTMLElement | null;
    if (!li) return;
    draggingIndex = parseInt(li.dataset["index"] ?? "-1", 10);
    draggingLi = li;
    dropHandled = false;
    ev.dataTransfer?.setData("text/plain", String(draggingIndex)); // Firefox compat
    if (ev.dataTransfer) ev.dataTransfer.effectAllowed = "move";
    // Defer the opacity so the drag ghost is captured at full opacity first
    setTimeout(() => li.classList.add("workout-selected__item--dragging"), 0);
    document.body.classList.add("is-dragging");
  });

  list.addEventListener("dragover", (e: Event) => {
    const ev = e as DragEvent;
    ev.preventDefault();
    if (ev.dataTransfer) ev.dataTransfer.dropEffect = "move";
    if (!draggingLi) return;
    const targetLi = (ev.target as HTMLElement).closest("li[data-index]") as HTMLElement | null;
    if (!targetLi || targetLi === draggingLi) return;
    // Insert before or after target based on cursor position relative to its midpoint
    const rect = targetLi.getBoundingClientRect();
    if (ev.clientY < rect.top + rect.height / 2) {
      list!.insertBefore(draggingLi, targetLi);
    } else {
      list!.insertBefore(draggingLi, targetLi.nextSibling);
    }
  });

  list.addEventListener("dragenter", (e: Event) => {
    (e as DragEvent).preventDefault();
  });

  list.addEventListener("drop", (e: Event) => {
    const ev = e as DragEvent;
    ev.preventDefault();
    document.body.classList.remove("is-dragging");
    if (!draggingLi) return;
    const finalIndex = getLiveDomIndex(draggingLi);
    draggingLi.classList.remove("workout-selected__item--dragging");
    dropHandled = true;
    const arr = getArray();
    if (finalIndex !== -1 && finalIndex !== draggingIndex) {
      reorder(arr, draggingIndex, finalIndex);
      announce(`Exercise moved to position ${finalIndex + 1} of ${arr.length}`);
    }
    draggingLi = null;
    draggingIndex = -1;
    onReorder();
  });

  list.addEventListener("dragend", () => {
    document.body.classList.remove("is-dragging");
    if (draggingLi) {
      draggingLi.classList.remove("workout-selected__item--dragging");
      draggingLi = null;
    }
    // If the drop didn't fire (e.g. dropped outside the list), restore order
    if (!dropHandled) {
      onReorder();
    }
    dropHandled = false;
    draggingIndex = -1;
  });

  // ── Touch ──────────────────────────────────────────────────────────────────

  let touchDragIndex = -1;
  let touchClone: HTMLElement | null = null;
  let touchCloneOffsetX = 0;
  let touchCloneOffsetY = 0;

  list.addEventListener("touchstart", (e: Event) => {
    const ev = e as TouchEvent;
    const handle = (ev.target as HTMLElement).closest(".workout-selected__drag-handle") as HTMLElement | null;
    if (!handle) return;
    const li = handle.closest("li[data-index]") as HTMLElement | null;
    if (!li) return;

    touchDragIndex = parseInt(li.dataset["index"] ?? "-1", 10);

    const touch = ev.touches[0];
    const rect = li.getBoundingClientRect();
    touchCloneOffsetX = touch.clientX - rect.left;
    touchCloneOffsetY = touch.clientY - rect.top;

    touchClone = li.cloneNode(true) as HTMLElement;
    touchClone.style.cssText = `
      position: fixed;
      left: ${rect.left}px;
      top: ${rect.top}px;
      width: ${rect.width}px;
      pointer-events: none;
      z-index: 9999;
      opacity: 0.85;
      box-shadow: 0 4px 12px rgba(0,0,0,0.15);
    `;
    document.body.appendChild(touchClone);
    li.classList.add("workout-selected__item--dragging");
    document.body.classList.add("is-dragging");
  }, { passive: true });

  list.addEventListener("touchmove", (e: Event) => {
    const ev = e as TouchEvent;
    if (touchDragIndex === -1 || !touchClone) return;
    ev.preventDefault();

    const touch = ev.touches[0];
    touchClone.style.left = `${touch.clientX - touchCloneOffsetX}px`;
    touchClone.style.top = `${touch.clientY - touchCloneOffsetY}px`;

    // Live-reorder: find the real li under the finger and move the dragged li in the DOM
    touchClone.style.visibility = "hidden";
    const target = document.elementFromPoint(touch.clientX, touch.clientY)?.closest("li[data-index]") as HTMLElement | null;
    touchClone.style.visibility = "";
    const touchLi = getLiAtIndex(touchDragIndex) ?? list!.querySelector(".workout-selected__item--dragging") as HTMLElement | null;
    if (target && touchLi && target !== touchLi) {
      const targetRect = target.getBoundingClientRect();
      if (touch.clientY < targetRect.top + targetRect.height / 2) {
        list!.insertBefore(touchLi, target);
      } else {
        list!.insertBefore(touchLi, target.nextSibling);
      }
    }
  }, { passive: false });

  list.addEventListener("touchend", (_e: Event) => {
    if (touchDragIndex === -1) return;

    document.body.classList.remove("is-dragging");

    if (touchClone) touchClone.style.visibility = "hidden";
    const touchLi = list!.querySelector(".workout-selected__item--dragging") as HTMLElement | null;
    const finalIndex = touchLi ? getLiveDomIndex(touchLi) : -1;

    if (touchClone) {
      touchClone.remove();
      touchClone = null;
    }

    touchLi?.classList.remove("workout-selected__item--dragging");

    const arr = getArray();
    if (finalIndex !== -1 && finalIndex !== touchDragIndex) {
      reorder(arr, touchDragIndex, finalIndex);
      announce(`Exercise moved to position ${finalIndex + 1} of ${arr.length}`);
    }
    touchDragIndex = -1;
    onReorder();
  });

  // ── Keyboard ───────────────────────────────────────────────────────────────

  list.addEventListener("keydown", (e: Event) => {
    const ev = e as KeyboardEvent;
    const handle = (ev.target as HTMLElement).closest(".workout-selected__drag-handle") as HTMLElement | null;
    if (!handle) return;

    const li = handle.closest("li[data-index]") as HTMLElement | null;
    if (!li) return;

    const currentIndex = parseInt(li.dataset["index"] ?? "-1", 10);
    const arr = getArray();
    const isPickedUp = handle.getAttribute("aria-pressed") === "true";

    if (ev.key === " " || ev.key === "Enter") {
      ev.preventDefault();
      if (!isPickedUp) {
        snapshotBeforePickup = [...arr];
        originalPickupIndex = currentIndex;
        handle.setAttribute("aria-pressed", "true");
        announce(`${li.querySelector(".workout-selected__name")?.textContent ?? "Exercise"} picked up. Use arrow keys to move, Space or Enter to drop, Escape to cancel.`);
      } else {
        handle.setAttribute("aria-pressed", "false");
        originalPickupIndex = -1;
        snapshotBeforePickup = [];
        announce(`Exercise dropped at position ${currentIndex + 1} of ${arr.length}`);
      }
    } else if ((ev.key === "ArrowUp" || ev.key === "ArrowDown") && isPickedUp) {
      ev.preventDefault();
      const direction = ev.key === "ArrowUp" ? -1 : 1;
      const newIndex = currentIndex + direction;
      if (newIndex < 0 || newIndex >= arr.length) return;
      reorder(arr, currentIndex, newIndex);
      onReorder();
      // Re-focus the handle at the new index after re-render
      requestAnimationFrame(() => {
        const newLi = getLiAtIndex(newIndex);
        const newHandle = newLi?.querySelector(".workout-selected__drag-handle") as HTMLElement | null;
        if (newHandle) {
          newHandle.setAttribute("aria-pressed", "true");
          newHandle.focus();
        }
        announce(`Exercise moved to position ${newIndex + 1} of ${arr.length}`);
      });
    } else if (ev.key === "Escape" && isPickedUp) {
      ev.preventDefault();
      // Restore snapshot and focus the handle at the original pickup position
      const restoredIndex = originalPickupIndex;
      arr.splice(0, arr.length, ...snapshotBeforePickup);
      originalPickupIndex = -1;
      snapshotBeforePickup = [];
      onReorder();
      requestAnimationFrame(() => {
        const restoredLi = getLiAtIndex(restoredIndex);
        const restoredHandle = restoredLi?.querySelector(".workout-selected__drag-handle") as HTMLElement | null;
        if (restoredHandle) {
          restoredHandle.setAttribute("aria-pressed", "false");
          restoredHandle.focus();
        }
        announce("Reorder cancelled. Exercise returned to original position.");
      });
    }
  });
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

  if (editSelectedExercises.length === 0) {
    errorEl.textContent = "At least one exercise is required.";
    return;
  }

  isEditSubmitting = true;
  const closeBtn = document.getElementById("workout-edit-close") as HTMLButtonElement | null;
  submitBtn.setAttribute("aria-disabled", "true");
  const originalText = submitBtn.textContent;
  submitBtn.textContent = "Saving...";
  submitBtn.classList.add("workout-form__submit--loading");
  if (closeBtn) closeBtn.disabled = true;

  try {
    const exercises = editSelectedExercises.map(exerciseId => ({
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
    if (closeBtn) closeBtn.disabled = false;
  }
}

function resetCreateForm(): void {
  const nameInput = document.getElementById("workout-name") as HTMLInputElement | null;
  const errorEl = document.getElementById("workout-error") as HTMLElement | null;
  const apiErrorEl = document.getElementById("workout-api-error") as HTMLElement | null;

  selectedExercises = [];

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

// === Pre-start Modal =========================================================

function initPreStartModal(): void {
  const backdrop = document.getElementById("workout-prestart-backdrop") as HTMLElement | null;
  if (!backdrop) return;

  const yesBtn = document.getElementById("prestart-yes") as HTMLButtonElement | null;
  const noBtn = document.getElementById("prestart-no") as HTMLButtonElement | null;
  const closeBtn = document.getElementById("prestart-close") as HTMLButtonElement | null;

  yesBtn?.addEventListener("click", () => { handleYes(); });
  noBtn?.addEventListener("click", () => { handleNo(); });
  closeBtn?.addEventListener("click", () => { closePreStartModal(); });

  backdrop.addEventListener("click", (event: Event) => {
    if (event.target === backdrop) closePreStartModal();
  });

  backdrop.addEventListener("keydown", (event: KeyboardEvent) => {
    if (event.key === "Escape") {
      closePreStartModal();
      return;
    }

    const modal = backdrop.querySelector(".prestart-modal") as HTMLElement | null;
    if (!modal) return;

    trapModalTabKey(event, modal);
  });
}

function openPreStartModal(workout: Workout, triggerBtn: HTMLButtonElement): void {
  const backdrop = document.getElementById("workout-prestart-backdrop") as HTMLElement | null;
  const yesBtn = document.getElementById("prestart-yes") as HTMLButtonElement | null;

  if (!backdrop) return;

  if (workout.exercises.length < 2) {
    navigate(`/active-session?id=${workout.plannedWorkoutId}`);
    return;
  }

  prestartWorkout = workout;
  prestartTriggerBtn = triggerBtn;

  backdrop.style.display = "";
  yesBtn?.focus();
}

function closePreStartModal(): void {
  const backdrop = document.getElementById("workout-prestart-backdrop") as HTMLElement | null;
  if (backdrop) backdrop.style.display = "none";

  const triggerBtn = prestartTriggerBtn;
  prestartWorkout = null;
  prestartTriggerBtn = null;

  triggerBtn?.focus();
}

function handleYes(): void {
  if (!prestartWorkout) return;
  const workoutId = prestartWorkout.plannedWorkoutId;
  const order = shuffle(prestartWorkout.exercises).map((ex) => ex.exerciseId).join(",");
  closePreStartModal();
  navigate(`/active-session?id=${workoutId}&order=${order}`);
}

function handleNo(): void {
  if (!prestartWorkout) return;
  const workoutId = prestartWorkout.plannedWorkoutId;
  closePreStartModal();
  navigate(`/active-session?id=${workoutId}`);
}

// =============================================================================

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

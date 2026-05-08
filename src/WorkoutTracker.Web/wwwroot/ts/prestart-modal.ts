export interface PrestartExercisePreview {
  readonly name: string;
}

export function getVisibleModalButtons(modal: HTMLElement): HTMLButtonElement[] {
  return Array.from(modal.querySelectorAll<HTMLButtonElement>("button:not([disabled])"))
    .filter((button) => {
      if (button.getClientRects().length === 0) {
        return false;
      }

      return getComputedStyle(button).display !== "none";
    });
}

export function trapModalTabKey(event: KeyboardEvent, modal: HTMLElement): void {
  if (event.key !== "Tab") {
    return;
  }

  const focusable = getVisibleModalButtons(modal);
  if (focusable.length === 0) {
    return;
  }

  const first = focusable[0];
  const last = focusable[focusable.length - 1];

  if (event.shiftKey) {
    if (document.activeElement === first) {
      event.preventDefault();
      last.focus();
    }
    return;
  }

  if (document.activeElement === last) {
    event.preventDefault();
    first.focus();
  }
}

export function renderPrestartExercisePreview(
  list: HTMLOListElement,
  exercises: readonly PrestartExercisePreview[]
): void {
  list.innerHTML = "";

  if (exercises.length === 0) {
    const li = document.createElement("li");
    li.className = "prestart-modal__exercise-empty";
    li.textContent = "No exercises configured";
    list.appendChild(li);
    return;
  }

  for (const exercise of exercises) {
    const li = document.createElement("li");
    li.className = "prestart-modal__exercise-item";
    li.textContent = exercise.name;
    list.appendChild(li);
  }
}

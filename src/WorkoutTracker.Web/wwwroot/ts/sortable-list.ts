import { reorder } from "./utils.js";

interface SortableListOptions<T> {
  readonly listId: string;
  readonly announceId: string;
  readonly getArray: () => T[];
  readonly onReorder: () => void;
}

export function initSortableList<T>(options: SortableListOptions<T>): void {
  const listElement = document.getElementById(options.listId);
  if (!listElement) return;
  const list = listElement;

  if (list.dataset["dndAttached"]) return;
  list.dataset["dndAttached"] = "1";

  let draggingIndex = -1;
  let draggingLi: HTMLElement | null = null;
  let dropHandled = false;
  let originalPickupIndex = -1;
  let snapshotBeforePickup: T[] = [];

  function announce(msg: string): void {
    const el = document.getElementById(options.announceId);
    if (el) el.textContent = msg;
  }

  function getLiAtIndex(idx: number): HTMLElement | null {
    return list.querySelector(`li[data-index="${idx}"]`);
  }

  function getLiveDomIndex(li: HTMLElement): number {
    return Array.from(list.children).indexOf(li);
  }

  list.addEventListener("dragstart", (e: Event) => {
    const ev = e as DragEvent;
    const li = (ev.target as HTMLElement).closest("li[data-index]") as HTMLElement | null;
    if (!li) return;
    draggingIndex = parseInt(li.dataset["index"] ?? "-1", 10);
    draggingLi = li;
    dropHandled = false;
    ev.dataTransfer?.setData("text/plain", String(draggingIndex));
    if (ev.dataTransfer) ev.dataTransfer.effectAllowed = "move";
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
    const rect = targetLi.getBoundingClientRect();
    if (ev.clientY < rect.top + rect.height / 2) {
      list.insertBefore(draggingLi, targetLi);
    } else {
      list.insertBefore(draggingLi, targetLi.nextSibling);
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
    const arr = options.getArray();
    if (finalIndex !== -1 && finalIndex !== draggingIndex) {
      reorder(arr, draggingIndex, finalIndex);
      announce(`Exercise moved to position ${finalIndex + 1} of ${arr.length}`);
    }
    draggingLi = null;
    draggingIndex = -1;
    options.onReorder();
  });

  list.addEventListener("dragend", () => {
    document.body.classList.remove("is-dragging");
    if (draggingLi) {
      draggingLi.classList.remove("workout-selected__item--dragging");
      draggingLi = null;
    }
    if (!dropHandled) {
      options.onReorder();
    }
    dropHandled = false;
    draggingIndex = -1;
  });

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

    touchClone.style.visibility = "hidden";
    const target = document.elementFromPoint(touch.clientX, touch.clientY)?.closest("li[data-index]") as HTMLElement | null;
    touchClone.style.visibility = "";
    const touchLi = getLiAtIndex(touchDragIndex) ?? list.querySelector(".workout-selected__item--dragging") as HTMLElement | null;
    if (target && touchLi && target !== touchLi) {
      const targetRect = target.getBoundingClientRect();
      if (touch.clientY < targetRect.top + targetRect.height / 2) {
        list.insertBefore(touchLi, target);
      } else {
        list.insertBefore(touchLi, target.nextSibling);
      }
    }
  }, { passive: false });

  list.addEventListener("touchend", () => {
    if (touchDragIndex === -1) return;

    document.body.classList.remove("is-dragging");

    if (touchClone) touchClone.style.visibility = "hidden";
    const touchLi = list.querySelector(".workout-selected__item--dragging") as HTMLElement | null;
    const finalIndex = touchLi ? getLiveDomIndex(touchLi) : -1;

    if (touchClone) {
      touchClone.remove();
      touchClone = null;
    }

    touchLi?.classList.remove("workout-selected__item--dragging");

    const arr = options.getArray();
    if (finalIndex !== -1 && finalIndex !== touchDragIndex) {
      reorder(arr, touchDragIndex, finalIndex);
      announce(`Exercise moved to position ${finalIndex + 1} of ${arr.length}`);
    }
    touchDragIndex = -1;
    options.onReorder();
  });

  list.addEventListener("keydown", (e: Event) => {
    const ev = e as KeyboardEvent;
    const handle = (ev.target as HTMLElement).closest(".workout-selected__drag-handle") as HTMLElement | null;
    if (!handle) return;

    const li = handle.closest("li[data-index]") as HTMLElement | null;
    if (!li) return;

    const currentIndex = parseInt(li.dataset["index"] ?? "-1", 10);
    const arr = options.getArray();
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
      options.onReorder();
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
      const restoredIndex = originalPickupIndex;
      arr.splice(0, arr.length, ...snapshotBeforePickup);
      originalPickupIndex = -1;
      snapshotBeforePickup = [];
      options.onReorder();
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

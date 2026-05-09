export function getVisibleModalButtons(modal: HTMLElement): HTMLButtonElement[] {
  return Array.from(modal.querySelectorAll<HTMLButtonElement>("button:not([disabled])"))
    .filter((button) => {
      let node: HTMLElement | null = button;
      while (node && node !== document.body) {
        const style = getComputedStyle(node);
        if (style.display === "none" || style.visibility === "hidden") {
          return false;
        }
        node = node.parentElement;
      }

      if (button.getClientRects().length === 0) {
        return false;
      }

      return true;
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

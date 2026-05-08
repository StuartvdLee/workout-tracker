// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';

import { getVisibleModalButtons, renderPrestartExercisePreview, trapModalTabKey } from '../prestart-modal';

function mockRects(element: HTMLElement, hasRects: boolean): void {
  Object.defineProperty(element, 'getClientRects', {
    configurable: true,
    value: () => (hasRects ? [{ width: 100 }] : []),
  });
}

describe('getVisibleModalButtons', () => {
  it('excludes controls hidden by parent rows', () => {
    document.body.innerHTML = `
      <div class="prestart-modal">
        <div id="visible-row"><button id="visible-btn" type="button">Visible</button></div>
        <div id="hidden-row" style="display:none;"><button id="hidden-btn" type="button">Hidden</button></div>
      </div>
    `;

    const modal = document.querySelector('.prestart-modal') as HTMLElement;
    const visibleBtn = document.getElementById('visible-btn') as HTMLButtonElement;
    const hiddenBtn = document.getElementById('hidden-btn') as HTMLButtonElement;
    mockRects(visibleBtn, true);
    mockRects(hiddenBtn, true);

    expect(getVisibleModalButtons(modal).map((button) => button.id)).toEqual(['visible-btn']);
  });
});

describe('trapModalTabKey', () => {
  it('loops focus from last button to first on Tab', () => {
    document.body.innerHTML = `
      <div class="prestart-modal">
        <button id="first" type="button">First</button>
        <button id="last" type="button">Last</button>
      </div>
    `;

    const modal = document.querySelector('.prestart-modal') as HTMLElement;
    const first = document.getElementById('first') as HTMLButtonElement;
    const last = document.getElementById('last') as HTMLButtonElement;
    mockRects(first, true);
    mockRects(last, true);

    last.focus();
    const event = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true });
    trapModalTabKey(event, modal);

    expect(document.activeElement).toBe(first);
    expect(event.defaultPrevented).toBe(true);
  });

  it('loops focus from first button to last on Shift+Tab', () => {
    document.body.innerHTML = `
      <div class="prestart-modal">
        <button id="first" type="button">First</button>
        <button id="last" type="button">Last</button>
      </div>
    `;

    const modal = document.querySelector('.prestart-modal') as HTMLElement;
    const first = document.getElementById('first') as HTMLButtonElement;
    const last = document.getElementById('last') as HTMLButtonElement;
    mockRects(first, true);
    mockRects(last, true);

    first.focus();
    const event = new KeyboardEvent('keydown', { key: 'Tab', shiftKey: true, bubbles: true, cancelable: true });
    trapModalTabKey(event, modal);

    expect(document.activeElement).toBe(last);
    expect(event.defaultPrevented).toBe(true);
  });

  it('ignores non-Tab keys', () => {
    document.body.innerHTML = `
      <div class="prestart-modal">
        <button id="first" type="button">First</button>
        <button id="last" type="button">Last</button>
      </div>
    `;

    const modal = document.querySelector('.prestart-modal') as HTMLElement;
    const first = document.getElementById('first') as HTMLButtonElement;
    const last = document.getElementById('last') as HTMLButtonElement;
    mockRects(first, true);
    mockRects(last, true);

    first.focus();
    const event = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true });
    trapModalTabKey(event, modal);

    expect(document.activeElement).toBe(first);
    expect(event.defaultPrevented).toBe(false);
  });

  it('does nothing when no focusable buttons are visible', () => {
    document.body.innerHTML = `
      <div class="prestart-modal">
        <button id="hidden" type="button">Hidden</button>
      </div>
    `;

    const modal = document.querySelector('.prestart-modal') as HTMLElement;
    const hidden = document.getElementById('hidden') as HTMLButtonElement;
    mockRects(hidden, false);

    const event = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true });
    trapModalTabKey(event, modal);

    expect(event.defaultPrevented).toBe(false);
  });
});

describe('renderPrestartExercisePreview', () => {
  it('renders empty-state content when there are no exercises', () => {
    const list = document.createElement('ol');
    renderPrestartExercisePreview(list, []);

    expect(list.querySelector('.prestart-modal__exercise-empty')?.textContent).toBe('No exercises configured');
  });
});

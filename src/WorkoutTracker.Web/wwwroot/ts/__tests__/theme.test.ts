// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import {
  getStoredPreference,
  getSystemPreference,
  resolveTheme,
  applyTheme,
  initTheme,
  type ThemePreference,
} from '../theme.js';

// ---------------------------------------------------------------------------
// localStorage mock (jsdom's impl lacks .clear() in this env)
// ---------------------------------------------------------------------------
const _store: Record<string, string> = {};
const lsMock = {
  getItem:    (key: string)            => _store[key] ?? null,
  setItem:    (key: string, val: string) => { _store[key] = val; },
  removeItem: (key: string)            => { delete _store[key]; },
  clear:      ()                       => { for (const k in _store) delete _store[k]; },
  length: 0,
  key: (_i: number) => null,
};
vi.stubGlobal('localStorage', lsMock);
const clearLs = () => { for (const k in _store) delete _store[k]; };

// ---------------------------------------------------------------------------
// matchMedia mock helper
// ---------------------------------------------------------------------------
function mockMatchMedia(prefersDark: boolean) {
  const mql = {
    matches: prefersDark,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
  };
  vi.stubGlobal('matchMedia', vi.fn().mockReturnValue(mql));
  return mql;
}

// ---------------------------------------------------------------------------
// DOM builder
// ---------------------------------------------------------------------------
function buildDOM() {
  document.documentElement.removeAttribute('data-theme');
  document.documentElement.removeAttribute('data-theme-pref');
  document.body.innerHTML = `
    <button id="theme-btn" aria-label="Theme: System"></button>
    <ul id="theme-menu" hidden>
      <li><button class="theme-menu__item" data-theme-value="light">Light</button></li>
      <li><button class="theme-menu__item" data-theme-value="dark">Dark</button></li>
      <li><button class="theme-menu__item" data-theme-value="system">System</button></li>
    </ul>
  `;
}

// ---------------------------------------------------------------------------
// T013 — getStoredPreference()
// ---------------------------------------------------------------------------

describe('getStoredPreference()', () => {
  beforeEach(() => clearLs());

  it('returns "system" when key is absent', () => {
    expect(getStoredPreference()).toBe('system');
  });

  it('returns "system" when stored value is unrecognised', () => {
    localStorage.setItem('workout-tracker-theme', 'invalid');
    expect(getStoredPreference()).toBe('system');
  });

  it('returns "light" when "light" is stored', () => {
    localStorage.setItem('workout-tracker-theme', 'light');
    expect(getStoredPreference()).toBe('light');
  });

  it('returns "dark" when "dark" is stored', () => {
    localStorage.setItem('workout-tracker-theme', 'dark');
    expect(getStoredPreference()).toBe('dark');
  });

  it('returns "system" when "system" is stored', () => {
    localStorage.setItem('workout-tracker-theme', 'system');
    expect(getStoredPreference()).toBe('system');
  });
});

// ---------------------------------------------------------------------------
// getSystemPreference() (U1/U2 coverage)
// ---------------------------------------------------------------------------

describe('getSystemPreference()', () => {
  it('returns "dark" when matchMedia matches dark', () => {
    mockMatchMedia(true);
    expect(getSystemPreference()).toBe('dark');
  });

  it('returns "light" when matchMedia does not match dark', () => {
    mockMatchMedia(false);
    expect(getSystemPreference()).toBe('light');
  });

  it('returns "light" when matchMedia throws', () => {
    vi.stubGlobal('matchMedia', vi.fn().mockImplementation(() => { throw new Error('unsupported'); }));
    expect(getSystemPreference()).toBe('light');
  });
});

// ---------------------------------------------------------------------------
// T014 — resolveTheme()
// ---------------------------------------------------------------------------

describe('resolveTheme()', () => {
  it('"light" resolves to "light"', () => {
    mockMatchMedia(false);
    expect(resolveTheme('light')).toBe('light');
  });

  it('"dark" resolves to "dark"', () => {
    mockMatchMedia(false);
    expect(resolveTheme('dark')).toBe('dark');
  });

  it('"system" resolves to "dark" when OS is dark', () => {
    mockMatchMedia(true);
    expect(resolveTheme('system')).toBe('dark');
  });

  it('"system" resolves to "light" when OS is light', () => {
    mockMatchMedia(false);
    expect(resolveTheme('system')).toBe('light');
  });
});

// ---------------------------------------------------------------------------
// T015 — applyTheme()
// ---------------------------------------------------------------------------

describe('applyTheme()', () => {
  beforeEach(() => {
    buildDOM();
  });

  it('sets data-theme="light" and data-theme-pref="light" for "light"', () => {
    mockMatchMedia(false);
    applyTheme('light');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
    expect(document.documentElement.getAttribute('data-theme-pref')).toBe('light');
  });

  it('sets data-theme="dark" and data-theme-pref="dark" for "dark"', () => {
    mockMatchMedia(false);
    applyTheme('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme-pref')).toBe('dark');
  });

  it('sets data-theme="dark" and data-theme-pref="system" for "system" with OS dark', () => {
    mockMatchMedia(true);
    applyTheme('system');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme-pref')).toBe('system');
  });

  it('sets data-theme="light" and data-theme-pref="system" for "system" with OS light', () => {
    mockMatchMedia(false);
    applyTheme('system');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
    expect(document.documentElement.getAttribute('data-theme-pref')).toBe('system');
  });

  it('updates aria-label on #theme-btn to "Theme: Light"', () => {
    mockMatchMedia(false);
    applyTheme('light');
    expect(document.getElementById('theme-btn')?.getAttribute('aria-label')).toBe('Theme: Light');
  });

  it('updates aria-label on #theme-btn to "Theme: Dark"', () => {
    mockMatchMedia(false);
    applyTheme('dark');
    expect(document.getElementById('theme-btn')?.getAttribute('aria-label')).toBe('Theme: Dark');
  });

  it('updates aria-label on #theme-btn to "Theme: System"', () => {
    mockMatchMedia(false);
    applyTheme('system');
    expect(document.getElementById('theme-btn')?.getAttribute('aria-label')).toBe('Theme: System');
  });

  it('marks the matching menu item active and others inactive', () => {
    mockMatchMedia(false);
    applyTheme('dark');
    const items = document.querySelectorAll<HTMLElement>('.theme-menu__item');
    expect(items[0].classList.contains('theme-menu__item--active')).toBe(false); // light
    expect(items[1].classList.contains('theme-menu__item--active')).toBe(true);  // dark
    expect(items[2].classList.contains('theme-menu__item--active')).toBe(false); // system
  });
});

// ---------------------------------------------------------------------------
// T018 — persistence: localStorage writes from menu item clicks
// ---------------------------------------------------------------------------

describe('persistence via initTheme()', () => {
  beforeEach(() => {
    clearLs();
    buildDOM();
    mockMatchMedia(false);
    initTheme();
  });

  it('clicking Light menu item writes "light" to localStorage', () => {
    document.querySelector<HTMLElement>('[data-theme-value="light"]')!.click();
    expect(lsMock.getItem('workout-tracker-theme')).toBe('light');
  });

  it('clicking Dark menu item writes "dark" to localStorage', () => {
    document.querySelector<HTMLElement>('[data-theme-value="dark"]')!.click();
    expect(lsMock.getItem('workout-tracker-theme')).toBe('dark');
  });

  it('clicking System menu item writes "system" to localStorage', () => {
    document.querySelector<HTMLElement>('[data-theme-value="system"]')!.click();
    expect(lsMock.getItem('workout-tracker-theme')).toBe('system');
  });

  it('first load with no stored key defaults to "system"', () => {
    // localStorage was cleared in beforeEach; initTheme() already ran
    // getStoredPreference() should return 'system'
    expect(getStoredPreference()).toBe('system');
  });
});

// ---------------------------------------------------------------------------
// T021 — real-time OS tracking via initTheme() change listener
// ---------------------------------------------------------------------------

describe('real-time OS tracking (matchMedia change event)', () => {
  let mql: ReturnType<typeof mockMatchMedia>;

  beforeEach(() => {
    clearLs();
    buildDOM();
    mql = mockMatchMedia(false);
    initTheme();
  });

  function fireChangeEvent() {
    const handler = mql.addEventListener.mock.calls.find(([event]) => event === 'change')?.[1] as ((e?: Event) => void) | undefined;
    expect(handler, 'initTheme() must register a "change" listener on the media query').toBeDefined();
    handler!();
  }

  it('registers a "change" listener on the system media query', () => {
    const changeCall = mql.addEventListener.mock.calls.find(([event]) => event === 'change');
    expect(changeCall).toBeDefined();
  });

  it('updates to dark when stored pref is "system" and OS changes to dark', () => {
    lsMock.setItem('workout-tracker-theme', 'system');
    mql.matches = true;
    fireChangeEvent();
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });

  it('updates to light when stored pref is "system" and OS changes to light', () => {
    lsMock.setItem('workout-tracker-theme', 'system');
    mql.matches = false;
    fireChangeEvent();
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
  });

  it('does NOT update theme when stored pref is "dark" and OS changes', () => {
    lsMock.setItem('workout-tracker-theme', 'dark');
    applyTheme('dark');
    mql.matches = false; // OS changes to light
    fireChangeEvent();
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });

  it('does NOT update theme when stored pref is "light" and OS changes', () => {
    lsMock.setItem('workout-tracker-theme', 'light');
    applyTheme('light');
    mql.matches = true; // OS changes to dark
    fireChangeEvent();
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
  });
});


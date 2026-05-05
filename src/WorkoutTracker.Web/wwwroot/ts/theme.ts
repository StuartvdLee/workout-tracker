export type ThemePreference = 'light' | 'dark' | 'system';
export type ResolvedTheme = 'light' | 'dark';

const STORAGE_KEY = 'workout-tracker-theme';
const VALID_PREFS: ThemePreference[] = ['light', 'dark', 'system'];

export function getStoredPreference(): ThemePreference {
  const stored = localStorage.getItem(STORAGE_KEY);
  return (VALID_PREFS as string[]).includes(stored ?? '') ? (stored as ThemePreference) : 'system';
}

export function getSystemPreference(): ResolvedTheme {
  try {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  } catch {
    return 'light';
  }
}

export function resolveTheme(pref: ThemePreference): ResolvedTheme {
  if (pref === 'light' || pref === 'dark') return pref;
  return getSystemPreference();
}

export function applyTheme(pref: ThemePreference): void {
  const resolved = resolveTheme(pref);
  document.documentElement.setAttribute('data-theme', resolved);
  document.documentElement.setAttribute('data-theme-pref', pref);

  const btn = document.getElementById('theme-btn');
  if (btn) {
    const label = pref === 'system' ? 'Theme: System' : pref === 'dark' ? 'Theme: Dark' : 'Theme: Light';
    btn.setAttribute('aria-label', label);
  }

  document.querySelectorAll<HTMLElement>('.theme-menu__item').forEach((item) => {
    const isActive = item.dataset.themeValue === pref;
    item.classList.toggle('theme-menu__item--active', isActive);
  });
}

export function initTheme(): void {
  applyTheme(getStoredPreference());

  try {
    const systemMediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    systemMediaQuery.addEventListener('change', () => {
      if (getStoredPreference() === 'system') {
        applyTheme('system');
      }
    });
  } catch {
    // OS preference tracking unavailable; skip live updates
  }

  const btn = document.getElementById('theme-btn');
  const menu = document.getElementById('theme-menu');
  if (!btn || !menu) return;

  const openMenu = () => {
    menu.removeAttribute('hidden');
    btn.setAttribute('aria-expanded', 'true');
    const firstItem = menu.querySelector<HTMLElement>('.theme-menu__item');
    firstItem?.focus();
  };

  const closeMenu = (returnFocus = true) => {
    menu.setAttribute('hidden', '');
    btn.setAttribute('aria-expanded', 'false');
    if (returnFocus) btn.focus();
  };

  const isOpen = () => !menu.hasAttribute('hidden');

  btn.addEventListener('click', () => {
    isOpen() ? closeMenu() : openMenu();
  });

  menu.querySelectorAll<HTMLElement>('.theme-menu__item').forEach((item) => {
    item.addEventListener('click', () => {
      const value = item.dataset.themeValue as ThemePreference | undefined;
      if (value && VALID_PREFS.includes(value)) {
        localStorage.setItem(STORAGE_KEY, value);
        applyTheme(value);
      }
      closeMenu();
    });
  });

  document.addEventListener('click', (e) => {
    if (isOpen() && !btn.contains(e.target as Node) && !menu.contains(e.target as Node)) {
      closeMenu(false);
    }
  });

  document.addEventListener('keydown', (e) => {
    if (!isOpen()) return;
    if (e.key === 'Escape') {
      e.preventDefault();
      closeMenu();
    } else if (e.key === 'Tab') {
      closeMenu(false);
    }
  });

  menu.addEventListener('keydown', (e) => {
    if (e.key !== 'ArrowDown' && e.key !== 'ArrowUp') return;
    e.preventDefault();
    const items = Array.from(menu.querySelectorAll<HTMLElement>('.theme-menu__item'));
    const current = document.activeElement as HTMLElement;
    const idx = items.indexOf(current);
    if (e.key === 'ArrowDown') {
      items[(idx + 1) % items.length]?.focus();
    } else {
      items[(idx - 1 + items.length) % items.length]?.focus();
    }
  });
}

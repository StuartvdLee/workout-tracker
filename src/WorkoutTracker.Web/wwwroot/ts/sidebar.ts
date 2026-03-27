import { navigate, onNavigate, getCurrentPath } from "./router.js";

export function initSidebar(): void {
  const links = document.querySelectorAll<HTMLAnchorElement>(".sidebar__link");

  for (const link of links) {
    link.addEventListener("click", (event: Event) => {
      event.preventDefault();
      const page = link.dataset.page;
      if (!page) {
        return;
      }
      const path = page === "home" ? "/" : `/${page}`;
      navigate(path);
      closeMobileSidebar();
    });
  }

  onNavigate((path: string) => updateActiveLink(path));
  initMobileToggle();
  updateActiveLink(getCurrentPath());
}

function updateActiveLink(path: string): void {
  const links = document.querySelectorAll<HTMLAnchorElement>(".sidebar__link");
  const activePage = path === "/" ? "home" : path.replace(/^\//, "");

  for (const link of links) {
    if (link.dataset.page === activePage) {
      link.classList.add("sidebar__link--active");
      link.setAttribute("aria-current", "page");
    } else {
      link.classList.remove("sidebar__link--active");
      link.removeAttribute("aria-current");
    }
  }
}

function initMobileToggle(): void {
  const toggle = document.querySelector<HTMLButtonElement>(".topbar__toggle");
  const sidebar = document.getElementById("sidebar");
  const backdrop = document.getElementById("sidebar-backdrop");

  if (!toggle || !sidebar || !backdrop) {
    return;
  }

  toggle.addEventListener("click", () => {
    const isOpen = sidebar.classList.contains("sidebar--open");
    if (isOpen) {
      closeMobileSidebar();
    } else {
      openMobileSidebar();
    }
  });

  backdrop.addEventListener("click", () => closeMobileSidebar());

  document.addEventListener("keydown", (event: KeyboardEvent) => {
    if (event.key === "Escape" && sidebar.classList.contains("sidebar--open")) {
      closeMobileSidebar();
    }
  });

  const mediaQuery = window.matchMedia("(min-width: 768px)");
  mediaQuery.addEventListener("change", (e: MediaQueryListEvent) => {
    if (e.matches) {
      closeMobileSidebar();
    }
  });
}

function openMobileSidebar(): void {
  const sidebar = document.getElementById("sidebar");
  const backdrop = document.getElementById("sidebar-backdrop");
  const toggle = document.querySelector<HTMLButtonElement>(".topbar__toggle");

  sidebar?.classList.add("sidebar--open");
  backdrop?.classList.add("sidebar__backdrop--visible");
  toggle?.setAttribute("aria-expanded", "true");
}

function closeMobileSidebar(): void {
  const sidebar = document.getElementById("sidebar");
  const backdrop = document.getElementById("sidebar-backdrop");
  const toggle = document.querySelector<HTMLButtonElement>(".topbar__toggle");

  sidebar?.classList.remove("sidebar--open");
  backdrop?.classList.remove("sidebar__backdrop--visible");
  toggle?.setAttribute("aria-expanded", "false");
}

type RenderFn = (container: HTMLElement) => void | Promise<void>;

interface Route {
  readonly path: string;
  readonly render: RenderFn;
}

const routes: Route[] = [];
let contentEl: HTMLElement | null = null;
let onNavigateCallback: ((path: string) => void) | null = null;

export function registerRoute(path: string, render: RenderFn): void {
  routes.push({ path, render });
}

export function onNavigate(callback: (path: string) => void): void {
  onNavigateCallback = callback;
}

export function navigate(path: string): void {
  const normalised = normalisePath(path);
  if (normalised === getCurrentPath()) {
    return;
  }
  history.pushState(null, "", normalised);
  renderCurrentRoute();
}

export function getCurrentPath(): string {
  return normalisePath(window.location.pathname);
}

export function init(): void {
  contentEl = document.getElementById("content");
  window.addEventListener("popstate", () => renderCurrentRoute());
  renderCurrentRoute();
}

function renderCurrentRoute(): void {
  if (!contentEl) {
    return;
  }

  const path = getCurrentPath();
  const route = routes.find((r) => r.path === path);

  if (!route) {
    history.replaceState(null, "", "/");
    const homeRoute = routes.find((r) => r.path === "/");
    if (homeRoute) {
      contentEl.innerHTML = "";
      homeRoute.render(contentEl);
    }
    onNavigateCallback?.("/");
    return;
  }

  contentEl.innerHTML = "";
  route.render(contentEl);
  onNavigateCallback?.(path);
}

function normalisePath(path: string): string {
  if (path === "/" || path === "") {
    return "/";
  }
  return path.replace(/\/+$/, "") || "/";
}

type RenderFn = (container: HTMLElement) => void | Promise<void>;

interface Route {
  readonly path: string;
  readonly render: RenderFn;
}

const routes: Route[] = [];
let contentEl: HTMLElement | null = null;
let onNavigateCallback: ((path: string) => void) | null = null;
let navigationToken = 0;

export function registerRoute(path: string, render: RenderFn): void {
  routes.push({ path, render });
}

export function onNavigate(callback: (path: string) => void): void {
  onNavigateCallback = callback;
}

export function navigate(path: string): void {
  const [pathname, search] = path.split("?");
  const normalised = normalisePath(pathname);
  const fullPath = search ? `${normalised}?${search}` : normalised;
  if (normalised === getCurrentPath() && window.location.search === (search ? `?${search}` : "")) {
    return;
  }
  history.pushState(null, "", fullPath);
  void renderCurrentRoute();
}

export function getCurrentPath(): string {
  return normalisePath(window.location.pathname);
}

export function init(): void {
  contentEl = document.getElementById("content");
  window.addEventListener("popstate", () => void renderCurrentRoute());
  void renderCurrentRoute();
}

async function renderCurrentRoute(): Promise<void> {
  if (!contentEl) {
    return;
  }

  const token = ++navigationToken;
  const path = getCurrentPath();
  const route = routes.find((r) => r.path === path);

  if (!route) {
    history.replaceState(null, "", "/");
    const homeRoute = routes.find((r) => r.path === "/");
    if (homeRoute) {
      contentEl.innerHTML = "";
      await homeRoute.render(contentEl);
    }
    if (token === navigationToken) {
      onNavigateCallback?.("/");
    }
    return;
  }

  contentEl.innerHTML = "";
  await route.render(contentEl);
  if (token === navigationToken) {
    onNavigateCallback?.(path);
  }
}

export function normalisePath(path: string): string {
  if (path === "/" || path === "") {
    return "/";
  }
  return path.replace(/\/+$/, "") || "/";
}

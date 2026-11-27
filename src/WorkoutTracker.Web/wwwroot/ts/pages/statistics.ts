import { onRouteCleanup } from "../router.js";
import {
  createReportingPeriod,
  countPeriodsSince,
  formatPeriodLabel,
  getAdjacentPeriod,
  isCurrentPeriod,
  type ReportingPeriod,
} from "./statistics-period.js";

interface WorkoutStatisticsResponse {
  readonly workoutCount: number;
  readonly history: {
    readonly workoutCount: number;
    readonly firstCompletedAt: string;
  } | null;
}

export async function render(container: HTMLElement): Promise<void> {
  let period: ReportingPeriod = createReportingPeriod("week");
  let requestGeneration = 0;

  container.innerHTML = `
    <div class="statistics-page">
      <h1 class="statistics-page__title">Workout Statistics</h1>
      <section class="statistics-controls" aria-label="Reporting period">
        <div class="statistics-controls__kinds" role="group" aria-label="Period type">
          <button type="button" class="statistics-controls__kind" id="statistics-kind-week" data-kind="week" aria-pressed="true">Week</button>
          <button type="button" class="statistics-controls__kind" id="statistics-kind-month" data-kind="month" aria-pressed="false">Month</button>
          <button type="button" class="statistics-controls__kind" id="statistics-kind-year" data-kind="year" aria-pressed="false">Year</button>
        </div>
        <div class="statistics-controls__navigation">
          <button type="button" class="statistics-controls__nav" id="statistics-previous">Previous</button>
          <button type="button" class="statistics-controls__nav" id="statistics-current">Current</button>
          <button type="button" class="statistics-controls__nav" id="statistics-next">Next</button>
        </div>
        <h2 class="statistics-page__period" id="statistics-period-label" aria-live="polite"></h2>
      </section>
      <p class="statistics-page__status" id="statistics-status" role="status" aria-live="polite">Loading statistics...</p>
      <div class="statistics-page__error" id="statistics-error" role="alert" hidden>
        <p>Statistics could not be loaded.</p>
        <button type="button" class="statistics-controls__nav" id="statistics-retry">Retry</button>
      </div>
      <div class="statistics-results" id="statistics-results" hidden>
        <section class="statistics-card" aria-labelledby="statistics-workout-heading">
          <h3 id="statistics-workout-heading">Completed workouts</h3>
          <p class="statistics-card__value" id="statistics-workout-count"></p>
          <p id="statistics-empty-message" class="statistics-card__note" hidden>
            No workouts were completed during this period.
          </p>
        </section>
        <section class="statistics-card" aria-labelledby="statistics-average-heading">
          <h3 id="statistics-average-heading"></h3>
          <p class="statistics-card__value" id="statistics-average-value"></p>
          <p class="statistics-card__note" id="statistics-average-note"></p>
        </section>
      </div>
    </div>
  `;

  const periodLabel = container.querySelector<HTMLElement>("#statistics-period-label");
  const status = container.querySelector<HTMLElement>("#statistics-status");
  const error = container.querySelector<HTMLElement>("#statistics-error");
  const results = container.querySelector<HTMLElement>("#statistics-results");
  const previous = container.querySelector<HTMLButtonElement>("#statistics-previous");
  const current = container.querySelector<HTMLButtonElement>("#statistics-current");
  const next = container.querySelector<HTMLButtonElement>("#statistics-next");
  const retry = container.querySelector<HTMLButtonElement>("#statistics-retry");

  if (!periodLabel || !status || !error || !results || !previous || !current || !next || !retry) {
    throw new Error("Statistics page controls are missing.");
  }

  const updateControls = (): void => {
    periodLabel.textContent = formatPeriodLabel(period);
    previous.disabled = false;
    current.disabled = isCurrentPeriod(period);
    next.disabled = current.disabled;
    container.querySelectorAll<HTMLButtonElement>("[data-kind]").forEach((button) => {
      const selected = button.dataset.kind === period.kind;
      button.setAttribute("aria-pressed", String(selected));
    });
  };

  const loadStatistics = async (): Promise<void> => {
    const requestId = ++requestGeneration;
    const requestedPeriod = period;
    updateControls();
    results.hidden = true;
    error.hidden = true;
    retry.disabled = false;
    status.classList.remove("sr-only");
    status.textContent = "Loading statistics...";

    try {
      const query = new URLSearchParams({
        startUtc: requestedPeriod.startUtc,
        endUtc: requestedPeriod.endUtc,
      });
      const response = await fetch(`/api/statistics/workouts?${query}`);
      if (!response.ok) {
        throw new Error(`Statistics request failed with status ${response.status}`);
      }

      const data: WorkoutStatisticsResponse = await response.json();
      if (requestId !== requestGeneration) {
        return;
      }

      renderStatistics(data, container, requestedPeriod);
      status.classList.add("sr-only");
      status.textContent = `Statistics loaded for ${formatPeriodLabel(requestedPeriod)}.`;
      results.hidden = false;
    } catch {
      if (requestId !== requestGeneration) {
        return;
      }
      status.textContent = "";
      status.classList.add("sr-only");
      error.hidden = false;
    }
  };

  previous.addEventListener("click", () => {
    period = getAdjacentPeriod(period, -1);
    void loadStatistics();
  });
  current.addEventListener("click", () => {
    period = createReportingPeriod(period.kind);
    void loadStatistics();
  });
  next.addEventListener("click", () => {
    if (!isCurrentPeriod(period)) {
      period = getAdjacentPeriod(period, 1);
      void loadStatistics();
    }
  });
  retry.addEventListener("click", () => void loadStatistics());
  container.querySelectorAll<HTMLButtonElement>("[data-kind]").forEach((button) => {
    button.addEventListener("click", () => {
      const kind = button.dataset.kind;
      if (kind !== "week" && kind !== "month" && kind !== "year") {
        return;
      }
      period = createReportingPeriod(kind);
      void loadStatistics();
    });
  });

  onRouteCleanup(() => {
    requestGeneration += 1;
  });
  await loadStatistics();
}

function renderStatistics(
  data: WorkoutStatisticsResponse,
  container: HTMLElement,
  period: ReportingPeriod,
): void {
  const workoutCount = container.querySelector<HTMLElement>("#statistics-workout-count");
  const emptyMessage = container.querySelector<HTMLElement>("#statistics-empty-message");
  const averageHeading = container.querySelector<HTMLElement>("#statistics-average-heading");
  const averageValue = container.querySelector<HTMLElement>("#statistics-average-value");
  const averageNote = container.querySelector<HTMLElement>("#statistics-average-note");

  if (!workoutCount || !emptyMessage || !averageHeading || !averageValue || !averageNote) {
    throw new Error("Statistics results are missing.");
  }

  workoutCount.textContent = String(data.workoutCount);
  emptyMessage.hidden = data.workoutCount !== 0;
  averageHeading.textContent = `Average workouts per ${period.kind}`;
  if (data.history === null) {
    averageValue.textContent = "—";
    averageNote.textContent = "No workouts recorded through this period.";
  } else {
    const periodCount = countPeriodsSince(period, data.history.firstCompletedAt);
    averageValue.textContent = new Intl.NumberFormat("en-GB", { maximumFractionDigits: 1 })
      .format(data.history.workoutCount / periodCount);
    averageNote.textContent = `From your first recorded workout through this ${period.kind}, including ${period.kind}s with no workouts.`;
  }
}

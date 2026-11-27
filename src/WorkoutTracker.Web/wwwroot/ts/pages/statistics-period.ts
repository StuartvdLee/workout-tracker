export type PeriodKind = "week" | "month" | "year";

export interface ReportingPeriod {
  readonly kind: PeriodKind;
  readonly localStart: Date;
  readonly localEndExclusive: Date;
  readonly startUtc: string;
  readonly endUtc: string;
}

export function createReportingPeriod(
  kind: PeriodKind,
  referenceDate: Date = new Date(),
): ReportingPeriod {
  let localStart: Date;
  let localEndExclusive: Date;

  if (kind === "week") {
    localStart = new Date(
      referenceDate.getFullYear(),
      referenceDate.getMonth(),
      referenceDate.getDate(),
    );
    localStart.setDate(localStart.getDate() - ((localStart.getDay() + 6) % 7));
    localEndExclusive = new Date(localStart);
    localEndExclusive.setDate(localEndExclusive.getDate() + 7);
  } else if (kind === "month") {
    localStart = new Date(
      referenceDate.getFullYear(),
      referenceDate.getMonth(),
      1,
    );
    localEndExclusive = new Date(
      localStart.getFullYear(),
      localStart.getMonth() + 1,
      1,
    );
  } else {
    localStart = new Date(referenceDate.getFullYear(), 0, 1);
    localEndExclusive = new Date(referenceDate.getFullYear() + 1, 0, 1);
  }

  return {
    kind,
    localStart,
    localEndExclusive,
    startUtc: localStart.toISOString(),
    endUtc: localEndExclusive.toISOString(),
  };
}

export function getAdjacentPeriod(
  period: ReportingPeriod,
  offset: -1 | 1,
): ReportingPeriod {
  const referenceDate = new Date(period.localStart);
  if (period.kind === "week") {
    referenceDate.setDate(referenceDate.getDate() + offset * 7);
  } else if (period.kind === "month") {
    referenceDate.setMonth(referenceDate.getMonth() + offset);
  } else {
    referenceDate.setFullYear(referenceDate.getFullYear() + offset);
  }
  return createReportingPeriod(period.kind, referenceDate);
}

export function isCurrentPeriod(
  period: ReportingPeriod,
  now: Date = new Date(),
): boolean {
  return (
    period.localStart.getTime() >=
    createReportingPeriod(period.kind, now).localStart.getTime()
  );
}

export function formatPeriodLabel(period: ReportingPeriod): string {
  if (period.kind === "month") {
    return new Intl.DateTimeFormat("en-GB", { month: "long" }).format(period.localStart);
  }
  if (period.kind === "year") {
    return String(period.localStart.getFullYear());
  }

  const inclusiveEnd = new Date(period.localEndExclusive);
  inclusiveEnd.setDate(inclusiveEnd.getDate() - 1);
  return `${formatLocalDate(period.localStart)} – ${formatLocalDate(inclusiveEnd)}`;
}

export function countPeriodsSince(period: ReportingPeriod, firstCompletedAt: string): number {
  const first = createReportingPeriod(period.kind, new Date(firstCompletedAt)).localStart;
  const last = period.localStart;
  if (period.kind === "year") {
    return last.getFullYear() - first.getFullYear() + 1;
  }
  if (period.kind === "month") {
    return (last.getFullYear() - first.getFullYear()) * 12 + last.getMonth() - first.getMonth() + 1;
  }

  const calendarDay = (date: Date): number =>
    Date.UTC(date.getFullYear(), date.getMonth(), date.getDate());
  return Math.round((calendarDay(last) - calendarDay(first)) / (7 * 24 * 60 * 60 * 1000)) + 1;
}

function formatLocalDate(date: Date): string {
  return new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(date);
}

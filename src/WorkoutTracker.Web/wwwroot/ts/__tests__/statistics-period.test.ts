import { afterEach, describe, expect, it } from "vitest";
import {
  createReportingPeriod,
  countPeriodsSince,
  formatPeriodLabel,
  getAdjacentPeriod,
  isCurrentPeriod,
} from "../pages/statistics-period";

const originalTimeZone = process.env.TZ;

afterEach(() => {
  if (originalTimeZone === undefined) {
    delete process.env.TZ;
  } else {
    process.env.TZ = originalTimeZone;
  }
});

describe("createReportingPeriod", () => {
  it("uses Monday as the start of a week, including when the reference is Sunday", () => {
    const period = createReportingPeriod("week", new Date(2026, 0, 4, 14));

    expect(period.localStart.getDay()).toBe(1);
    expect(period.localStart.getDate()).toBe(29);
    expect(period.localStart.getMonth()).toBe(11);
    expect(period.localEndExclusive.getDate()).toBe(5);
    expect(period.localEndExclusive.getMonth()).toBe(0);
  });

  it("creates calendar month and year boundaries", () => {
    const month = createReportingPeriod("month", new Date(2026, 1, 17));
    const year = createReportingPeriod("year", new Date(2026, 1, 17));

    expect(month.localStart.getDate()).toBe(1);
    expect(month.localStart.getMonth()).toBe(1);
    expect(month.localEndExclusive.getMonth()).toBe(2);
    expect(month.localEndExclusive.getDate()).toBe(1);
    expect(year.localStart.getMonth()).toBe(0);
    expect(year.localStart.getDate()).toBe(1);
    expect(year.localEndExclusive.getFullYear()).toBe(2027);
    expect(year.localEndExclusive.getMonth()).toBe(0);
  });

  it("converts local boundaries independently across daylight-saving changes", () => {
    process.env.TZ = "America/New_York";
    const spring = createReportingPeriod("week", new Date(2026, 2, 4));
    const autumn = createReportingPeriod("week", new Date(2026, 9, 28));

    expect(spring.startUtc).toBe(new Date(2026, 2, 2).toISOString());
    expect(spring.endUtc).toBe(new Date(2026, 2, 9).toISOString());
    expect(
      new Date(spring.endUtc).getTime() - new Date(spring.startUtc).getTime(),
    ).toBe(167 * 60 * 60 * 1000);
    expect(
      new Date(autumn.endUtc).getTime() - new Date(autumn.startUtc).getTime(),
    ).toBe(169 * 60 * 60 * 1000);
  });

  it("formats an inclusive local date range", () => {
    const period = createReportingPeriod("week", new Date(2026, 0, 1));

    expect(formatPeriodLabel(period)).toBe("29 Dec 2025 – 4 Jan 2026");
  });

  it("formats month and year periods without exact dates", () => {
    const date = new Date(2026, 1, 17);

    expect(formatPeriodLabel(createReportingPeriod("month", date))).toBe("February");
    expect(formatPeriodLabel(createReportingPeriod("year", date))).toBe("2026");
    expect(formatPeriodLabel(getAdjacentPeriod(createReportingPeriod("year", date), -1))).toBe("2025");
  });
});

describe("getAdjacentPeriod", () => {
  it("moves by calendar month without overflowing at month boundaries", () => {
    const january = createReportingPeriod("month", new Date(2026, 0, 31));
    const previous = getAdjacentPeriod(january, -1);
    const next = getAdjacentPeriod(previous, 1);

    expect(previous.localStart.getMonth()).toBe(11);
    expect(previous.localStart.getFullYear()).toBe(2025);
    expect(next.localStart.getMonth()).toBe(0);
    expect(next.localStart.getFullYear()).toBe(2026);
  });

  describe("countPeriodsSince", () => {
    it("counts empty calendar months and years from the first workout", () => {
      const first = new Date(2024, 10, 30, 12).toISOString();
      expect(countPeriodsSince(createReportingPeriod("month", new Date(2026, 1, 1)), first)).toBe(16);
      expect(countPeriodsSince(createReportingPeriod("year", new Date(2026, 1, 1)), first)).toBe(3);
    });

    it("counts local weeks correctly across daylight-saving changes", () => {
      process.env.TZ = "America/New_York";
      const first = new Date(2026, 2, 1, 12).toISOString();
      expect(countPeriodsSince(createReportingPeriod("week", new Date(2026, 2, 16)), first)).toBe(4);
    });
  });

  it("identifies the current period and prevents treating a past one as current", () => {
    const now = new Date(2026, 5, 15, 12);
    const current = createReportingPeriod("month", now);
    const previous = getAdjacentPeriod(current, -1);

    expect(isCurrentPeriod(current, now)).toBe(true);
    expect(isCurrentPeriod(previous, now)).toBe(false);
  });
});

const EFFORT_COLOURS: Record<number, string> = {
  1: "#22C55E",
  2: "#4ADE80",
  3: "#84CC16",
  4: "#A3E635",
  5: "#EAB308",
  6: "#F59E0B",
  7: "#F97316",
  8: "#EA580C",
  9: "#EF4444",
  10: "#DC2626",
};

export function getEffortColour(value: number): string {
  return EFFORT_COLOURS[value] ?? "";
}

const EFFORT_LABELS: Record<number, string> = {
  1: "Easy",
  2: "Easy",
  3: "Easy",
  4: "Moderate",
  5: "Moderate",
  6: "Moderate",
  7: "Hard",
  8: "Hard",
  9: "All Out",
  10: "All Out",
};

export function getEffortLabel(value: number): string {
  return EFFORT_LABELS[value] ?? "";
}

export function reorder<T>(arr: T[], fromIndex: number, toIndex: number): void {
  if (
    fromIndex === toIndex ||
    fromIndex < 0 ||
    toIndex < 0 ||
    fromIndex >= arr.length ||
    toIndex >= arr.length
  ) {
    return;
  }
  arr.splice(toIndex, 0, arr.splice(fromIndex, 1)[0]);
}

export function shuffle<T>(arr: readonly T[]): T[] {
  const result = [...arr];
  for (let i = result.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [result[i], result[j]] = [result[j], result[i]];
  }
  return result;
}

export function applyOrder<T extends { readonly exerciseId: string }>(
  exercises: readonly T[],
  order: readonly string[]
): T[] {
  const validOrder = order.filter(id => exercises.some(e => e.exerciseId === id));
  if (validOrder.length === 0) return [...exercises];

  const orderedExercises: T[] = [];
  const remaining = [...exercises];

  for (const id of validOrder) {
    const idx = remaining.findIndex(e => e.exerciseId === id);
    if (idx !== -1) {
      orderedExercises.push(remaining.splice(idx, 1)[0]);
    }
  }

  // Append any exercises not present in the order list
  orderedExercises.push(...remaining);

  return orderedExercises;
}

// Maps a data value to a Y coordinate within the SVG plot area (y: 20–220, top-to-bottom).
// When min === max (flat line), returns 120 (midpoint) to avoid division-by-zero.
export function normaliseValue(value: number, min: number, max: number): number {
  if (min === max) return 120;
  return 220 - ((value - min) / (max - min)) * 200;
}

// Returns tickCount evenly spaced tick values from min to max inclusive.
// If tickCount < 2, returns [min].
export function buildYTicks(min: number, max: number, tickCount: number): number[] {
  if (tickCount < 2) return [min];
  const ticks: number[] = [];
  for (let i = 0; i < tickCount; i++) {
    ticks.push(min + (i / (tickCount - 1)) * (max - min));
  }
  return ticks;
}

// Returns an array the same length as dates. Labels are shown at evenly spaced indices up to
// maxLabels (always including the last); intermediate positions get null (no label rendered).
// Shown dates are formatted as "DD MMM" (e.g. "01 Apr").
export function buildXLabels(dates: readonly string[], maxLabels: number): (string | null)[] {
  if (dates.length === 0) return [];
  const result: (string | null)[] = new Array(dates.length).fill(null);
  if (maxLabels <= 0) return result;

  // Always show the last label
  const indices = new Set<number>([dates.length - 1]);

  if (maxLabels > 1 && dates.length > 1) {
    const step = (dates.length - 1) / (maxLabels - 1);
    for (let i = 0; i < maxLabels - 1; i++) {
      indices.add(Math.round(i * step));
    }
  }

  for (const idx of indices) {
    const d = new Date(dates[idx]);
    result[idx] = d.toLocaleDateString("en-GB", { day: "2-digit", month: "short" });
  }

  return result;
}

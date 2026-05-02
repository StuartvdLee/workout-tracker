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

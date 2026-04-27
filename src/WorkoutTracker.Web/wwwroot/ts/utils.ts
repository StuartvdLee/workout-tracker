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

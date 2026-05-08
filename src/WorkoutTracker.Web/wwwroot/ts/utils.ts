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

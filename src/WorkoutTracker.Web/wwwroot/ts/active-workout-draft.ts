export interface DraftEntry {
  loggedWeight: string;
  loggedEffort: number | null;
}

export interface CompletionPayload {
  completionOperationId: string;
  sets: 3 | 5;
  overallEffort: number | null;
  loggedExercises: {
    exerciseId: string;
    loggedWeight: string | null;
    effort: number | null;
    sequence: number;
  }[];
}

export interface ActiveWorkoutDraft {
  version: 1;
  draftId: string;
  workoutId: string;
  sets: 3 | 5;
  exerciseOrder: string[];
  entries: Record<string, DraftEntry>;
  completion: CompletionPayload | null;
}

export const DRAFT_STORAGE_KEY = "workout-tracker-active-draft";
const MAX_BYTES = 1_048_576;
const MAX_EXERCISES = 1_000;
const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isId(value: unknown): value is string {
  return typeof value === "string" && UUID.test(value) && value !== "00000000-0000-0000-0000-000000000000";
}

function isEffort(value: unknown): value is number | null {
  return value === null || (typeof value === "number" && Number.isInteger(value) && value >= 1 && value <= 10);
}

function isDraft(value: unknown): value is ActiveWorkoutDraft {
  if (!isRecord(value) || value.version !== 1 || !isId(value.draftId) || !isId(value.workoutId) ||
      (value.sets !== 3 && value.sets !== 5) || !Array.isArray(value.exerciseOrder) ||
      value.exerciseOrder.length > MAX_EXERCISES || !value.exerciseOrder.every(isId) ||
      new Set(value.exerciseOrder).size !== value.exerciseOrder.length || !isRecord(value.entries)) return false;
  const order: string[] = value.exerciseOrder;
  if (Object.keys(value.entries).length !== order.length || !order.every(id => {
    const entry = (value.entries as Record<string, unknown>)[id];
    return isRecord(entry) && typeof entry.loggedWeight === "string" &&
      entry.loggedWeight.length <= 100 && isEffort(entry.loggedEffort);
  })) return false;
  if (value.completion === null) return true;
  const completion = value.completion;
  if (!isRecord(completion) || completion.completionOperationId !== value.draftId ||
      completion.sets !== value.sets || !isEffort(completion.overallEffort) ||
      !Array.isArray(completion.loggedExercises) || completion.loggedExercises.length !== order.length) return false;
  return completion.loggedExercises.every((item: unknown, index: number) => {
    if (!isRecord(item) || item.exerciseId !== order[index] || item.sequence !== index ||
        !(item.loggedWeight === null || (typeof item.loggedWeight === "string" && item.loggedWeight.length <= 100)) ||
        !isEffort(item.effort)) return false;
    const entry = (value.entries as Record<string, DraftEntry>)[order[index]];
    return item.loggedWeight === (entry.loggedWeight || null) && item.effort === entry.loggedEffort;
  });
}

export type DraftReadResult =
  | { status: "missing" | "invalid" | "unavailable" }
  | { status: "found"; draft: ActiveWorkoutDraft };

export function readDraft(): DraftReadResult {
  let raw: string | null;
  try {
    raw = localStorage.getItem(DRAFT_STORAGE_KEY);
  } catch {
    return { status: "unavailable" };
  }
  if (raw === null) return { status: "missing" };
  if (new TextEncoder().encode(raw).length > MAX_BYTES) return { status: "invalid" };
  try {
    const value: unknown = JSON.parse(raw);
    return isDraft(value) ? { status: "found", draft: value } : { status: "invalid" };
  } catch {
    return { status: "invalid" };
  }
}

export function writeDraft(draft: ActiveWorkoutDraft): void {
  if (!isDraft(draft)) throw new Error("The workout draft contains invalid or oversized values.");
  const raw = JSON.stringify(draft);
  if (new TextEncoder().encode(raw).length > MAX_BYTES) throw new Error("The workout draft is too large.");
  localStorage.setItem(DRAFT_STORAGE_KEY, raw);
}

export function removeDraft(): void {
  localStorage.removeItem(DRAFT_STORAGE_KEY);
}

export type DraftLock = { status: "acquired"; release: () => void } | { status: "busy" | "unavailable" };
let releasingLock: Promise<void> | null = null;

export async function acquireDraftLock(): Promise<DraftLock> {
  if (!navigator.locks) return { status: "unavailable" };
  if (releasingLock) await releasingLock;
  return new Promise(resolve => {
    let resolved = false;
    const resolveOnce = (result: DraftLock): void => {
      if (!resolved) {
        resolved = true;
        resolve(result);
      }
    };
    try {
      const request = navigator.locks.request("workout-tracker-active-draft", { ifAvailable: true }, lock => {
        if (!lock) {
          resolveOnce({ status: "busy" });
          return;
        }
        return new Promise<void>(release => {
          resolveOnce({ status: "acquired", release: () => {
            release();
            releasingLock = request.then(() => undefined, () => undefined);
          } });
        });
      });
      void request.catch(() => resolveOnce({ status: "unavailable" }));
    } catch {
      resolveOnce({ status: "unavailable" });
    }
  });
}

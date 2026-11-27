import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  DRAFT_STORAGE_KEY,
  readDraft,
  writeDraft,
  removeDraft,
  acquireDraftLock,
  type ActiveWorkoutDraft,
} from "../active-workout-draft.js";

const values = new Map<string, string>();
const storage = {
  getItem: vi.fn((key: string) => values.get(key) ?? null),
  setItem: vi.fn((key: string, value: string) => { values.set(key, value); }),
  removeItem: vi.fn((key: string) => { values.delete(key); }),
};
vi.stubGlobal("localStorage", storage);

const draft: ActiveWorkoutDraft = {
  version: 1,
  draftId: "a1000000-0000-4000-8000-000000000001",
  workoutId: "a1000000-0000-4000-8000-000000000002",
  sets: 5,
  exerciseOrder: ["a1000000-0000-4000-8000-000000000003"],
  entries: {
    "a1000000-0000-4000-8000-000000000003": { loggedWeight: "42.5", loggedEffort: 6 },
  },
  completion: null,
};

describe("active workout drafts", () => {
  beforeEach(() => {
    values.clear();
    vi.clearAllMocks();
  });

  it("round trips entered values, sets and order", () => {
    writeDraft(draft);
    expect(readDraft()).toEqual({ status: "found", draft });
    removeDraft();
    expect(readDraft()).toEqual({ status: "missing" });
  });

  it("round trips both set counts and committed order with touched and untouched effort", () => {
    const updated = structuredClone(draft);
    const otherId = "a1000000-0000-4000-8000-000000000004";
    updated.exerciseOrder = [otherId, updated.exerciseOrder[0]];
    updated.entries[otherId] = { loggedWeight: "12", loggedEffort: null };
    for (const sets of [3, 5] as const) {
      updated.sets = sets;
      writeDraft(updated);
      expect(readDraft()).toEqual({ status: "found", draft: updated });
    }
  });

  it("reports invalid or unsupported data without deleting it", () => {
    localStorage.setItem(DRAFT_STORAGE_KEY, '{"version":2}');
    expect(readDraft()).toEqual({ status: "invalid" });
    expect(localStorage.getItem(DRAFT_STORAGE_KEY)).toBe('{"version":2}');
  });

  it("rejects excess weight without replacing an earlier draft", () => {
    writeDraft(draft);
    const invalid = structuredClone(draft);
    invalid.entries[invalid.exerciseOrder[0]].loggedWeight = "1".repeat(101);
    expect(() => writeDraft(invalid)).toThrow();
    expect(readDraft()).toEqual({ status: "found", draft });
  });

  it("reports unavailable storage", () => {
    storage.getItem.mockImplementationOnce(() => { throw new Error("blocked"); });
    expect(readDraft()).toEqual({ status: "unavailable" });
  });

  it("preserves untouched effort and accepts the 100-character weight boundary", () => {
    const updated = structuredClone(draft);
    updated.sets = 3;
    updated.entries[updated.exerciseOrder[0]] = { loggedWeight: "1".repeat(100), loggedEffort: null };
    writeDraft(updated);
    expect(readDraft()).toEqual({ status: "found", draft: updated });
  });

  it("enforces the exercise-count and uniqueness boundaries", () => {
    const updated = structuredClone(draft);
    updated.exerciseOrder = Array.from({ length: 1000 }, (_, index) =>
      `a1000000-0000-4000-8000-${(index + 1).toString(16).padStart(12, "0")}`);
    updated.entries = Object.fromEntries(updated.exerciseOrder.map(id => [id, { loggedWeight: "", loggedEffort: null }]));
    writeDraft(updated);
    expect(readDraft().status).toBe("found");
    updated.exerciseOrder.push("a1000000-0000-4000-8000-000000000fff");
    updated.entries[updated.exerciseOrder[1000]] = { loggedWeight: "", loggedEffort: null };
    expect(() => writeDraft(updated)).toThrow();
    updated.exerciseOrder = [updated.exerciseOrder[0], updated.exerciseOrder[0]];
    expect(() => writeDraft(updated)).toThrow();
  });

  it("enforces the exact UTF-8 byte limit before parsing", () => {
    const base = JSON.stringify(draft).slice(0, -1) + ',"padding":"';
    const raw = base + "x".repeat(1_048_576 - base.length - 2) + '"}';
    storage.setItem(DRAFT_STORAGE_KEY, raw);
    expect(new TextEncoder().encode(raw).length).toBe(1_048_576);
    expect(readDraft().status).toBe("found");
    storage.setItem(DRAFT_STORAGE_KEY, raw.replace('"}', 'x"}'));
    expect(readDraft().status).toBe("invalid");
  });

  it("rejects malformed, inconsistent, and mismatched completion data", () => {
    storage.setItem(DRAFT_STORAGE_KEY, "{");
    expect(readDraft().status).toBe("invalid");
    const updated = structuredClone(draft);
    updated.completion = {
      completionOperationId: updated.workoutId, sets: 5, overallEffort: null,
      loggedExercises: [{ exerciseId: updated.exerciseOrder[0], loggedWeight: "42.5", effort: 6, sequence: 0 }],
    };
    expect(() => writeDraft(updated)).toThrow();
  });

  it("rejects invalid fields and entries that do not match the exercise order", () => {
    const invalidSets = { ...draft, sets: 4 };
    values.set(DRAFT_STORAGE_KEY, JSON.stringify(invalidSets));
    expect(readDraft().status).toBe("invalid");
    const invalidEffort = structuredClone(draft);
    invalidEffort.entries[invalidEffort.exerciseOrder[0]].loggedEffort = 11;
    expect(() => writeDraft(invalidEffort)).toThrow();
    const mismatchedEntries = structuredClone(draft);
    delete mismatchedEntries.entries[mismatchedEntries.exerciseOrder[0]];
    expect(() => writeDraft(mismatchedEntries)).toThrow();
  });

  it("surfaces write and removal failures", () => {
    storage.setItem.mockImplementationOnce(() => { throw new Error("quota"); });
    expect(() => writeDraft(draft)).toThrow("quota");
    storage.removeItem.mockImplementationOnce(() => { throw new Error("blocked"); });
    expect(() => removeDraft()).toThrow("blocked");
  });

  it("reports unavailable, busy and synchronously failing locks", async () => {
    vi.stubGlobal("navigator", {});
    expect(await acquireDraftLock()).toEqual({ status: "unavailable" });
    vi.stubGlobal("navigator", { locks: { request: (_name: string, _options: object, callback: (lock: null) => void) => {
      callback(null);
      return Promise.resolve();
    } } });
    expect(await acquireDraftLock()).toEqual({ status: "busy" });
    vi.stubGlobal("navigator", { locks: { request: () => { throw new Error("blocked"); } } });
    expect(await acquireDraftLock()).toEqual({ status: "unavailable" });
    vi.unstubAllGlobals();
    vi.stubGlobal("localStorage", storage);
  });

  it("waits for its own released lock before reacquiring", async () => {
    let occupied = false;
    vi.stubGlobal("navigator", { locks: { request: async (_name: string, _options: object, callback: (lock: object | null) => Promise<void> | void) => {
      if (occupied) return callback(null);
      occupied = true;
      await callback({});
      occupied = false;
    } } });
    const first = await acquireDraftLock();
    expect(first.status).toBe("acquired");
    if (first.status !== "acquired") throw new Error("Lock was not acquired");
    first.release();
    const second = await acquireDraftLock();
    expect(second.status).toBe("acquired");
    if (second.status === "acquired") second.release();
    vi.unstubAllGlobals();
    vi.stubGlobal("localStorage", storage);
  });
});

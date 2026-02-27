import { apiFetch } from "./apiClient";
import type { ExerciseEntry, WeightUnit } from "./models";

export async function addEntry(
    sessionId: string,
    input: {
        exerciseName: string;
        sets: number;
        reps: number;
        weight: number;
        weightUnit: WeightUnit;
        performedAt: string;
    }
): Promise<ExerciseEntry> {
    return apiFetch<ExerciseEntry>(`/sessions/${sessionId}/entries`, {
        method: "POST",
        body: JSON.stringify(input)
    });
}

export async function listSessionEntries(sessionId: string): Promise<ExerciseEntry[]> {
    return apiFetch<ExerciseEntry[]>(`/sessions/${sessionId}/entries`);
}

export async function updateEntry(entryId: string, input: Partial<Pick<ExerciseEntry, "sets" | "reps" | "weight" | "performedAt">>): Promise<ExerciseEntry> {
    return apiFetch<ExerciseEntry>(`/entries/${entryId}`, {
        method: "PATCH",
        body: JSON.stringify(input)
    });
}

export async function deleteEntry(entryId: string): Promise<void> {
    await apiFetch<void>(`/entries/${entryId}`, { method: "DELETE" });
}

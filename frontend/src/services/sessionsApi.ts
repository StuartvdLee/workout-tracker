import { apiFetch } from "./apiClient";
import type { WorkoutSession } from "./models";

export async function createSession(startedAt: string, notes?: string): Promise<WorkoutSession> {
    return apiFetch<WorkoutSession>("/sessions", {
        method: "POST",
        body: JSON.stringify({ startedAt, notes: notes || null })
    });
}

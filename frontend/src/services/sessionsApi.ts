import { apiFetch } from "./apiClient";
import type { CreateSessionPayload, WorkoutSession } from "./models";

export async function createSession(payload: CreateSessionPayload): Promise<WorkoutSession>;
export async function createSession(startedAt: string, notes?: string): Promise<WorkoutSession>;
export async function createSession(payloadOrStartedAt: CreateSessionPayload | string, notes?: string): Promise<WorkoutSession> {
    const payload = typeof payloadOrStartedAt === "string"
        ? { workoutType: "Push", startedAt: payloadOrStartedAt, notes: notes || null }
        : { workoutType: payloadOrStartedAt.workoutType, startedAt: payloadOrStartedAt.startedAt, notes: payloadOrStartedAt.notes || null };

    return apiFetch<WorkoutSession>("/sessions", {
        method: "POST",
        body: JSON.stringify(payload)
    });
}

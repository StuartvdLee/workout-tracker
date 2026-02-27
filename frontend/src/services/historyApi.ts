import { apiFetch } from "./apiClient";
import type { ExerciseHistoryResponse } from "./models";

export async function getExerciseHistory(exerciseName: string, page = 1, pageSize = 25): Promise<ExerciseHistoryResponse> {
    const encoded = encodeURIComponent(exerciseName);
    return apiFetch<ExerciseHistoryResponse>(`/exercises/${encoded}/history?page=${page}&pageSize=${pageSize}`);
}

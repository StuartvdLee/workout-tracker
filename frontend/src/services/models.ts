export type WeightUnit = "kg" | "lb";

export interface WorkoutSession {
    id: string;
    startedAt: string;
    endedAt?: string | null;
    notes?: string | null;
}

export interface ExerciseEntry {
    id: string;
    sessionId: string;
    exerciseName: string;
    normalizedExerciseName: string;
    sets: number;
    reps: number;
    weight: number;
    weightUnit: WeightUnit;
    performedAt: string;
}

export interface ExerciseHistoryResponse {
    exerciseName: string;
    page: number;
    pageSize: number;
    total: number;
    entries: ExerciseEntry[];
}

export interface ProgressComparison {
    exerciseName: string;
    currentEntryId: string;
    currentVolume: number;
    previousEntryId?: string | null;
    previousVolume?: number | null;
    bestEntryId?: string | null;
    bestVolume?: number | null;
    deltaFromPrevious?: number | null;
    deltaFromBest?: number | null;
    statusVsPrevious: "improved" | "unchanged" | "declined" | "no-baseline";
    statusVsBest: "improved" | "unchanged" | "declined" | "no-baseline";
}

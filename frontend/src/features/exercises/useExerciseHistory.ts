import { useEffect, useState } from "react";
import { getExerciseHistory } from "../../services/historyApi";
import type { ExerciseHistoryResponse } from "../../services/models";

export function useExerciseHistory(exerciseName: string, page: number, pageSize = 25) {
    const [data, setData] = useState<ExerciseHistoryResponse | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        if (!exerciseName.trim()) {
            setData(null);
            return;
        }

        let isCancelled = false;
        setLoading(true);
        setError(null);

        getExerciseHistory(exerciseName, page, pageSize)
            .then((response) => {
                if (!isCancelled) {
                    setData(response);
                }
            })
            .catch(() => {
                if (!isCancelled) {
                    setError("Unable to load exercise history.");
                }
            })
            .finally(() => {
                if (!isCancelled) {
                    setLoading(false);
                }
            });

        return () => {
            isCancelled = true;
        };
    }, [exerciseName, page, pageSize]);

    return { data, error, loading };
}

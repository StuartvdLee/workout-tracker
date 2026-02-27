import { useState } from "react";
import { ExerciseHistoryList } from "../features/exercises/ExerciseHistoryList";
import { useExerciseHistory } from "../features/exercises/useExerciseHistory";

export function HistoryPage() {
    const [exerciseName, setExerciseName] = useState("");
    const [page, setPage] = useState(1);
    const { data, error, loading } = useExerciseHistory(exerciseName, page);

    return (
        <section>
            <h2>Exercise History</h2>
            <label htmlFor="history-exercise-name">Exercise Name</label>
            <input
                id="history-exercise-name"
                value={exerciseName}
                onChange={(event) => {
                    setExerciseName(event.target.value);
                    setPage(1);
                }}
            />

            <ExerciseHistoryList
                entries={data?.entries ?? []}
                page={data?.page ?? page}
                pageSize={data?.pageSize ?? 25}
                total={data?.total ?? 0}
                loading={loading}
                error={error}
                onPageChange={setPage}
            />
        </section>
    );
}

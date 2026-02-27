import type { ExerciseEntry } from "../../services/models";

interface ExerciseHistoryListProps {
    entries: ExerciseEntry[];
    page: number;
    pageSize: number;
    total: number;
    loading: boolean;
    error?: string | null;
    onPageChange: (page: number) => void;
}

export function ExerciseHistoryList({ entries, page, pageSize, total, loading, error, onPageChange }: ExerciseHistoryListProps) {
    const totalPages = Math.max(1, Math.ceil(total / pageSize));

    if (loading) {
        return <p>Loading history…</p>;
    }

    if (error) {
        return <p role="alert">{error}</p>;
    }

    if (entries.length === 0) {
        return <p>No matching history entries found.</p>;
    }

    return (
        <section aria-label="Exercise history list">
            <ul>
                {entries.map((entry) => (
                    <li key={entry.id}>
                        {new Date(entry.performedAt).toLocaleString()} - {entry.sets} x {entry.reps} @ {entry.weight} {entry.weightUnit}
                    </li>
                ))}
            </ul>

            <div>
                <button type="button" onClick={() => onPageChange(Math.max(1, page - 1))} disabled={page <= 1}>
                    Previous
                </button>
                <span aria-live="polite">
                    Page {page} of {totalPages}
                </span>
                <button type="button" onClick={() => onPageChange(Math.min(totalPages, page + 1))} disabled={page >= totalPages}>
                    Next
                </button>
            </div>
        </section>
    );
}

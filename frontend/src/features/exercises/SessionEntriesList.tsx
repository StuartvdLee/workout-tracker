import { useState } from "react";
import type { ExerciseEntry } from "../../services/models";

interface SessionEntriesListProps {
    entries: ExerciseEntry[];
    onUpdate: (entryId: string, payload: { sets?: number; reps?: number; weight?: number }) => Promise<void>;
    onDelete: (entryId: string) => Promise<void>;
}

export function SessionEntriesList({ entries, onUpdate, onDelete }: SessionEntriesListProps) {
    const [editingId, setEditingId] = useState<string | null>(null);
    const [editSets, setEditSets] = useState(0);
    const [editReps, setEditReps] = useState(0);
    const [editWeight, setEditWeight] = useState(0);

    if (entries.length === 0) {
        return <p>No entries yet.</p>;
    }

    return (
        <ul aria-label="Session entries">
            {entries.map((entry) => (
                <li key={entry.id}>
                    <strong>{entry.exerciseName}</strong> - {entry.sets} x {entry.reps} @ {entry.weight} {entry.weightUnit}

                    {editingId === entry.id ? (
                        <form
                            onSubmit={async (event) => {
                                event.preventDefault();
                                await onUpdate(entry.id, { sets: editSets, reps: editReps, weight: editWeight });
                                setEditingId(null);
                            }}
                        >
                            <input type="number" min={1} value={editSets} onChange={(event) => setEditSets(Number(event.target.value))} />
                            <input type="number" min={1} value={editReps} onChange={(event) => setEditReps(Number(event.target.value))} />
                            <input type="number" min={0} value={editWeight} onChange={(event) => setEditWeight(Number(event.target.value))} />
                            <button type="submit">Save</button>
                        </form>
                    ) : (
                        <>
                            <button
                                type="button"
                                onClick={() => {
                                    setEditingId(entry.id);
                                    setEditSets(entry.sets);
                                    setEditReps(entry.reps);
                                    setEditWeight(entry.weight);
                                }}
                            >
                                Edit
                            </button>
                            <button type="button" onClick={() => onDelete(entry.id)}>
                                Delete
                            </button>
                        </>
                    )}
                </li>
            ))}
        </ul>
    );
}

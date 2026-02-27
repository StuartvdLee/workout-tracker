import { useState } from "react";
import type { WeightUnit } from "../../services/models";

interface ExerciseEntryFormProps {
    disabled?: boolean;
    onSubmit: (payload: {
        exerciseName: string;
        sets: number;
        reps: number;
        weight: number;
        weightUnit: WeightUnit;
        performedAt: string;
    }) => Promise<void>;
}

export function ExerciseEntryForm({ disabled, onSubmit }: ExerciseEntryFormProps) {
    const [exerciseName, setExerciseName] = useState("");
    const [sets, setSets] = useState(3);
    const [reps, setReps] = useState(8);
    const [weight, setWeight] = useState(20);
    const [weightUnit, setWeightUnit] = useState<WeightUnit>("kg");
    const [performedAt, setPerformedAt] = useState(new Date().toISOString().slice(0, 16));
    const [error, setError] = useState<string | null>(null);

    async function handleSubmit(event: React.FormEvent) {
        event.preventDefault();
        setError(null);

        if (!exerciseName.trim()) {
            setError("Exercise name is required.");
            return;
        }

        if (sets < 1 || reps < 1 || weight < 0) {
            setError("Sets and reps must be greater than zero and weight cannot be negative.");
            return;
        }

        await onSubmit({
            exerciseName: exerciseName.trim(),
            sets,
            reps,
            weight,
            weightUnit,
            performedAt: new Date(performedAt).toISOString()
        });

        setExerciseName("");
    }

    return (
        <form onSubmit={handleSubmit} aria-label="Add exercise entry form">
            <h3>Add Exercise Entry</h3>

            <label htmlFor="exercise-name">Exercise</label>
            <input
                id="exercise-name"
                value={exerciseName}
                onChange={(event) => setExerciseName(event.target.value)}
                required
            />

            <label htmlFor="entry-sets">Sets</label>
            <input
                id="entry-sets"
                type="number"
                min={1}
                max={100}
                value={sets}
                onChange={(event) => setSets(Number(event.target.value))}
                required
            />

            <label htmlFor="entry-reps">Reps</label>
            <input
                id="entry-reps"
                type="number"
                min={1}
                max={500}
                value={reps}
                onChange={(event) => setReps(Number(event.target.value))}
                required
            />

            <label htmlFor="entry-weight">Weight</label>
            <input
                id="entry-weight"
                type="number"
                min={0}
                step="0.5"
                value={weight}
                onChange={(event) => setWeight(Number(event.target.value))}
                required
            />

            <label htmlFor="entry-weight-unit">Unit</label>
            <select
                id="entry-weight-unit"
                value={weightUnit}
                onChange={(event) => setWeightUnit(event.target.value as WeightUnit)}
            >
                <option value="kg">kg</option>
                <option value="lb">lb</option>
            </select>

            <label htmlFor="entry-performed-at">Performed At</label>
            <input
                id="entry-performed-at"
                type="datetime-local"
                value={performedAt}
                onChange={(event) => setPerformedAt(event.target.value)}
                required
            />

            {error ? <p role="alert">{error}</p> : null}

            <button type="submit" disabled={disabled}>Save Entry</button>
        </form>
    );
}

import { useState } from "react";

interface SessionCreateFormProps {
    onCreate: (payload: { startedAt: string; notes?: string }) => Promise<void>;
}

export function SessionCreateForm({ onCreate }: SessionCreateFormProps) {
    const [startedAt, setStartedAt] = useState(new Date().toISOString().slice(0, 16));
    const [notes, setNotes] = useState("");
    const [error, setError] = useState<string | null>(null);

    async function handleSubmit(event: React.FormEvent) {
        event.preventDefault();
        setError(null);
        if (!startedAt) {
            setError("Session start time is required.");
            return;
        }

        await onCreate({ startedAt: new Date(startedAt).toISOString(), notes });
        setNotes("");
    }

    return (
        <form onSubmit={handleSubmit} aria-label="Create session form">
            <h3>Create Session</h3>
            <label htmlFor="session-started-at">Started At</label>
            <input
                id="session-started-at"
                type="datetime-local"
                value={startedAt}
                onChange={(event) => setStartedAt(event.target.value)}
                required
            />

            <label htmlFor="session-notes">Notes</label>
            <textarea
                id="session-notes"
                value={notes}
                onChange={(event) => setNotes(event.target.value)}
            />

            {error ? <p role="alert">{error}</p> : null}
            <button type="submit">Start Session</button>
        </form>
    );
}

import { useState } from "react";
import { ExerciseEntryForm } from "../features/exercises/ExerciseEntryForm";
import { SessionEntriesList } from "../features/exercises/SessionEntriesList";
import { SessionCreateForm } from "../features/sessions/SessionCreateForm";
import { addEntry, deleteEntry, listSessionEntries, updateEntry } from "../services/entriesApi";
import { createSession } from "../services/sessionsApi";
import type { ExerciseEntry, WorkoutSession } from "../services/models";

export function SessionsPage() {
    const [session, setSession] = useState<WorkoutSession | null>(null);
    const [entries, setEntries] = useState<ExerciseEntry[]>([]);

    async function refreshEntries(sessionId: string) {
        const result = await listSessionEntries(sessionId);
        setEntries(result);
    }

    return (
        <section>
            <h2>Sessions</h2>
            <p>Create a workout session and log exercises.</p>

            <SessionCreateForm
                onCreate={async ({ startedAt, notes }) => {
                    const created = await createSession(startedAt, notes);
                    setSession(created);
                    setEntries([]);
                }}
            />

            <ExerciseEntryForm
                disabled={!session}
                onSubmit={async (payload) => {
                    if (!session) {
                        return;
                    }

                    await addEntry(session.id, payload);
                    await refreshEntries(session.id);
                }}
            />

            <SessionEntriesList
                entries={entries}
                onUpdate={async (entryId, payload) => {
                    await updateEntry(entryId, payload);
                    if (session) {
                        await refreshEntries(session.id);
                    }
                }}
                onDelete={async (entryId) => {
                    await deleteEntry(entryId);
                    if (session) {
                        await refreshEntries(session.id);
                    }
                }}
            />
        </section>
    );
}

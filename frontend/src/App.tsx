import { useState } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { SessionsPage } from "./pages/SessionsPage";
import { HistoryPage } from "./pages/HistoryPage";
import { ProgressionPage } from "./pages/ProgressionPage";
import { mapApiError } from "./services/apiErrorMapper";
import { createSession } from "./services/sessionsApi";
import type { WorkoutType } from "./services/models";

function HomePage() {
    const [workoutType, setWorkoutType] = useState<WorkoutType | "">("");
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    async function handleStartSession(event: React.FormEvent) {
        event.preventDefault();
        setSuccess(null);

        if (!workoutType) {
            setError("Please select a workout type.");
            return;
        }

        try {
            const session = await createSession({ workoutType });
            setError(null);
            setSuccess(`Session started (${session.workoutType}) at ${new Date(session.startedAt).toLocaleString()}.`);
        } catch (requestError) {
            setError(mapApiError(requestError));
        }
    }

    return (
        <section className="home-page">
            <h1 className="home-title">Workout Tracker</h1>
            <form className="home-form" onSubmit={handleStartSession} aria-label="Start workout session form">
                <label className="sr-only" htmlFor="workout-type">Workout Type</label>
                <select
                    className="workout-select"
                    id="workout-type"
                    value={workoutType}
                    onChange={(event) => {
                        const selected = event.target.value as WorkoutType | "";
                        setWorkoutType(selected);
                        if (selected) {
                            setError(null);
                        }
                    }}
                    aria-describedby={error ? "workout-type-error" : undefined}
                >
                    <option value="">Select workout type</option>
                    <option value="Push">Push</option>
                    <option value="Pull">Pull</option>
                    <option value="Legs">Legs</option>
                </select>

                <p id="workout-type-error" role="alert" className="error-slot">
                    {error ?? "\u00A0"}
                </p>

                <button className="start-button" type="submit">Start Session</button>
            </form>

            {success ? <p className="success-text">{success}</p> : null}
        </section>
    );
}

export function App() {
    return (
        <BrowserRouter>
            <main>
                <Routes>
                    <Route path="/" element={<HomePage />} />
                    <Route path="/sessions" element={<SessionsPage />} />
                    <Route path="/history" element={<HistoryPage />} />
                    <Route path="/progression" element={<ProgressionPage />} />
                    <Route path="*" element={<Navigate to="/" replace />} />
                </Routes>
            </main>
        </BrowserRouter>
    );
}

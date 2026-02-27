import { BrowserRouter, Link, Navigate, Route, Routes } from "react-router-dom";
import { SessionsPage } from "./pages/SessionsPage";
import { HistoryPage } from "./pages/HistoryPage";
import { ProgressionPage } from "./pages/ProgressionPage";

export function App() {
    return (
        <BrowserRouter>
            <main>
                <h1>Strength Progression Tracker</h1>
                <nav>
                    <Link to="/sessions">Sessions</Link> | <Link to="/history">History</Link> | <Link to="/progression">Progression</Link>
                </nav>
                <Routes>
                    <Route path="/sessions" element={<SessionsPage />} />
                    <Route path="/history" element={<HistoryPage />} />
                    <Route path="/progression" element={<ProgressionPage />} />
                    <Route path="*" element={<Navigate to="/sessions" replace />} />
                </Routes>
            </main>
        </BrowserRouter>
    );
}

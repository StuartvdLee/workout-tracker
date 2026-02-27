import type { ProgressComparison } from "../../services/models";

interface ProgressComparisonCardProps {
    comparison: ProgressComparison | null;
    loading: boolean;
    error?: string | null;
}

function labelForStatus(status: ProgressComparison["statusVsPrevious"]) {
    switch (status) {
        case "improved":
            return "Improved";
        case "declined":
            return "Declined";
        case "unchanged":
            return "Unchanged";
        default:
            return "No baseline";
    }
}

export function ProgressComparisonCard({ comparison, loading, error }: ProgressComparisonCardProps) {
    if (loading) {
        return <p>Loading comparison…</p>;
    }

    if (error) {
        return <p role="alert">{error}</p>;
    }

    if (!comparison) {
        return <p>Enter an entry ID to view comparison.</p>;
    }

    return (
        <section aria-label="Progress comparison card">
            <h3>{comparison.exerciseName}</h3>
            <p>Current volume: {comparison.currentVolume}</p>

            <p>
                Vs Previous: <strong>{labelForStatus(comparison.statusVsPrevious)}</strong>
                {comparison.deltaFromPrevious != null ? ` (${comparison.deltaFromPrevious >= 0 ? "+" : ""}${comparison.deltaFromPrevious})` : ""}
            </p>

            <p>
                Vs Best: <strong>{labelForStatus(comparison.statusVsBest)}</strong>
                {comparison.deltaFromBest != null ? ` (${comparison.deltaFromBest >= 0 ? "+" : ""}${comparison.deltaFromBest})` : ""}
            </p>
        </section>
    );
}

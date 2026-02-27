import { useState } from "react";
import { ProgressComparisonCard } from "../features/progression/ProgressComparisonCard";
import { useProgressComparison } from "../features/progression/useProgressComparison";

export function ProgressionPage() {
    const [entryId, setEntryId] = useState("");
    const { comparison, loading, error } = useProgressComparison(entryId);

    return (
        <section>
            <h2>Progression</h2>
            <label htmlFor="progress-entry-id">Entry ID</label>
            <input
                id="progress-entry-id"
                value={entryId}
                onChange={(event) => setEntryId(event.target.value)}
                placeholder="Paste entry UUID"
            />

            <ProgressComparisonCard comparison={comparison} loading={loading} error={error} />
        </section>
    );
}

import { useEffect, useState } from "react";
import { getEntryComparison } from "../../services/progressionApi";
import type { ProgressComparison } from "../../services/models";

export function useProgressComparison(entryId: string) {
    const [comparison, setComparison] = useState<ProgressComparison | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (!entryId.trim()) {
            setComparison(null);
            return;
        }

        let isCancelled = false;
        setLoading(true);
        setError(null);

        getEntryComparison(entryId)
            .then((result) => {
                if (!isCancelled) {
                    setComparison(result);
                }
            })
            .catch(() => {
                if (!isCancelled) {
                    setError("Unable to load progression comparison.");
                }
            })
            .finally(() => {
                if (!isCancelled) {
                    setLoading(false);
                }
            });

        return () => {
            isCancelled = true;
        };
    }, [entryId]);

    return { comparison, loading, error };
}

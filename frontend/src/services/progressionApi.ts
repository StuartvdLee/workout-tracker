import { apiFetch } from "./apiClient";
import type { ProgressComparison } from "./models";

export async function getEntryComparison(entryId: string): Promise<ProgressComparison> {
    return apiFetch<ProgressComparison>(`/entries/${entryId}/comparison`);
}

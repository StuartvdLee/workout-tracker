import { describe, expect, it } from "vitest";
import { mapApiError } from "../../src/services/apiErrorMapper";

describe("simplified homepage flow", () => {
    it("maps 400 responses to workout type selection guidance", () => {
        const message = mapApiError(new Error("API request failed: 400"));
        expect(message).toBe("Please select a workout type.");
    });

    it("maps 404 responses to not-found guidance", () => {
        const message = mapApiError(new Error("API request failed: 404"));
        expect(message).toBe("Requested record was not found.");
    });

    it("maps unknown errors to fallback message", () => {
        const message = mapApiError(new Error("network disconnected"));
        expect(message).toBe("Something went wrong. Please try again.");
    });
});

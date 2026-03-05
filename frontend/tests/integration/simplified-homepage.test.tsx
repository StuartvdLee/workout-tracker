import { describe, expect, it } from "vitest";
import React from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { HomePage } from "../../src/HomePage";

describe("simplified homepage flow", () => {
    it("supports selecting workout type and starting a session", async () => {
        render(<HomePage />);

        const user = userEvent.setup();

        const workoutTypeSelect = screen.getByLabelText(/workout type/i);
        await user.selectOptions(workoutTypeSelect, "Strength");

        const startButton = screen.getByRole("button", { name: /start/i });
        await user.click(startButton);

        const activeSessionHeading = await screen.findByText(/active workout/i);
        expect(activeSessionHeading).toBeDefined();
    });

    it("shows error when workout type is missing", async () => {
        render(<HomePage />);

        const user = userEvent.setup();

        const startButton = screen.getByRole("button", { name: /start/i });
        await user.click(startButton);

        const errorMessage = await screen.findByText(/select a workout type/i);
        expect(errorMessage).toBeDefined();
    });

    it("removes legacy homepage navigation and add-entry section", () => {
        render(<HomePage />);

        const legacyNav = screen.queryByTestId("legacy-homepage-nav");
        const legacyAddEntry = screen.queryByTestId("legacy-add-entry-section");

        expect(legacyNav).toBeNull();
        expect(legacyAddEntry).toBeNull();
    });
});

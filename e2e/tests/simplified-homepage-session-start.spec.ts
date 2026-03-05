import { test, expect } from "@playwright/test";

test("simplified homepage shows workout type and start session controls", async ({ page }) => {
    await page.goto("/");
    await expect(page.locator("h1")).toContainText("Workout Tracker");
    await expect(page.getByLabel("Workout Type")).toBeVisible();
    await expect(page.getByRole("button", { name: "Start Session" })).toBeVisible();
});

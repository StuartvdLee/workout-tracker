import { test, expect } from "@playwright/test";

test("critical journey: logging history progression", async ({ page }) => {
    await page.goto("/");
    await expect(page.locator("h1")).toContainText("Strength Progression Tracker");
});

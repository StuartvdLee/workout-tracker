export function render(container: HTMLElement): void {
  const params = new URLSearchParams(window.location.search);
  const workoutId = params.get("id");

  if (!workoutId) {
    container.innerHTML = `
      <div class="active-session">
        <p>No workout selected. <a href="/workouts">Go to Workouts</a></p>
      </div>
    `;
    return;
  }

  container.innerHTML = `
    <div class="active-session">
      <div class="active-session__header">
        <button class="active-session__back-btn" type="button" id="session-back">← Back to Workouts</button>
        <h1 class="active-session__title" id="session-title">Loading...</h1>
      </div>
      <div class="active-session__exercises" id="session-exercises"></div>
      <div class="active-session__error" id="session-error" role="alert" aria-live="polite"></div>
      <div class="active-session__api-error" id="session-api-error" role="alert" aria-live="polite"></div>
      <div class="active-session__actions">
        <button class="active-session__save-btn" type="button" id="session-save">Save Workout</button>
        <button class="active-session__cancel-btn" type="button" id="session-cancel">Cancel</button>
      </div>
    </div>
  `;
}

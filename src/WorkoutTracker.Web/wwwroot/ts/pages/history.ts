export function render(container: HTMLElement): void {
  container.innerHTML = `
    <div class="history-page">
      <h1 class="history-page__title">Workout History</h1>
      <div class="history-page__loading" id="history-loading">Loading...</div>
      <div class="history-page__empty" id="history-empty" style="display:none;">
        No workouts logged yet. Complete your first workout and it will appear here!
      </div>
      <div id="history-list"></div>
    </div>
  `;
}

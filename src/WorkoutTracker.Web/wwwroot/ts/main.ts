import { registerRoute, init } from "./router.js";
import { initSidebar } from "./sidebar.js";
import { render as renderHome } from "./pages/home.js";
import { render as renderWorkouts } from "./pages/workouts.js";
import { render as renderExercises } from "./pages/exercises.js";
import { render as renderHistory } from "./pages/history.js";
import { render as renderActiveSession } from "./pages/active-session.js";

function initializeApp(): void {
  registerRoute("/", renderHome);
  registerRoute("/workouts", renderWorkouts);
  registerRoute("/exercises", renderExercises);
  registerRoute("/history", renderHistory);
  registerRoute("/active-session", renderActiveSession);

  initSidebar();
  init();
}

document.addEventListener("DOMContentLoaded", initializeApp);

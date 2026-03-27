import { registerRoute, init } from "./router.js";
import { initSidebar } from "./sidebar.js";
import { render as renderHome } from "./pages/home.js";
import { render as renderWorkouts } from "./pages/workouts.js";
import { render as renderExercises } from "./pages/exercises.js";

function initializeApp(): void {
  registerRoute("/", renderHome);
  registerRoute("/workouts", renderWorkouts);
  registerRoute("/exercises", renderExercises);

  initSidebar();
  init();
}

document.addEventListener("DOMContentLoaded", initializeApp);

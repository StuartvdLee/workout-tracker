# Data Model: Sidebar Navigation Layout

**Feature**: 002-sidebar-navigation
**Date**: 2026-03-27

## Overview

This feature introduces no new persistent data entities. The sidebar navigation and routing are entirely client-side concerns with no database, API, or server-side state changes.

## Client-Side State

The following transient state is managed in the browser during a user session:

### Route

Represents the current page the user is viewing.

| Attribute    | Type   | Description                                         |
| ------------ | ------ | --------------------------------------------------- |
| path         | string | URL pathname (e.g., `/`, `/workouts`, `/exercises`) |
| pageId       | string | Identifier for the page module to render             |

**Valid routes**:

| Path         | Page ID     | Description                          |
| ------------ | ----------- | ------------------------------------ |
| `/`          | `home`      | Home page with workout selection form |
| `/workouts`  | `workouts`  | Workouts placeholder page             |
| `/exercises` | `exercises` | Exercises placeholder page            |

**State transitions**: Navigating between routes replaces the rendered content in the main area. No data is persisted between navigations. The Home page's workout form state (selected dropdown value, error state) resets when navigating away and back.

### Sidebar State (mobile only)

| Attribute | Type    | Description                              |
| --------- | ------- | ---------------------------------------- |
| isOpen    | boolean | Whether the mobile sidebar is visible     |

**State transitions**:
- **Closed → Open**: User clicks hamburger toggle button
- **Open → Closed**: User selects a menu item, clicks the backdrop, or presses Escape
- **Open → Closed**: Viewport resizes above 768px (sidebar becomes always-visible)

## Existing Entities (unchanged)

The following entities exist in the database and are unaffected by this feature:

- **WorkoutType**: Categories of workouts (fetched via `/api/workout-types` on the Home page)
- **Workout**: User workout sessions
- **Exercise**: Individual exercises
- **WorkoutExercise**: Junction linking workouts to exercises

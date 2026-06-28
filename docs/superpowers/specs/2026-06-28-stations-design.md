# Stations — Design Spec

**Date:** 2026-06-28
**Rung:** 1, step 1 (stations)
**Status:** Approved, ready for implementation plan

---

## Goal

Place two distinguishable space stations in the world, roughly 1500 units apart, each built from primitives in code, plus a minimal on-screen marker that points to the pilot's target station so it can be found across that distance.

This is the first world-content step of Rung 1. Docking, trade, and the full UI come after and are out of scope here.

## Context

- Existing objects are code-spawned primitives. `ReferenceField.cs` builds throwaway shapes at `Start()` via `GameObject.CreatePrimitive`; this is the proven, reliable pattern in this project.
- In-scene object placement via the MCP tooling is unreliable (see project memory). All station content is therefore built in code at Play, not placed in the Editor.
- Flight scale: ship cruises at 40 u/s, 80 boosted. 1500 units is ~37s cruise, ~19s boosted: a real trip without dragging.
- Visual style is intentional placeholder quality.

## Components

Each component has one clear job and can be understood independently.

### `Station.cs`
A component representing a single station.

- **State:** display name (e.g. "Station A"), a short id, a tint color, and a dock point position (a local offset marking where a ship will later dock).
- **Behaviour:** on spawn, builds its own body from primitives so it reads as a station rather than a plain block: a central cylinder core, a flat ring, and a few box modules. Applies its tint color to the parts.
- **Why:** the station owns both its look and its dock point, so the docking step that follows has the data it needs with no rework.
- **Depends on:** Unity primitives only.

### `StationField.cs`
A small spawner that creates and positions the two stations. Mirrors `ReferenceField`.

- **Behaviour:** at `Start()`, instantiates Station A near the origin and Station B ~1500 units away along a fixed axis. Assigns names ("Station A", "Station B"), ids, and colors (A = blue, B = orange). Keeps references to both stations.
- **Why:** one place owns where the stations are and exposes them so other systems (the marker now, docking later) can find them without hunting through the scene.
- **Depends on:** `Station`.

### `TargetMarker.cs`
A code-only on-screen marker pointing at the target station.

- **Behaviour:** each frame, projects the target station's world position to screen space and draws an arrow/indicator with `OnGUI` (no Canvas setup required). When the target is off-screen, the indicator clamps to the screen edge pointing toward it. Target defaults to Station B.
- **Why:** at 1500 units a station is invisible until close; without a pointer the pilot flies blind. `OnGUI` keeps it 100% code and avoids fragile in-Editor Canvas wiring.
- **Depends on:** a reference to the target `Station` (supplied by `StationField`) and the main camera.

## Data Flow

1. `StationField.Start()` spawns Station A and Station B, each of which builds its own body.
2. `StationField` holds references to both and sets the `TargetMarker`'s current target (default: Station B).
3. Each frame, `TargetMarker` reads the target station's position, projects it through the camera, and draws the indicator.

## Scope Boundary

**In scope:** two distinct, findable stations; one target arrow.

**Explicitly parked (do not build here):** docking logic, trade loop, credits/cargo readouts, buy/sell panel, multiple cargo types, varied docking, navigation beyond the single target arrow.

**Scope note:** the target marker pulls a slice of UI ahead of docking (Rung 1 sequences UI last). This is a deliberate, minimal exception so stations are testable. It is limited to a single target arrow; the credits/cargo/buy-sell UI stays parked for step 4.

## Acceptance (manual play-test)

- Press Play: Station A is visible nearby (blue), built from multiple primitives so it reads as a station.
- An on-screen arrow points toward Station B.
- Flying the arrow, Station B (orange) grows into view ~1500 units out.
- When the target station is off-screen, the arrow pins to the screen edge pointing the right way.
- Switching the target back to Station A flips the arrow around.
- No console errors during a full there-and-back flight.

## Open Questions

None blocking. Exact distance, colors, and station part layout are easy to tune during play-test.

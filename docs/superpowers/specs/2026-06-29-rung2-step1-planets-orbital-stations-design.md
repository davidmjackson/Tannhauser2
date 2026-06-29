# Rung 2, Step 1 — Planets, Orbital Stations, and the Cruise/Jump Drive

**Date:** 2026-06-29
**Rung:** 2 (POC), first step
**Status:** Approved design, ready for implementation plan

## Goal

Turn the two-station Rung 1 sandbox into a small three-planet system you fly
around. Add three planets as visual backdrops, one station per planet, on-screen
markers for all stations, and an in-system cruise/jump drive that lets you cross
the larger distances without the flight dragging.

This is the first of three Rung 2 steps. The other two (market price trends, one
cargo mission type) are separate steps with their own spec/plan/build cycles and
are out of scope here.

## Scope decisions (locked during brainstorming)

1. **Three planets, one station each → three stations.** Replaces the current
   two-station setup. Planets are pure visual backdrops: non-landable,
   non-dockable, no collider. Stations remain the only dockable things.
2. **Stations are static-positioned** at a fixed point just off their planet
   (the orbital look, no actual motion).
3. **A marker on every station, always visible.** Reuses `TargetMarker`, one per
   station. Adds a "focused" visual state.
4. **Spread-out system:** triangle layout, legs roughly 3000–4000 units. Exact
   distances are a tunable, set to a sensible default and adjusted by feel.
5. **In-system cruise/jump drive** (explicitly NOT the Rung 4 hyperdrive — no
   system jump): aim to focus a station, press J to lock heading and jump at a
   speed multiplier, no steering during jump, auto-cutout at a realistic range of
   the target or manual disengage with J.

## World layout

- **Planets:** three large procedural spheres (placeholder art, built in code the
  way stations are now), each a distinct color. No collider. Backdrop only.
- **Stations:** one per planet, at a fixed offset from the planet center. Each
  keeps the existing docking + trade behaviour. Each has its own static price for
  now (price trends are step 2).
- **Arrangement:** a triangle with legs ~3000–4000 units. Distances tunable.
- This replaces the Rung 1 two-station spawn. Docking and trade keep working
  unchanged against the three stations.

## Navigation markers

- One `TargetMarker` per station, always on, showing the station name and the
  player's distance to it, with an edge arrow when the station is off-screen
  (current behaviour).
- **Focused state:** when the ship's nose points within an aim cone of a station,
  that station's marker switches to a focused look (bold font + color change).
  Exactly one station is focused at a time — the one the ship is aiming nearest.
  If no station is within the cone, none is focused.

## Cruise/jump drive

In-system travel boost. Not the Rung 4 inter-system hyperdrive.

**States:** Normal, Jumping.

**Flow:**
1. **Focus:** aim at a station so its marker focuses. A prompt appears:
   `J — Jump to [Station]`.
2. **Engage (press J):** the ship locks its heading toward the focused station at
   that instant (a fixed straight line, set once — not live autopilot steering)
   and accelerates to a high cruise speed (tunable `X` multiplier). The player
   cannot steer while jumping.
3. **Drop out:**
   - **Auto:** on reaching a realistic range of the target station, the drive
     cuts out and the ship returns to normal speed, dropped close enough to fly
     in and dock manually.
   - **Manual:** during jump the prompt reads `J — Disengage`; pressing it drops
     the ship out of jump immediately at its current position.

**Rules and forgiveness:**
- Cannot engage while docked.
- The aim cone and auto-cutout range are generous, so a reasonable aim lands the
  ship in range. Heading is locked toward the focused station at engage time, so
  the ship reliably arrives if a station was focused.

## Code architecture

Follows the existing code-spawn pattern (primitives built at play time; in-Editor
placement is unreliable in this project).

- **`Planet`** (new MonoBehaviour): builds a procedural sphere, tinted, no
  collider. Backdrop only. Mirrors `Station`'s build-from-primitives approach.
- **`StationField` → `SystemField`** (rename/extend): spawns three planets and
  three stations in the triangle and attaches one `TargetMarker` per station.
  Replaces the current two-station spawn. Holds the tunables (positions, colors,
  prices, separation).
- **`TargetMarker`** (extend): add a focused visual state (bold + color change)
  toggled by the targeting component.
- **`NavTargeting`** (new, on the Ship): each frame, finds the station with the
  smallest angle between the ship's forward vector and the direction to the
  station, within the aim cone; marks that station's marker focused and exposes it
  as the current focused target. Clears focus when none is within the cone.
- **`JumpDrive`** (new, on the Ship): owns the Normal/Jumping state machine, reads
  the focused target from `NavTargeting`, handles the J keypress to engage and
  disengage, suspends player steering and drives the ship straight at boost speed
  during jump, auto-cuts-out at range, and draws the on-screen prompts via OnGUI
  (consistent with the existing placeholder UI). Will not engage while
  `DockingController` reports docked.
- **`ShipController`** (small change): expose a hook so `JumpDrive` can suspend
  player steering during jump, mirroring how docking already disables control.

Input uses the new Input System (`Keyboard.current`), matching the rest of the
project.

## Out of scope (parked)

- Market price trends (Rung 2, step 2).
- Cargo mission type (Rung 2, step 3).
- Rung 4 hyperdrive / jump to a second system.
- Landable or detailed planets.
- Station colliders / solid bodies (carried-over known limitation).
- Moving or orbiting stations.

## Feel-work (judged by play-testing, not code review)

- Jump speed multiplier, aim-cone width, and auto-cutout range: defaults set in
  code, tuned by the director.
- Whether being a passenger mid-jump (no steering) feels good over a 3000–4000u
  leg, or whether the leg distances need adjusting.
- Whether three always-on markers stay readable, or need decluttering.

## Acceptance for this step

- Three planets visible as backdrops, each with one station near it.
- Markers show all three stations with name + distance; the aimed-at station
  focuses (bold + color).
- Aiming at a station shows the Jump prompt; pressing J jumps to it at boosted
  speed with no steering; the drive auto-cuts-out at range; Disengage drops out
  early.
- Docking and trade still work at all three stations.
- A full trade loop across the bigger system can be flown without errors.

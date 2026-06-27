# Flight design — Rung 1, step 1

**Date:** 2026-06-27
**Status:** Approved by director, ready for implementation plan
**Scope:** Rung 1, mechanic 1 (Flight) only. See CLAUDE.md sections 5 and 6.

## Goal

Make a single ship that moves and turns in 3D space and *feels good to fly*. This is feel work, judged by the director playing, not by code review. Nothing else (stations, docking, trade, starfield) is in scope here.

## Flight model: Hybrid (momentum + assist)

The ship has mass and momentum. Thrust builds up velocity; releasing thrust does not instantly stop the ship. A built-in "flight assist" continuously dampens unwanted drift and residual rotation so the ship stays controllable and does not spin out. The director feels weight but keeps authority over the ship.

All key feel values are exposed as tunable fields in the Inspector so the director and Claude can adjust by playing:
- Thrust power (acceleration)
- Max speed
- Turn rate (pitch / yaw responsiveness)
- Roll rate
- Flight-assist damping strength (how aggressively drift and spin bleed off)
- Boost multiplier / brake strength

## Controls (mouse + keyboard)

- **Mouse** — aims the nose. Mouse X = yaw, Mouse Y = pitch.
- **W / S** — throttle forward / reverse.
- **A / D** — roll (bank left / right).
- **Left Shift** — boost (temporary speed increase).
- **Left Ctrl** — brake (extra damping / slow down).
- **No strafing** in this version. Lateral/vertical thrusters are deferred; add later only if docking needs them.

## Camera: chase cam

External camera floating behind and slightly above the ship. Follows with a small amount of smoothing/lag so fast turns feel dynamic rather than rigid. Looks at the ship (or just ahead of it). This is the flight camera only; it is not the out-of-scope "walk around" third person from CLAUDE.md.

## Placeholder ship

A simple elongated shape with a clearly readable "nose" (a stretched block with a smaller nose marker, or a capsule/cone body) so facing direction is always obvious. Pure placeholder, replaced by real art in a later rung. Reuse or replace the existing test cube.

## Test scene

A minimal scene: the ship floating in empty space, free to fly around. No stations, no starfield yet. Success = the director plays it and judges turning, accelerating, boosting, and stopping as good and responsive.

## Out of scope (parked)

Stations, docking, trade loop, UI, starfield/skybox, strafing, multiple ships. These come in later steps once flight feels right.

## Acceptance

- [ ] Ship moves and turns in 3D under mouse + keyboard control.
- [ ] Momentum + flight assist feel present and controllable (director's call).
- [ ] Boost and brake work.
- [ ] Chase cam follows smoothly and reads well.
- [ ] Feel values are tunable in the Inspector without code changes.
- [ ] Director judges the flight as good and responsive.

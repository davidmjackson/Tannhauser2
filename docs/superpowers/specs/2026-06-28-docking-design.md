# Docking — design spec (Rung 1, step 3)

**Date:** 2026-06-28
**Status:** Approved, ready to plan
**Scope:** Rung 1, mechanic 2 ("Docking") from CLAUDE.md section 6.

## Goal

Fly up to a station, deliberately dock, "arrive" in a clear frozen state, and
undock to fly off again. This is feel-work: the director judges it by playing.
It produces the docked state that the later trade step builds on. No trade,
credits, or cargo here.

## Locked decisions

- **Trigger:** get close, press a key. Fly into a docking zone around the
  station's dock point; a `Press F to dock` prompt appears; press **F** to dock.
- **On dock:** freeze and hold. Ship halts, flight controls disable, a
  `DOCKED — <station>` banner shows. No snap-to-point, no camera change.
- **Undock:** same key toggles. Press **F** again to release and fly off.

## Component: `DockingController`

Lives on the ship GameObject alongside `ShipController` and the ship Rigidbody.
Owns the entire dock/undock state machine. Two states: **Flying** and **Docked**.

### Flying state
- Each frame, find the nearest registered `Station` whose `DockPoint` is within
  `dockRange` of the ship.
- If one is in range: show the `Press F to dock` prompt (OnGUI, bottom-center).
- On **F**: enter Docked state, bound to that station.

### Docked state
- Ship is frozen (see below). Show `DOCKED — <displayName>  (F to undock)`
  banner (OnGUI, top-center).
- On **F** (after cooldown): return to Flying state.

### Freeze mechanism
- **On dock:** set the Rigidbody `isKinematic = true` (kills all linear and
  angular motion instantly, blocks force leak) and set `ShipController.enabled =
  false` (stops input-driven movement).
- **On undock:** set `isKinematic = false` and `ShipController.enabled = true`.
- A ~0.5s cooldown after each toggle prevents an accidental instant
  dock→undock from a single held keypress.

### Station discovery
- Each `Station` adds itself to a static list (e.g. `Station.All`) when it
  initializes and removes itself if destroyed.
- `DockingController` scans that list each frame against the ship position.
  Works for both A and B with no hardcoding, and avoids per-frame
  `FindObjectsByType` scans.
- Range is measured from the ship to `Station.DockPoint` (the existing 40-unit
  front offset). Starting `dockRange = 80` units. Tunable; expect play-testing.

## UI

- Drawn with `OnGUI`, matching the existing `TargetMarker` style (no Canvas).
- Prompt: bottom-center box, e.g. `Press F to dock`.
- Banner: top-center box, e.g. `DOCKED — Station A   (F to undock)`.

## Wiring

- `DockingController` is attached to the ship GameObject (the one carrying
  `ShipController`). It auto-resolves its Rigidbody and `ShipController` via
  `GetComponent` in `Awake`, so no inspector references are required.
- `Station.Initialize` is the natural place to register into `Station.All`.

## Known limitation (accepted)

- Stations have **no solid colliders**. The ship can pass through the station
  body. Accepted for this step: docking freezes you on approach before it
  matters, and adding solid physics colliders to a fast ship risks bounce/stuck
  bugs not worth chasing now. Solid hulls stay parked.

## Out of scope (parked)

- Buy/sell trade panel, credits, cargo (the next step).
- Snap-to-dock-point motion and docked camera framing (possible later polish).
- Speed/alignment-gated docking and per-station docking procedures (Rung 3).

## Acceptance (for this step)

- Flying within range of either station shows the dock prompt.
- Pressing F docks: ship freezes, banner shows the correct station name.
- Pressing F again undocks: ship flies normally again.
- Works at both Station A and Station B.
- No console errors across a dock/undock cycle at each station.
- Final feel (range, prompt clarity) is the director's call after play-testing.

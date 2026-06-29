# Tannhauser — Game Vision (North Star)

**Date:** 2026-06-29
**Type:** Vision / north-star document, NOT an implementation spec
**Status:** Approved by the director

## Purpose

This is the agreed picture of the finished game. It exists to steer every rung
of the build ladder, not to be built from directly. We still build one rung at a
time (see CLAUDE.md section 5); this document keeps those rungs pointed the right
way. It expands the short vision in CLAUDE.md section 1 into the director's full
intent.

Nothing here changes current scope. When a feature below is ready to build, it
gets its own spec and plan.

## 1. The world

A single star system at roughly **10x the scale of Sol**, built for a genuine
sense of distance.

- Planets sit at varied ranges from the star: some far out (a crossing takes
  minutes even under jump), some in closer, safe orbits.
- **Gas giants have ring systems.** Stations sit *inside* the rings, in safe
  voids among the ice particles.
- **Moons carry surface bases** with landing pads you set down on.
- Some planets host **multiple bases** spread across orbit and surface.
- Late game, the hyperdrive opens **many more systems** beyond this one.

## 2. Travel (three tiers)

- **Normal flight** — moment-to-moment piloting near bases and in combat.
- **In-system jump (cruise drive)** — fast travel between planets within a
  system. Essential because of the scale; far legs still take minutes even
  jumping. (This is what Rung 2 prototypes.)
- **Inter-system hyperdrive** — the late-game unlock that jumps to a *new system*.
  Distinct from the in-system jump. (Rung 4.)

## 3. Arrival and docking

Arriving is a **skill moment that varies by location**, not a menu:

- **Orbital stations** — a clean approach and dock.
- **Ring-system stations** — thread through the ice field to a safe void.
- **Surface pads** (moons/planets) — descend and set down on a pad.

Each location is its own small piloting challenge with its own procedure. Arrival
is part of the fun.

The **ship handles differently depending on mode**: normal flight is responsive
and fast; an approach/docking mode is slower and more precise for threading rings
and setting down on pads. Switching between flight and docking behaviour is part
of the skill-based docking feature (designed when that feature is built, light in
Rung 2, full in Rung 3).

## 4. Activities — a balanced sandbox

Trade, missions, and combat are **all first-class**. The player chooses their
path: pure trader, mercenary, or a blend, and the game supports each.

- Bases offer cargo contracts, combat jobs, assassination, escort, and similar.
- A **living market**: prices shift with supply, demand, and the player's own
  activity (trends up and down).

## 5. Combat

**Arcade dogfighting** — fast, reflex-driven ship-to-ship fights with lasers and
missiles against pirates and mission targets. Flying skill wins fights.

## 6. Ships and economy

**Buy whole hulls and deeply upgrade them.**

- A ladder of ships: starter trader → larger freighters, fast fighters,
  multi-role craft. You save up and *buy* a hull for a role.
- Each hull takes upgrade **modules**: engines, weapons, shields, cargo hold,
  in-system jump range, and eventually the inter-system hyperdrive.
- Credits flow in from trade, missions, and bounties; they sink into ships,
  modules, fuel, and repairs.

## 7. Progression and endgame

From a broke starter pilot to a galaxy-hopping operator:

1. Fly and earn (trade / missions / combat).
2. Upgrade and specialize your ship.
3. Afford the **hyperdrive**.
4. Make the **first inter-system jump** — a major milestone.
5. Explore an **open galaxy** of new systems, with **exploration data** as
   ongoing income.

Open-ended; no hard finish line. Emergent, sandbox-style progression (no scripted
campaign assumed).

## 8. Scope honesty: vision vs. the build ladder

This vision is **large** — realistically a multi-year, multi-project ambition.
The existing 4-rung ladder still holds and reaches **"v1 = the first
inter-system jump."** Mapping the vision onto the ladder:

- **Rung 1 (done):** fly, dock, trade between two stations. Windows build.
- **Rung 2 (in progress):** three planets with orbital stations, the in-system
  jump (cruise drive), market price trends, the first cargo mission type.
- **Rung 3:** arcade combat and shooter missions, varied skill-based docking,
  ship buying and upgrades.
- **Rung 4 (v1 done):** the hyperdrive and the jump to a second system.
- **Beyond v1 (new territory, not yet laddered):** the open galaxy of many
  systems; surface/moon landings on pads; ring-system stations; the full living
  economy; the multi-role ship roster; exploration-data income; assassination/
  escort mission variety. These are the long-term north star past v1.

The discipline stays the same: do not start a rung until the one below feels
good (CLAUDE.md section 5). This document is the destination, not the next step.

## What this means for right now

We return to **Rung 2, step 1** (planets + orbital stations + in-system jump),
already in progress on branch `rung2-planets-stations`. The vision confirms that
step's direction: the in-system jump is core, and the placeholder planet
distances will later grow toward the realistic 10x-Sol scale once the jump makes
that scale playable. Realistic scale is a tuning/late concern, not part of step 1.

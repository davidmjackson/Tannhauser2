# Docked Console + Cockpit View (parked vision)

Date: 2026-06-29
Status: PARKED — captured so it is not lost; scheduled for Rung 3, not built now
Rung: Rung 3 (Toward MVP)

## What this is

A director idea, recorded during Step 2c brainstorming, for how the ship's UI
should eventually feel. It is **not** being built yet: almost every panel it needs
is a Rung 3 mechanic (combat damage, fuel, shields, ship upgrades) that does not
exist. Building the frame now would mean wrapping mostly empty panels. So it is
parked here and added to the roadmap as a Rung 3 line.

## The vision

**On dock, the view transitions** from the third-person "behind the ship" camera
to a **console screen**, the ship's operations terminal. The console shows:

- **News feed** (built first, in Step 2c, as a standalone unit that drops in here).
- **Cargo manifest:** load on ship, price paid for each cargo, and the profit if
  sold at the current station.
- **Damage register + repair:** current damage, repair options, and repair cost.
- **Fuel:** fuel on ship and a refuel option.
- **Shields:** shield health.
- **Ship blueprint:** a top-down "blueprint" of the ship with **slots** (initially
  empty) where the player buys parts and installs them into empty slots.

**In flight, a cockpit view** where the player flies from inside the ship and sees
dashboards, controls, and screens (including a news feed), instead of (or
alongside) the current chase camera.

## Why it is Rung 3, not now

- The console's value comes from panels that are all Rung 3 systems: **repair**
  needs a damage/combat model, **refuel** needs a fuel mechanic (not yet on the
  roadmap), **shields** need combat, and the **blueprint slots** are ship
  upgrades + purchasing (explicitly parked in CLAUDE.md §9).
- Fuel, damage, and shields are **new mechanics**, not just UI — each needs its
  own design pass. The console can't show a repair cost until "damage" means
  something.
- The dock camera transition and the cockpit both touch flight/docking **feel**,
  which is heavy play-test work, not a quick add.
- Pulling it forward would break the build-ladder discipline (CLAUDE.md §5).

## How Step 2c respects it

Step 2c's news is built as a self-contained feed (`MarketDirector` owns the
headlines; the UI just reads and renders them). When the Rung 3 console arrives,
the news becomes one panel of it for nearly free. Nothing built now is wasted.

## When to revisit

At the start of Rung 3, alongside combat, ship buying, and upgrades. At that
point, design fuel/damage/shields as mechanics first, then build the console as
their shared hub, then consider the cockpit flight view as its own feel project.

# Tannhauser — Build Roadmap

A living, high-level checklist of what gets built under each rung, so progress is
easy to see at a glance. Details for each item live in its own spec/plan under
`docs/superpowers/`. The destination behind all of it is the north-star vision:
`docs/superpowers/specs/2026-06-29-game-vision-northstar.md`.

**Discipline:** build top-down, one rung at a time. Do not start a rung until the
one below feels good and passes its gate (CLAUDE.md section 5).

**Legend:** [x] done · [~] in progress · [ ] not started

---

## Rung 1 — Vertical slice  ✅ COMPLETE

Fly between two stations, dock, and trade for profit.

- [x] Two stations placed in space + on-screen target marker
- [x] Flight (move, turn, feel good to fly)
- [x] Docking (approach, dock, undock)
- [x] Trade loop (buy at A, sell at B, credits go up) + minimal UI
- [x] Runs as a standalone Windows build + Esc-to-quit

## Rung 2 — POC (Proof of Concept)  ⏳ IN PROGRESS

A small three-planet system with fast travel, a moving market, and a first job.

**Step 1 — Planets, orbital stations, in-system jump**  [~]
- [x] Three-planet layout (3 planets, one station each, a marker on each)
- [x] Aim-to-focus markers (marker highlights the station you point at)
- [x] In-system cruise/jump drive (aim, jump, auto-cutout or disengage)
- [x] Acceptance play-test + distance/jump feel tuning (all tests pass; distances and jump feel approved)

**Jump-drive polish (deferred refinements from the step-1 play-test):**
- [ ] Jump engine charge-up: a couple-second wind-up before the jump engages (functional timing now-able; the power-cell charging animation is art, later)
- [ ] Light-speed visual effect during jump (stretched starlight) — art, later
- [ ] Stop the jump passing straight through a planet when a station sits behind one (behavior fix)

**Step 2 — Market price trends**  [~]
- [ ] Prices drift up and down over time (a market that moves): per-station wave
      + light noise, a buy/sell spread to force travel, docked-only trend UI.
      Single commodity. (Spec: `docs/superpowers/specs/2026-06-29-rung2-step2-market-price-trends-design.md`)

**Step 2b — Multiple commodities**  [ ]
- [ ] Several goods (ore, medicine, etc.) with a multi-good cargo hold and trade
      UI, per-commodity prices. Lands before news so news can name specific goods.

**Step 2c — Financial news + event shocks**  [ ]
- [ ] A periodic news feed whose articles hint at coming price peaks/troughs,
      plus one-off "mega" price shocks layered on top of the natural waves.

**Step 3 — First cargo mission**  [ ]
- [ ] Accept a delivery job at one station, fulfil it at another for a reward

## Rung 3 — Toward MVP  [ ]

The meaty systems. Each is heavy feel-work, prototyped then judged by playing.

- [ ] Arcade dogfighting combat + a shooter mission type
- [ ] Varied skill-based docking (orbital approach / thread the rings / surface pad landing)
- [ ] Distinct flight vs. docking handling modes (the "ship behaves differently" idea)
- [ ] Ship buying (a ladder of hulls for different roles)
- [ ] Ship upgrades (modules: engines, weapons, shields, cargo, jump range)

## Rung 4 — v1 (the natural "done" line)  [ ]

- [ ] Inter-system hyperdrive (distinct from the in-system jump)
- [ ] The jump to a second star system  →  **v1 complete**

## Beyond v1 — north-star territory (not yet broken into steps)

The long-term vision past v1. Captured so it is not lost; not scheduled yet.

- [ ] Open galaxy of many systems to explore
- [ ] Surface and moon landings on pads
- [ ] Ring-system stations tucked in safe voids among the ice
- [ ] Full living economy (supply, demand, player impact)
- [ ] Multi-role ship roster
- [ ] Exploration-data income
- [ ] Assassination / escort / wider mission variety
- [ ] Real 3D art pass (replace placeholder blocks with models)

---

## Art note (when models come in)

Greybox (placeholder blocks) through Rung 2. Bring in real models around Rung 3,
when combat and docking make looks matter. Final / AI-generated "hero" art waits
until the mechanics are locked. A free placeholder ship model can be dropped in
any time, cheaply, if a less-boxy look is wanted sooner.

## Where we are right now

Rung 2, Step 1 is complete (all four tasks done, acceptance passed) and merged.
Rung 2, Step 2 (market price trends) is in progress: design approved, spec
written, implementation plan next. During its brainstorming the news idea was
captured and re-sequenced as Steps 2b (multiple commodities) and 2c (financial
news + event shocks). A few jump-drive polish items remain parked (see the step-1
polish list).

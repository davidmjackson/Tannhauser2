# Trade loop design (Rung 1, step 3 + minimal UI)

Date: 2026-06-29
Status: approved, ready for implementation plan

## Goal

Close the Rung 1 core loop: docked at Station A, buy cargo cheap; fly to Station B,
sell it dear; watch credits rise. This is the step that makes the game a *game*.
Folds in the minimal buy/sell UI (CLAUDE.md step 4), because trade is untestable
without a clickable face.

## Scope decisions (locked during brainstorming)

- **One cargo type.** Multiple cargo types are parked.
- **One price per station.** Each station has a single `unitPrice`. Buy and sell both
  happen at the docked station's price. A is cheap, B is dear, so the player learns
  "buy low, sell high" naturally. Prices are fixed (market trends are parked).
- **UI is a functional placeholder, not real game UI.** Match docking's approach:
  Unity `OnGUI`, no Canvas, no art, no layout polish. We agreed *behaviour*, not looks.
  A proper HUD pass belongs to a later rung.

## Components

### TradeController (new) — on the Ship GameObject

Owns the player economy. Plain instance fields (survive a mid-play recompile; only
static lists get wiped by domain reload, which does not apply here).

- `credits` (int), starts at **1000**
- `cargoUnits` (int), starts at **0**
- `cargoCapacity` (int) = **10**

Behaviour:
- Reads docked state from `DockingController.DockedStation` each frame.
- `Buy()`: guard `credits >= price && cargoUnits < cargoCapacity`, then
  `credits -= price; cargoUnits += 1`.
- `Sell()`: guard `cargoUnits > 0`, then `credits += price; cargoUnits -= 1`.
- Owns the OnGUI: always-on corner readout, plus the docked panel.

### Station (existing) — add pricing

- New field `unitPrice` (int).
- Set via `Initialize(...)` or directly in `StationField`.

### StationField (existing) — set the two prices

- Station A: `unitPrice = 50` (cheap, buy here).
- Station B: `unitPrice = 80` (dear, sell here).

### DockingController (existing) — expose docked station

- New read-only property `public Station DockedStation => docked ? dockedAt : null;`.
- One-way dependency: docking owns movement state, trade reads it. No tangle.

## Data flow

```
StationField --sets--> Station.unitPrice
DockingController --exposes--> DockedStation (Station or null)
TradeController --reads--> DockedStation, Station.unitPrice
TradeController --mutates--> credits, cargoUnits
TradeController.OnGUI --draws--> corner readout + docked buy/sell panel
```

## UI behaviour (OnGUI placeholder)

- **Always on screen:** top-left corner readout.
  - `Credits: <n>`
  - `Cargo: <cargoUnits>/<cargoCapacity>`
- **When docked** (`DockedStation != null`): a small panel under the existing
  `DOCKED` banner.
  - Line: `Cargo price: <unitPrice> cr/unit`
  - Button **[Buy]** — enabled only when affordable and hold not full.
  - Button **[Sell]** — enabled only when holding cargo.
  - Disabled buttons grey out (use `GUI.enabled`).
- One unit per click, so the player watches numbers move. A "buy/sell max" button
  is a trivial later add if click-by-click feels tedious in play.

## Economy numbers (first pass, tune by feel)

- Start credits: 1000
- Hold capacity: 10
- Price A: 50, Price B: 80 → margin 30/unit → +300 profit per full round trip.
- A full load at A costs 500, well within starting credits.

## Excluded (parked)

Multiple cargo types, price trends/movement, buy-max convenience, any real UI art,
per-station varied procedures.

## Acceptance (manual play-test)

- Dock at A, click Buy until the hold fills; credits drop by 50 each click,
  cargo rises; Buy greys out at 10 units or when broke.
- Fly to B, dock, click Sell; credits rise by 80 each click, cargo falls;
  Sell greys out at 0 cargo.
- A full loop takes credits from 1000 toward 1300 with no errors.
- Corner readout updates live throughout.

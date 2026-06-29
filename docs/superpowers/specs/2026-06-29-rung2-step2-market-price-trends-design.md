# Rung 2, Step 2 — Market Price Trends (design)

Date: 2026-06-29
Status: approved, ready for implementation plan
Rung/step: Rung 2 (POC), Step 2

## Goal

Make station prices move over time so trading involves **timing decisions**, not
just flying A-to-B. The player should be able to watch a docked station's price
rise and fall and decide whether to transact now, loiter for a better price, or
leave and come back.

This is the natural-waves foundation only. Event-driven news and one-off price
shocks are explicitly deferred (see Scope and sequencing).

## Decisions locked during brainstorming

- **Purpose:** create timing decisions (prices must be visibly moving and
  somewhat readable, not just background flavour).
- **Price visibility:** docked station only. No live market board for other
  stations. (The future news system will be how you learn about other stations.)
- **Movement model:** waves + noise. A slow learnable wave per station plus light
  random jitter so it is not perfectly clockwork.
- **Anti-scalping:** a buy/sell spread. Each station pays less to buy cargo from
  you than it charges to sell cargo to you, so round-tripping at one station loses
  the spread and travel is always required for profit.
- **Commodity count:** single commodity for this step. Multiple commodities are
  deferred to a later step (they are needed by the news system).

## Scope and sequencing

This step is single-commodity natural waves. The richer ideas raised during
brainstorming are captured and re-sequenced as their own later steps:

- **Step 2 (this step):** market price trends — natural waves + spread + docked
  trend UI, single commodity.
- **Step 2b:** multiple commodities (cargo model + multi-good trade UI,
  per-commodity prices). Needed before news so news can name specific goods.
- **Step 2c:** financial news feed + one-off event price shocks, layered on top
  of the wave foundation. News hints at where prices will peak and trough.
- **Step 3 (unchanged):** first cargo mission.

The news/shock layer is purely additive on top of the wave model, so building
waves first wastes no work.

## Mechanics (what the player experiences)

- Every station has a baseline mid-price. The current fixed prices
  (Helios 50, Verdant 65, Cobalt 80) become these baselines.
- The mid-price is always moving: a slow wave plus light noise, each station on
  its own rhythm so the three markets are out of step. Prices move whether or not
  the player is docked, so "come back in a bit" is a real option.
- At any moment a station exposes two prices derived from its mid:
  - **Buy price** (what the player pays to take cargo) = mid + spread/2
  - **Sell price** (what the station pays the player) = mid − spread/2
- A trend cue (▲ rising / ▼ falling / — flat) shows the recent direction of the
  mid-price.
- The decision loop while docked: see both prices and the trend, then transact
  now, loiter a few seconds for a better price, or leave.

## Architecture

Small, single-purpose units, matching the existing script style.

### 1. `PriceCurve` (new — plain C# class, not a MonoBehaviour)

Encapsulates the market math for one station and nothing else. Pure function of
time `t`: same `t` in, same prices out. No per-frame state.

- Fields: `basePrice`, wave `amplitude`, `period`, `phase`; noise `noiseAmplitude`,
  `noiseScale`, `seed`; `spread`; `priceFloor`.
- `Mid(t)` = `basePrice` + wave term (`Mathf.Sin` over `period`/`phase`) + coherent
  noise term (`Mathf.PerlinNoise(seed, t * noiseScale)` recentred to roughly
  ±noiseAmplitude). Coherent noise, never per-frame `Random`.
- `SellPriceToPlayer(t)` = `Mid(t)` + `spread`/2, clamped to `priceFloor`.
  (Price the player pays to buy cargo.)
- `BuyPriceFromPlayer(t)` = `Mid(t)` − `spread`/2, clamped to `priceFloor`.
  (Price the station pays when the player sells.)
- `Trend(t)` = sign of `Mid(t)` − `Mid(t − lookback)` with a small deadband →
  +1 / 0 / −1 (lookback ~1.5s).

Naming note: the player-facing labels are "Buy" (player buys) and "Sell" (player
sells). The method names above are spelled to avoid that ambiguity. Final method
names can be finalised in the plan; the meaning is fixed here.

### 2. `Station` (changed)

- Remove the single `int unitPrice`.
- Add a `PriceCurve` instance plus convenience properties that evaluate it at the
  current market time: `SellPrice` (player pays), `BuyPrice` (player receives),
  `PriceTrend`.
- Each station derives a distinct `phase` / `period` / `seed` from its spawn index
  so the markets move out of step.
- `Initialize(...)` takes a `basePrice` in place of `unitPrice`.

### 3. `StationField` (changed)

- Pass `basePrice` (the old 50/65/80) instead of `unitPrice`.
- Set shared market defaults and the per-station phase offset. Starting values
  (all feel-work, tuned in play-test):
  - `amplitude` ≈ 20% of base
  - `period` 20–40s, varied per station
  - `noiseAmplitude` ≈ 5% of base
  - `spread` ≈ 15–20% of base (kept smaller than the inter-station price gap so
    travel always pays)
  - `priceFloor` a small minimum so prices never hit zero or go negative.

### 4. `TradeController` (changed)

- `Buy` uses `Station.SellPrice` (player pays); `CanBuy` checks
  `credits >= SellPrice && cargoUnits < cargoCapacity`.
- `Sell` uses `Station.BuyPrice` (player receives); `CanSell` checks
  `cargoUnits > 0`.
- Docked panel: show a market trend cue plus two prices — "Buy @ X" and
  "Sell @ Y" — refreshed live each `OnGUI` frame so the player watches them tick.
- The always-on corner credits/cargo readout is unchanged.

### Time source

`Time.timeSinceLevelLoad`. It always advances during play and resets per session,
which is acceptable (no save system yet).

## Balance sanity check

With baselines 50 / 65 / 80, spread ~15% and amplitude ~20%:

- Buy cheap at Helios ≈ 50 + spread/2 (~57); sell dear at Cobalt ≈ 80 − spread/2
  (~73). Baseline profit ~16/unit plus timing swings (±~10–16). Travel pays.
- Same-station round trip: buy ~57, sell ~43 → loses the spread (~14). Scalping
  one station is structurally unprofitable, as intended.

## Testing

Project convention is play-testing (no automated test suite). Exception proposed:
`PriceCurve` is a pure deterministic function, so one small EditMode unit test is
cheap and worth it — assert spread ordering (`SellPrice > BuyPrice`), floor clamp,
and trend sign. If the director prefers play-test-only, drop the test; nothing
else depends on it.

## Acceptance tests (play-test)

- [ ] Prices visibly move while docked; the trend cue matches the movement.
- [ ] Buy price > sell price at the same station (the spread is visible).
- [ ] A full profitable loop still works: buy at a cheap station, fly, sell at a
      dear station, credits increase.
- [ ] Timing pays: waiting for a dip to buy / a peak to sell beats transacting
      blind.
- [ ] No console errors across the three-planet system.
- [ ] Feel tuning (period, amplitude, spread, noise) approved by the director
      after play-testing.

## Out of scope (deferred)

Multiple commodities, financial news feed, one-off event price shocks, any live
market board for non-docked stations, price persistence/saving.

# Rung 2, Step 2b — Multiple Commodities (design)

Date: 2026-06-29
Status: approved, ready for implementation plan
Rung/step: Rung 2 (POC), Step 2b

## Goal

Add three tradeable goods, each produced by one station, so trading becomes
about **routes** ("buy Fuel cheap at Helios, sell it dear at Cobalt") on top of
the timing layer from Step 2. This also sets up Step 2c (financial news), which
needs named goods to talk about.

## Decisions locked during brainstorming

- **Economy shape:** stations specialize. Each station's home good is cheap
  there (good to buy); the two imported goods are dear there (good to sell).
- **Commodity count:** three, one produced per station.
  - **Fuel** produced at **Helios**
  - **Grain** produced at **Verdant**
  - **Electronics** produced at **Cobalt**
- **Cargo hold:** one shared capacity (10 units), tracked per good, all goods
  competing for the same space.
- **Buy/sell amount:** one unit per click, one Buy/Sell pair per good (consistent
  with Step 2).
- **Prices still move:** every (station, good) pair gets its own moving market
  (the Step 2 `PriceCurve`, reused unchanged), so timing matters on top of routes.

## Scope and sequencing

- **This step (2b):** three goods, station specialization, shared hold, per-good
  moving prices and trade UI.
- **Next (2c):** financial news feed + one-off event price shocks, which can now
  name specific goods (e.g. "ore strike at Helios").
- **Then Step 3:** first cargo mission.

## Mechanics (what the player experiences)

- Three goods: Fuel, Grain, Electronics.
- At each station, its home good has a **low base price** (cheap to buy) and the
  two imported goods have a **high base price** (dear, good to sell). This makes
  three natural routes:
  - Buy Fuel at Helios, sell at Verdant or Cobalt.
  - Buy Grain at Verdant, sell at Helios or Cobalt.
  - Buy Electronics at Cobalt, sell at Helios or Verdant.
- Every (station, good) market still moves (wave + noise) and keeps a buy/sell
  spread, so a same-station round trip still loses the spread, and timing a dip
  to buy / a peak to sell still pays.
- The hold has one shared capacity of 10 units. The player chooses the mix
  (10 Fuel, or 5 Fuel + 5 Grain, etc.).
- Docked panel shows one row per good: name (with a "produced here" tag on the
  home good), live Buy and Sell price, a trend cue, held count, and a one-unit
  Buy / Sell button pair. The corner readout shows credits and a compact per-good
  cargo line.

## Architecture

Small, single-purpose units. `PriceCurve` from Step 2 is reused unchanged.

### 1. `Commodity.cs` (new)

```
public enum Commodity { Fuel, Grain, Electronics }

public static class Commodities
{
    public static readonly Commodity[] All; // { Fuel, Grain, Electronics }
    public static string DisplayName(Commodity c); // "Fuel" / "Grain" / "Electronics"
}
```

One job: name and enumerate the goods. No Unity dependency.

### 2. `Station.cs` (changed)

- Replace the single `PriceCurve market` with a per-commodity set:
  `Dictionary<Commodity, PriceCurve> markets`.
- Add `Commodity homeCommodity`.
- API becomes per-good (evaluated at the current market time):
  - `int SellPrice(Commodity c)` (player pays)
  - `int BuyPrice(Commodity c)` (station pays the player)
  - `int PriceTrend(Commodity c)` (+1 / 0 / -1)
  - `bool Produces(Commodity c)` (c == homeCommodity)
- `Initialize(...)` takes `homeCommodity` and the built `markets` dictionary.

### 3. `StationField.cs` (changed)

- Each station def maps to a home good: Helios→Fuel, Verdant→Grain,
  Cobalt→Electronics.
- Replace the single per-station base price with two shared tunable prices:
  - `homeBasePrice` (low, default 40)
  - `foreignBasePrice` (high, default 90)
- Keep the Step 2 market fractions: `amplitudeFraction`, `noiseFraction`,
  `spreadFraction`, `periodMin`, `periodMax`, `noiseScale`, `priceFloor`.
- For each station, build 3 `PriceCurve`s (one per good): base price is
  `homeBasePrice` for the home good and `foreignBasePrice` for the others, with
  amplitude/noise/spread derived from that base by the existing fractions. Give
  each (station, good) a distinct phase/period/seed (from a per-pair lane index,
  0..8) so all nine markets drift out of step.

### 4. `TradeController.cs` (changed)

- Replace the single `cargoUnits` with per-good counts
  (`Dictionary<Commodity, int> hold`), keeping `cargoCapacity` (10) as a shared
  total. Expose `TotalUnits` (sum of held).
- Buy/sell become per-good:
  - `CanBuy(s, c)` = credits >= `s.SellPrice(c)` && `TotalUnits < cargoCapacity`
  - `CanSell(s, c)` = `hold[c] > 0`
  - `Buy(s, c)`: credits -= `s.SellPrice(c)`; `hold[c]++`
  - `Sell(s, c)`: credits += `s.BuyPrice(c)`; `hold[c]--`
- OnGUI:
  - Corner readout: credits and a compact per-good cargo line plus total n/10.
  - Docked panel: a header with the station name, then one row per good
    (`Commodities.All`): name (+ "produced here" if `s.Produces(c)`), Buy price,
    Sell price, trend cue (`^` / `v` / `-`), held count, and one-unit Buy / Sell
    buttons (greyed when not affordable / hold full / none held). Keep the
    existing high-res scaling.

### Time source

`Time.timeSinceLevelLoad`, as in Step 2.

## Balance sanity check

Home base 40, foreign base 90, spread 18%, amplitude 20%:

- Buy home good at home ~ 40 + spread/2 (~44). Sell it where it is foreign
  ~ 90 - spread/2 (~82). Baseline profit ~38/unit before timing swings. Travel
  pays strongly.
- Same-station round trip on any good loses that good's spread, as intended.
- Amplitude (home ~8, foreign ~18) never lets a home-good peak exceed a
  foreign-good trough, so routes stay profitable even on poor timing; good timing
  adds upside.

## Testing

Play-testing (project convention; no automated suite). `PriceCurve` is unchanged
and stays pure/test-ready.

## Acceptance tests (play-test)

- [ ] Each station's home good is clearly the cheapest place to buy it; the other
      two goods are dear there.
- [ ] A full route profits: buy Fuel cheap at Helios, fly to Cobalt, sell dear,
      credits increase.
- [ ] Shared hold caps at 10 total across mixed goods; cannot overfill.
- [ ] Prices still move per good, trend cues match, and Buy > Sell per good
      (the spread holds).
- [ ] Docked panel shows all three goods with correct prices, trends, and held
      counts; Buy/Sell buttons work and grey out correctly.
- [ ] Corner readout shows credits and per-good cargo correctly.
- [ ] No console errors across the three-planet system.
- [ ] Feel approved by the director (route profitability and price spreads feel
      right) after play-testing.

## Out of scope (deferred)

Financial news and event shocks (Step 2c), cargo missions (Step 3), more than
three goods, buy-max / adjustable-quantity controls, per-good separate capacities,
price persistence/saving, any market board for non-docked stations.

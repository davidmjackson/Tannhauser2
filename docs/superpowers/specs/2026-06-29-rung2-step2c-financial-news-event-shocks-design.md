# Rung 2, Step 2c — Financial News + Event Shocks (design)

Date: 2026-06-29
Status: approved, ready for implementation plan
Rung/step: Rung 2 (POC), Step 2c

## Goal

Add a **news feed** and **one-off price shocks** so the market has legible,
tradeable events on top of the ambient waves from Steps 2/2b. News and shocks are
**one system**: every shock has a headline that appears *before* the shock peaks,
so a sharp player reads the feed, positions cargo, and profits from the swing.

## Decisions locked during brainstorming

- **News heralds shocks (one system).** The natural sine/noise waves stay as
  ambient drift. The legible, tradeable events are one-off shocks, and every shock
  is announced by a headline. News never narrates the everyday waves.
- **Shocks go both directions.** A shock is a signed bump on one (station, good):
  - **Spike up** ("Fuel shortage at Helios") → price rises there → **sell** into it.
  - **Crash down** ("Grain glut at Verdant") → price falls there → **buy** into it.
- **News is always true.** Every headline corresponds to a real shock that will
  happen. No false rumors in this step (parked as a later twist).
- **News shows only when docked**, in the docked panel (the proto-console). No
  always-on flying ticker yet. (See the console vision note, below.)
- **Headlines are explicit.** They name the station, the good, and the direction
  in plain language. The skill is in logistics and timing, not decoding a riddle.
- **Few shocks at once.** Roughly one new shock every ~30–60s, only 1–2 active.
  Exact cadence is tuning.

## The loop (what the player experiences)

1. Dock at a station. The docked panel shows the market **plus a news section**
   listing the currently active headlines across the system.
2. A headline names a coming shock and its direction, e.g. *"Grain glut at
   Verdant"* (a buy tip) or *"Fuel shortage at Helios"* (a sell tip).
3. The shock is a temporary bump on that station's price for that good: it ramps
   up from normal, **peaks**, then decays back to normal. The headline appears at
   the start of the ramp, so there is lead time before the peak.
4. The player flies to the target and acts while the price is abnormal — **sell**
   into a spike, **buy** into a crash — then hauls the cargo on to cash in. The
   affected good's row in the docked market carries a cue (a `!`) so the player can
   match the headline to the price line.

## Architecture

The market today is a **pure function of time** (`PriceCurve.Mid(t)`). We keep
that. A shock is modelled as **another pure time-term added to the mid**, so the
clean, testable, deterministic design is preserved. New work is two small classes
plus light, additive changes to existing ones.

### 1. `MarketShock.cs` (new, pure — no Unity scene dependency)

One value describing a single shock:

```
public class MarketShock
{
    public Commodity commodity;   // which good
    public string headline;       // e.g. "Fuel shortage at Helios"
    public float announceTime;    // t when it appears and the ramp starts
    public float riseDuration;    // ramp time, announce -> peak (the lead time)
    public float decayDuration;   // peak -> back to normal
    public float magnitude;       // signed peak deviation in credits (+spike / -crash)

    public float PeakTime  => announceTime + riseDuration;
    public float EndTime   => PeakTime + decayDuration;
    public bool  IsActive(float t) => t >= announceTime && t < EndTime;

    // Pure pulse: 0 before announce, eases to `magnitude` at peak, eases back to
    // 0 by end, 0 after. Same t in = same value out.
    public float Contribution(float t);
}
```

`Contribution` uses a smooth ease (smoothstep) up then down, so the price glides
into and out of the shock rather than snapping. Which station a shock targets is
tracked by *where the shock lives* (see Station), so it is not a field here.

### 2. `MarketDirector.cs` (new, MonoBehaviour — the only stateful piece)

The scheduler and the owner of the headline feed.

- On a timer (`spawnIntervalMin`/`Max`), while fewer than `maxActiveShocks` are
  live, it schedules a new shock: pick a random station, a random good, a random
  direction, and a magnitude in a tunable range; build the headline text; and add
  the shock to the **target station's** active-shock list.
- Each update it prunes shocks whose `EndTime` has passed.
- Exposes the union of currently active headlines for the UI (`IEnumerable`/list).
- Follows the project's static-registry idiom: a self-healing `Instance` lookup
  so a domain reload during play (which wipes statics — see the Station.All
  lesson) is recovered by re-finding the scene object.
- Needs the station list; `StationField` creates and wires it at spawn.

Tunable fields (serialized): `spawnIntervalMin`/`Max`, `maxActiveShocks`,
`riseDuration`, `decayDuration`, `magnitudeFractionMin`/`Max` (peak deviation as a
fraction of the affected good's base price; larger than the ~20% natural wave so a
shock dominates while active), and an up-vs-down probability.

### 3. `Station.cs` (light change)

- Holds its own `List<MarketShock> activeShocks` (only shocks targeting this
  station; each carries its commodity). The director adds to and prunes this list.
- When pricing a good, fold the active shocks into the mid **before** the spread:
  `effectiveMid(c, t) = markets[c].Mid(t) + Σ shock.Contribution(t)` over active
  shocks with `shock.commodity == c`.
- `SellPrice`, `BuyPrice`, and `PriceTrend` all use the effective mid, so the
  buy/sell straddle and the `^`/`v` trend cue reflect the shock automatically.
- `bool HasShock(Commodity c)` so the UI can flag the affected row.

### 4. `PriceCurve.cs` (small refactor, behaviour-preserving)

Today the straddle/trend live inside `SellPriceToPlayer(t)` / `BuyPriceFromPlayer(t)`
/ `Trend(t)`, which call `Mid(t)` internally. To let the Station inject the shock
offset, expose the straddle as helpers that take a mid value:

- `int SellFromMid(float mid)` → `RoundToInt(Max(priceFloor, mid + spread/2))`
- `int BuyFromMid(float mid)`  → `RoundToInt(Max(priceFloor, mid - spread/2))`
- `int TrendFromMids(float midNow, float midPast)` → +1 / 0 / -1 with the existing
  deadband.

The existing `Mid(t)`, `spread`, `priceFloor`, lookback and deadband stay. The old
`SellPriceToPlayer`/`BuyPriceFromPlayer`/`Trend(t)` can be reimplemented on top of
these helpers (passing the unshocked mid) so nothing else breaks.

### 5. `TradeController.cs` (UI, additive)

- The docked panel gains a **news section**: a header plus one line per active
  headline read from `MarketDirector` (the system-wide feed). If none are active,
  show a quiet "No market news." line.
- Each affected good's existing market row shows a `!` cue when
  `s.HasShock(c)` is true, so the player can match a headline to a price line.
- Everything else (corner readout, per-good rows, Buy/Sell buttons, high-res
  scaling) is unchanged.

### 6. `StationField.cs` (small change)

After spawning the stations, create the `MarketDirector`, give it the station
list, and wire the self-healing reference. No change to market construction.

### Time source

`Time.timeSinceLevelLoad`, consistent with Steps 2 and 2b.

## Balance / feel notes

- **Lead vs travel (feel-work).** Because news only shows when docked, a shock
  must last long enough that a headline read at one station is still actionable
  after the player flies to the target. `riseDuration` (the lead) plus the
  high-price window around the peak are tuned against jump-drive travel time. This
  is a play-test judgment call.
- **Shock size.** Peak deviation is a larger fraction of base price than the
  natural wave (so the event clearly dominates while active), but the `priceFloor`
  still guards a crash from going to zero.
- **Cadence.** 1–2 active shocks and a ~30–60s spawn gap keep the feed legible and
  each event meaningful.

## Testing

Play-testing (project convention; no automated suite). `MarketShock` and
`PriceCurve` stay pure logic, so they remain unit-test-ready if/when test
infrastructure (asmdefs) is added later.

## Acceptance tests (play-test)

- [ ] Headlines appear in the docked panel, are readable, and each names a real
      station, good, and direction.
- [ ] A spike headline → that good's price at that station visibly rises, peaks,
      then falls back to normal.
- [ ] A crash headline → that good's price visibly drops, bottoms, then recovers.
- [ ] The affected good's row shows the `!` cue while its shock is active.
- [ ] Acting on a headline (sell into a spike / buy into a crash) is reachable in
      time and clearly **more profitable** than ignoring it.
- [ ] Only ~1–2 shocks are active at once; the feed stays legible.
- [ ] No console errors; the Step 2/2b trade loop is unaffected.
- [ ] Feel approved by the director (lead time, shock size, and cadence feel
      right) after play-testing.

## Out of scope (deferred)

- **The full docked console + cockpit vision** (camera transition on dock; a
  console with manifest/profit, damage+repair, fuel+refuel, shields, and a ship
  blueprint with upgrade slots; an in-flight cockpit with dashboards). Captured
  separately as a parked **Rung 3** design; see
  `2026-06-29-docked-console-cockpit-vision.md`. Step 2c's news is built as a
  self-contained unit so it drops into that console later for nearly free.
- False/uncertain news (rumors), cryptic headlines, an always-on flying news
  ticker, player-triggered or news-from-missions events, shock persistence/saving,
  and shocks that affect more than one (station, good) at a time.

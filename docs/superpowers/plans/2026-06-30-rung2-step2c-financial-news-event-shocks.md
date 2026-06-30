# Rung 2, Step 2c — Financial News + Event Shocks Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a system-wide news feed and one-off, both-direction price shocks so the market has legible, tradeable events that a sharp player can read in the docked panel and profit from.

**Architecture:** The market stays a pure function of time. A shock is modelled as another pure time-term (`MarketShock.Contribution(t)`, a smoothstep pulse) added to a station's mid-price before the buy/sell spread. A single `MarketDirector` MonoBehaviour schedules shocks onto stations and exposes the active-headline feed. Existing classes get small, additive changes. The shock unit is self-contained so it later drops into the Rung 3 docked console for free.

**Tech Stack:** Unity 6.3 LTS (6000.3.18f1), C#, URP. Code-spawned scene objects. Placeholder OnGUI UI (no Canvas).

## Global Constraints

- **Engine:** Unity 6.3 LTS (6000.3.18f1). Do not introduce APIs newer than this LTS.
- **No automated tests.** Project convention is play-testing only; adding a unit test would require introducing asmdefs (out of scope). Each task is verified by a **compile-check** (focus Unity so it recompiles, then read the MCP console — empty/clean = pass) plus, where the task changes visible behaviour, a **play-test checkpoint**.
- **Time source:** `Time.timeSinceLevelLoad`, consistent with Steps 2 and 2b.
- **Purity:** `MarketShock` and `PriceCurve` stay pure (no Unity scene dependency, no per-frame state) so same `t` in = same value out.
- **No em dashes in code comments or UI copy.** Use commas, periods, or parentheses.
- **Randomness:** use `UnityEngine.Random` (scene-runtime randomness is fine here; determinism is only required of the pure pricing classes).
- **Compile-check protocol:** new `.cs` files must be imported before they compile — ask David to focus the Unity window (auto-imports changed/new scripts and recompiles). A "Connection failed"/timeout right after a code change usually means the recompile/server restart is mid-flight: re-read the console, do not retry blindly. An empty `[]` from `get_console_logs` (error level) means a clean compile.

---

### Task 1: `MarketShock` (pure pulse class)

A single shock: a signed, smoothly-ramping deviation added to one good's mid-price at one station. Pure — no Unity scene dependency, no per-frame state.

**Files:**
- Create: `Assets/Scripts/Station/MarketShock.cs`

**Interfaces:**
- Consumes: `Commodity` (existing enum).
- Produces:
  - `class MarketShock` with public fields `Commodity commodity`, `string headline`, `float announceTime`, `float riseDuration`, `float decayDuration`, `float magnitude`.
  - `float PeakTime` (get), `float EndTime` (get).
  - `bool IsActive(float t)`.
  - `float Contribution(float t)` — 0 before announce and after end, eases up to `magnitude` at peak, eases back to 0 by end.

- [ ] **Step 1: Create the file**

Create `Assets/Scripts/Station/MarketShock.cs`:

```csharp
using UnityEngine;

/// <summary>
/// One market event: a temporary, signed bump on a single good's mid-price at a
/// single station. Pure value object (no Unity scene dependency, no per-frame
/// state), so the same time t in always gives the same contribution out, matching
/// the deterministic PriceCurve design.
///
/// The pulse eases up from zero at announceTime, peaks at `magnitude` after
/// riseDuration (the lead time the player gets from the headline), then eases back
/// to zero over decayDuration. A positive magnitude is a price spike (sell into
/// it); a negative magnitude is a crash (buy into it). Which station the shock
/// targets is tracked by which station's list it lives in, not stored here.
/// </summary>
public class MarketShock
{
    public Commodity commodity;   // which good this shock moves
    public string headline;       // e.g. "Fuel shortage at Station Helios (...)"
    public float announceTime;    // t when it appears and the ramp starts
    public float riseDuration;    // ramp time, announce -> peak (the lead time)
    public float decayDuration;   // peak -> back to normal
    public float magnitude;       // signed peak deviation in credits (+spike / -crash)

    /// <summary>t at which the deviation reaches its full magnitude.</summary>
    public float PeakTime => announceTime + riseDuration;

    /// <summary>t at which the shock has fully decayed back to normal.</summary>
    public float EndTime => PeakTime + decayDuration;

    /// <summary>True while the shock is contributing anything to the price.</summary>
    public bool IsActive(float t) => t >= announceTime && t < EndTime;

    /// <summary>
    /// Signed deviation to add to the mid-price at time t. Zero outside the active
    /// window; a smoothstep ramp up to `magnitude` at the peak, then a smoothstep
    /// ramp back down to zero. Durations of zero collapse to an instant edge
    /// (no divide-by-zero).
    /// </summary>
    public float Contribution(float t)
    {
        if (t < announceTime || t >= EndTime) return 0f;

        if (t < PeakTime)
        {
            float u = riseDuration > 0f ? (t - announceTime) / riseDuration : 1f;
            return magnitude * Mathf.SmoothStep(0f, 1f, u);
        }

        float d = decayDuration > 0f ? (t - PeakTime) / decayDuration : 1f;
        return magnitude * (1f - Mathf.SmoothStep(0f, 1f, d));
    }
}
```

- [ ] **Step 2: Compile-check**

Ask David to focus the Unity window so the new script imports and compiles. Then read the console:
Run (MCP): `get_console_logs` at error level.
Expected: empty `[]` (clean compile). The class is not referenced yet, so the game is unchanged.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Station/MarketShock.cs
git commit -m "feat(market): add pure MarketShock pulse class"
```

---

### Task 2: `PriceCurve` mid-injection helpers (behaviour-preserving refactor)

Expose the straddle and trend as helpers that take a mid value, so a caller can fold a shock offset into the mid before the spread is applied. The existing time-based methods are reimplemented on top, passing the unshocked mid, so current behaviour is byte-for-byte identical.

**Files:**
- Modify: `Assets/Scripts/Station/PriceCurve.cs`

**Interfaces:**
- Consumes: nothing new.
- Produces (new public surface on `PriceCurve`):
  - `int SellFromMid(float mid)` — `RoundToInt(Max(priceFloor, mid + spread/2))`.
  - `int BuyFromMid(float mid)` — `RoundToInt(Max(priceFloor, mid - spread/2))`.
  - `int TrendFromMids(float midNow, float midPast)` — +1 / 0 / -1 with the existing deadband.
  - `float TrendLookbackSeconds` (get) — exposes the lookback constant so a caller can sample the past mid itself.
  - `float basePrice` is already public (used in Task 4 via `Station.BasePrice`).

- [ ] **Step 1: Replace the pricing/trend methods with mid-helpers plus thin wrappers**

In `Assets/Scripts/Station/PriceCurve.cs`, replace this block:

```csharp
    /// <summary>Price the player pays to buy one unit (mid plus half the spread).</summary>
    public int SellPriceToPlayer(float t)
    {
        return Mathf.RoundToInt(Mathf.Max(priceFloor, Mid(t) + spread * 0.5f));
    }

    /// <summary>Price the station pays the player per unit sold (mid minus half the spread).</summary>
    public int BuyPriceFromPlayer(float t)
    {
        return Mathf.RoundToInt(Mathf.Max(priceFloor, Mid(t) - spread * 0.5f));
    }

    /// <summary>Recent direction of the mid-price: +1 rising, -1 falling, 0 flat.</summary>
    public int Trend(float t)
    {
        float delta = Mid(t) - Mid(t - TrendLookback);
        if (delta > TrendDeadband) return 1;
        if (delta < -TrendDeadband) return -1;
        return 0;
    }
```

with:

```csharp
    // --- Mid-injection helpers ---------------------------------------------
    // The straddle and trend take a mid VALUE rather than a time, so a caller
    // (Station) can add an event shock's deviation to the mid before the spread
    // is applied. The time-based methods below are thin wrappers passing the
    // plain (unshocked) mid, so existing behaviour is unchanged.

    /// <summary>Player's buy price for a given mid (mid plus half the spread).</summary>
    public int SellFromMid(float mid)
    {
        return Mathf.RoundToInt(Mathf.Max(priceFloor, mid + spread * 0.5f));
    }

    /// <summary>Station's pay-out price for a given mid (mid minus half the spread).</summary>
    public int BuyFromMid(float mid)
    {
        return Mathf.RoundToInt(Mathf.Max(priceFloor, mid - spread * 0.5f));
    }

    /// <summary>Trend from a now-mid and a past-mid: +1 rising, -1 falling, 0 flat.</summary>
    public int TrendFromMids(float midNow, float midPast)
    {
        float delta = midNow - midPast;
        if (delta > TrendDeadband) return 1;
        if (delta < -TrendDeadband) return -1;
        return 0;
    }

    /// <summary>Seconds a caller should look back to sample the past mid for a trend.</summary>
    public float TrendLookbackSeconds => TrendLookback;

    // --- Time-based convenience (unshocked) --------------------------------

    /// <summary>Price the player pays to buy one unit (mid plus half the spread).</summary>
    public int SellPriceToPlayer(float t) => SellFromMid(Mid(t));

    /// <summary>Price the station pays the player per unit sold (mid minus half the spread).</summary>
    public int BuyPriceFromPlayer(float t) => BuyFromMid(Mid(t));

    /// <summary>Recent direction of the mid-price: +1 rising, -1 falling, 0 flat.</summary>
    public int Trend(float t) => TrendFromMids(Mid(t), Mid(t - TrendLookback));
```

- [ ] **Step 2: Compile-check**

Ask David to focus Unity, then read the console.
Run (MCP): `get_console_logs` at error level.
Expected: empty `[]`. `Station` still calls `SellPriceToPlayer`/`BuyPriceFromPlayer`/`Trend`, which now delegate to the helpers, so nothing observable changes.

- [ ] **Step 3: Play-test checkpoint (regression only)**

Enter Play. Dock at a station. Confirm prices, the `^`/`v`/`-` trend cue, and the buy/sell spread behave exactly as before this task (ambient waves only, no shocks yet).
This is feel/regression verification: tell David to watch for any change in the numbers (there should be none).

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Station/PriceCurve.cs
git commit -m "refactor(market): expose PriceCurve mid-injection helpers (behaviour-preserving)"
```

---

### Task 3: `Station` folds active shocks into pricing

Give each station its own list of active shocks (only those targeting it), fold them into the effective mid before the spread, and expose the small surface the director and UI need. No shocks are created yet, so the game is still unchanged after this task.

**Files:**
- Modify: `Assets/Scripts/Station/Station.cs`

**Interfaces:**
- Consumes: `MarketShock` (Task 1), `PriceCurve.SellFromMid/BuyFromMid/TrendFromMids/TrendLookbackSeconds/basePrice` (Task 2).
- Produces (new public surface on `Station`):
  - `void AddShock(MarketShock shock)`.
  - `void PruneExpiredShocks(float t)`.
  - `int LiveShockCount(float t)`.
  - `bool HasShock(Commodity c)` — true if a shock for good `c` is active now.
  - `float BasePrice(Commodity c)` — the good's centre price (for sizing shock magnitude).
  - `System.Collections.Generic.IReadOnlyList<MarketShock> ActiveShocks` (get).
  - Existing `SellPrice`/`BuyPrice`/`PriceTrend` now reflect shocks automatically.

- [ ] **Step 1: Add the shock list field**

In `Assets/Scripts/Station/Station.cs`, just after the `markets` field:

```csharp
    // One moving market per good. Set in Initialize.
    Dictionary<Commodity, PriceCurve> markets = new Dictionary<Commodity, PriceCurve>();
```

add:

```csharp
    // Event shocks currently targeting this station (each carries its commodity).
    // The MarketDirector adds shocks here and prunes expired ones.
    readonly List<MarketShock> activeShocks = new List<MarketShock>();

    /// <summary>Read-only view of this station's active shocks (for the news feed).</summary>
    public IReadOnlyList<MarketShock> ActiveShocks => activeShocks;
```

- [ ] **Step 2: Replace the price/trend accessors with shock-aware versions**

Replace this block:

```csharp
    /// <summary>Price the player pays to buy one unit of good c right now.</summary>
    public int SellPrice(Commodity c) => markets[c].SellPriceToPlayer(Time.timeSinceLevelLoad);

    /// <summary>Price this station pays the player per unit of good c sold right now.</summary>
    public int BuyPrice(Commodity c) => markets[c].BuyPriceFromPlayer(Time.timeSinceLevelLoad);

    /// <summary>Recent price direction for good c: +1 rising, -1 falling, 0 flat.</summary>
    public int PriceTrend(Commodity c) => markets[c].Trend(Time.timeSinceLevelLoad);
```

with:

```csharp
    float Now => Time.timeSinceLevelLoad;

    /// <summary>
    /// Mid-price for good c at time t, with any active event shocks for that good
    /// folded in (summed) before the spread is applied.
    /// </summary>
    float EffectiveMid(Commodity c, float t)
    {
        float mid = markets[c].Mid(t);
        for (int i = 0; i < activeShocks.Count; i++)
            if (activeShocks[i].commodity == c)
                mid += activeShocks[i].Contribution(t);
        return mid;
    }

    /// <summary>Price the player pays to buy one unit of good c right now.</summary>
    public int SellPrice(Commodity c) => markets[c].SellFromMid(EffectiveMid(c, Now));

    /// <summary>Price this station pays the player per unit of good c sold right now.</summary>
    public int BuyPrice(Commodity c) => markets[c].BuyFromMid(EffectiveMid(c, Now));

    /// <summary>Recent price direction for good c: +1 rising, -1 falling, 0 flat.</summary>
    public int PriceTrend(Commodity c)
    {
        float t = Now;
        float look = markets[c].TrendLookbackSeconds;
        return markets[c].TrendFromMids(EffectiveMid(c, t), EffectiveMid(c, t - look));
    }

    /// <summary>Centre (base) price of good c, used to size shock magnitudes.</summary>
    public float BasePrice(Commodity c) => markets[c].basePrice;

    /// <summary>True if an event shock for good c is active right now.</summary>
    public bool HasShock(Commodity c)
    {
        float t = Now;
        for (int i = 0; i < activeShocks.Count; i++)
            if (activeShocks[i].commodity == c && activeShocks[i].IsActive(t))
                return true;
        return false;
    }

    /// <summary>Add a shock targeting this station. Called by the MarketDirector.</summary>
    public void AddShock(MarketShock shock) => activeShocks.Add(shock);

    /// <summary>Drop shocks that have fully decayed. Called by the MarketDirector.</summary>
    public void PruneExpiredShocks(float t) => activeShocks.RemoveAll(sh => t >= sh.EndTime);

    /// <summary>How many shocks are active here right now (for the active-count cap).</summary>
    public int LiveShockCount(float t)
    {
        int n = 0;
        for (int i = 0; i < activeShocks.Count; i++)
            if (activeShocks[i].IsActive(t)) n++;
        return n;
    }
```

- [ ] **Step 2 note:** `System.Collections.Generic` is already imported at the top of `Station.cs` (`using System.Collections.Generic;`), so `IReadOnlyList`/`List` resolve without a new using.

- [ ] **Step 3: Compile-check**

Ask David to focus Unity, then read the console.
Run (MCP): `get_console_logs` at error level.
Expected: empty `[]`. No shocks are ever added yet, so `EffectiveMid` always equals the plain mid and the trade loop is unchanged.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Station/Station.cs
git commit -m "feat(market): Station folds active shocks into effective mid"
```

---

### Task 4: `MarketDirector` (scheduler + headline feed)

The one stateful piece: a MonoBehaviour that, on a timer, schedules shocks onto random stations while under the active cap, prunes expired shocks, and exposes the union of active headlines for the UI. Uses the existing self-healing `Station.All` registry to find stations, and its own self-healing `Instance` so a domain reload during play is recovered.

**Files:**
- Create: `Assets/Scripts/Station/MarketDirector.cs`

**Interfaces:**
- Consumes: `Station.All` + `Station.EnsureRegistry()` (existing), `Station.AddShock/PruneExpiredShocks/LiveShockCount/BasePrice` (Task 3), `Commodities.All/DisplayName` (existing), `MarketShock` (Task 1).
- Produces:
  - `static MarketDirector Get()` — self-healing singleton accessor (null if none in scene).
  - `List<string> ActiveHeadlines()` — union of active headlines across all stations.
  - Serialized tunables: `spawnIntervalMin/Max`, `maxActiveShocks`, `riseDuration`, `decayDuration`, `magnitudeFractionMin/Max`, `spikeProbability`.

**Design note (deliberate, documented):** the director reads stations from the existing `Station.All` registry (self-healing via `EnsureRegistry`) rather than holding a second serialized array. This avoids a redundant registry that could desync on a domain reload, and matches the project's established static-registry idiom. `StationField` (Task 5) only has to create the director, not feed it a list.

- [ ] **Step 1: Create the file**

Create `Assets/Scripts/Station/MarketDirector.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Schedules one-off market shocks and owns the news feed. On a timer, while
/// fewer than maxActiveShocks are live, it picks a random station, good, and
/// direction, sizes a signed magnitude from the good's base price, writes a plain
/// headline, and hands the shock to the target station (which folds it into its
/// price). Each frame it also prunes shocks that have fully decayed.
///
/// Stations are read from the self-healing Station.All registry. This director
/// also self-heals its own static Instance so a domain reload during play (which
/// wipes statics) is recovered by re-finding the scene object.
/// </summary>
public class MarketDirector : MonoBehaviour
{
    [Tooltip("Shortest and longest gap between new shocks, in seconds.")]
    public float spawnIntervalMin = 30f;
    public float spawnIntervalMax = 60f;

    [Tooltip("Most shocks allowed active across the whole system at once.")]
    public int maxActiveShocks = 2;

    [Tooltip("Lead time from headline to peak, in seconds (player's warning window).")]
    public float riseDuration = 12f;

    [Tooltip("Time from peak back to normal, in seconds.")]
    public float decayDuration = 12f;

    [Tooltip("Peak deviation as a fraction of the good's base price. Larger than the " +
             "~20% natural wave so a shock clearly dominates while active.")]
    public float magnitudeFractionMin = 0.45f;
    public float magnitudeFractionMax = 0.70f;

    [Tooltip("Chance a shock is an upward spike (sell-into). The rest are crashes (buy-into).")]
    [Range(0f, 1f)] public float spikeProbability = 0.5f;

    static MarketDirector instance;

    // Next time (timeSinceLevelLoad) a shock is allowed to spawn.
    float nextSpawnTime;

    /// <summary>
    /// Self-healing accessor. Returns the scene's director, re-finding it if a
    /// domain reload cleared the static reference. Null if none exists.
    /// </summary>
    public static MarketDirector Get()
    {
        if (instance == null) instance = FindFirstObjectByType<MarketDirector>();
        return instance;
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // First shock after one normal interval, so the market opens calm.
        nextSpawnTime = Time.timeSinceLevelLoad + Random.Range(spawnIntervalMin, spawnIntervalMax);
    }

    void Update()
    {
        float t = Time.timeSinceLevelLoad;
        Station.EnsureRegistry();

        // Prune decayed shocks everywhere.
        for (int i = 0; i < Station.All.Count; i++)
            Station.All[i].PruneExpiredShocks(t);

        // Spawn a new shock if it is time and we are under the active cap.
        if (t >= nextSpawnTime && ActiveCount(t) < maxActiveShocks)
        {
            ScheduleShock(t);
            nextSpawnTime = t + Random.Range(spawnIntervalMin, spawnIntervalMax);
        }
    }

    int ActiveCount(float t)
    {
        int n = 0;
        for (int i = 0; i < Station.All.Count; i++)
            n += Station.All[i].LiveShockCount(t);
        return n;
    }

    void ScheduleShock(float t)
    {
        if (Station.All.Count == 0) return;

        Station target = Station.All[Random.Range(0, Station.All.Count)];
        Commodity c = Commodities.All[Random.Range(0, Commodities.All.Length)];
        bool spike = Random.value < spikeProbability;

        float frac = Random.Range(magnitudeFractionMin, magnitudeFractionMax);
        float magnitude = target.BasePrice(c) * frac * (spike ? 1f : -1f);

        var shock = new MarketShock
        {
            commodity = c,
            headline = BuildHeadline(target, c, spike),
            announceTime = t,
            riseDuration = riseDuration,
            decayDuration = decayDuration,
            magnitude = magnitude,
        };
        target.AddShock(shock);
    }

    // Plain-language headline naming the good, station, and direction (always true).
    static string BuildHeadline(Station s, Commodity c, bool spike)
    {
        string good = Commodities.DisplayName(c);
        if (spike)
            return good + " shortage at " + s.displayName + " (prices spiking, sell here)";
        return good + " glut at " + s.displayName + " (prices crashing, buy here)";
    }

    /// <summary>Union of active headlines across the system, for the docked news panel.</summary>
    public List<string> ActiveHeadlines()
    {
        var lines = new List<string>();
        float t = Time.timeSinceLevelLoad;
        Station.EnsureRegistry();
        for (int i = 0; i < Station.All.Count; i++)
        {
            var shocks = Station.All[i].ActiveShocks;
            for (int j = 0; j < shocks.Count; j++)
                if (shocks[j].IsActive(t)) lines.Add(shocks[j].headline);
        }
        return lines;
    }
}
```

- [ ] **Step 2: Compile-check**

Ask David to focus Unity so the new script imports and compiles, then read the console.
Run (MCP): `get_console_logs` at error level.
Expected: empty `[]`. Nothing creates a `MarketDirector` yet, so the game is unchanged.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Station/MarketDirector.cs
git commit -m "feat(market): add MarketDirector shock scheduler and news feed"
```

---

### Task 5: `StationField` creates the director

Spawn one `MarketDirector` after the stations exist, so shocks start happening. The UI does not show headlines yet, but prices will visibly spike and crash, which is the testable deliverable here.

**Files:**
- Modify: `Assets/Scripts/Station/StationField.cs`

**Interfaces:**
- Consumes: `MarketDirector` (Task 4).
- Produces: a live `MarketDirector` component in the scene at play time.

- [ ] **Step 1: Add an optional tuning field (so the director is configurable from StationField)**

In `Assets/Scripts/Station/StationField.cs`, after the existing market header block (after the `foreignBasePrice` field, before `public Station[] Stations`):

```csharp
    [Tooltip("Base price of goods not produced at a station. High, so they are dear to sell into.")]
    public float foreignBasePrice = 90f;
```

add:

```csharp
    [Header("Market (event shocks)")]
    [Tooltip("Shortest and longest gap between news-driven price shocks, in seconds.")]
    public float shockIntervalMin = 30f;
    public float shockIntervalMax = 60f;

    [Tooltip("Most shocks active across the system at once.")]
    public int maxActiveShocks = 2;
```

- [ ] **Step 2: Create the director at the end of `Start()`**

In `Start()`, after the station/marker spawn loop closes (after the `for` loop, before the method's closing brace):

```csharp
        for (int i = 0; i < defs.Length; i++)
        {
            SpawnPlanet(defs[i].planetName, defs[i].tint, planetPos[i]);

            Vector3 spos = planetPos[i] + new Vector3(stationOffset, 0f, 0f);
            var markets = BuildMarkets(defs[i].home, i, defs.Length);
            Station s = SpawnStation(defs[i].stationName, defs[i].id, defs[i].tint, spos, defs[i].home, markets);
            Stations[i] = s;

            var marker = gameObject.AddComponent<TargetMarker>();
            marker.target = s;
            marker.cam = cam;
        }
```

add immediately after the loop:

```csharp
        // Stations exist now, so start the news/shock director. It reads the
        // self-healing Station.All registry, so it needs no station list passed in.
        var director = gameObject.AddComponent<MarketDirector>();
        director.spawnIntervalMin = shockIntervalMin;
        director.spawnIntervalMax = shockIntervalMax;
        director.maxActiveShocks = maxActiveShocks;
```

- [ ] **Step 3: Compile-check**

Ask David to focus Unity, then read the console.
Run (MCP): `get_console_logs` at error level.
Expected: empty `[]`.

- [ ] **Step 4: Play-test checkpoint (shocks are live, no UI yet)**

Enter Play and dock at a station (or watch its market rows). Within ~`shockIntervalMin`..`Max` seconds a good somewhere should start drifting hard up or down past its normal wave, peak, then return. Because the panel has no news section yet, the cue is purely the moving number.
Tell David: lower `shockIntervalMin/Max` temporarily (e.g. 5/10) to see a shock quickly, then restore. Confirm no console errors during play. This de-risks the director before the UI lands.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Station/StationField.cs
git commit -m "feat(market): spawn MarketDirector from StationField"
```

---

### Task 6: `TradeController` news section + `!` cue

Add the player-facing payoff: a news section in the docked panel listing active headlines, and a `!` cue on each affected good's market row so a headline can be matched to a price line.

**Files:**
- Modify: `Assets/Scripts/Ship/TradeController.cs`

**Interfaces:**
- Consumes: `MarketDirector.Get()` + `ActiveHeadlines()` (Task 4), `Station.HasShock(c)` (Task 3).
- Produces: docked news panel + per-row `!` cue. No other UI change.

- [ ] **Step 1: Add the `!` cue to each market row**

In `Assets/Scripts/Ship/TradeController.cs`, inside the per-good loop, replace this block:

```csharp
            int tr = s.PriceTrend(c);
            string trend = tr > 0 ? "^" : tr < 0 ? "v" : "-";
            string tag = s.Produces(c) ? " (produced here)" : "";
            string info = " " + Commodities.DisplayName(c) + tag
                        + "    Buy " + s.SellPrice(c)
                        + "    Sell " + s.BuyPrice(c)
                        + "    " + trend
                        + "    held " + hold[c];
```

with:

```csharp
            int tr = s.PriceTrend(c);
            string trend = tr > 0 ? "^" : tr < 0 ? "v" : "-";
            string tag = s.Produces(c) ? " (produced here)" : "";
            string cue = s.HasShock(c) ? "  !" : "";
            string info = " " + Commodities.DisplayName(c) + tag + cue
                        + "    Buy " + s.SellPrice(c)
                        + "    Sell " + s.BuyPrice(c)
                        + "    " + trend
                        + "    held " + hold[c];
```

- [ ] **Step 2: Draw the news section below the market rows**

At the end of `OnGUI()`, after the per-good `for` loop closes (before `OnGUI`'s closing brace):

```csharp
        for (int i = 0; i < Commodities.All.Length; i++)
        {
            // ... existing row drawing ...
        }
```

add:

```csharp
        // News section: system-wide active headlines, below the market rows.
        float newsY = y + (rowH + pad) * (Commodities.All.Length + 1) + pad * 2f;

        var newsHeaderStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = Mathf.RoundToInt(15f * scale),
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Box(new Rect(x, newsY, w, rowH), "Market News", newsHeaderStyle);

        var newsStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = Mathf.RoundToInt(13f * scale),
            alignment = TextAnchor.MiddleLeft
        };

        MarketDirector director = MarketDirector.Get();
        var headlines = director != null ? director.ActiveHeadlines() : null;

        if (headlines == null || headlines.Count == 0)
        {
            float ny = newsY + (rowH + pad);
            GUI.Box(new Rect(x, ny, w, rowH), " No market news.", newsStyle);
        }
        else
        {
            for (int i = 0; i < headlines.Count; i++)
            {
                float ny = newsY + (rowH + pad) * (i + 1);
                GUI.Box(new Rect(x, ny, w, rowH), " " + headlines[i], newsStyle);
            }
        }
```

- [ ] **Step 3: Compile-check**

Ask David to focus Unity, then read the console.
Run (MCP): `get_console_logs` at error level.
Expected: empty `[]`.

- [ ] **Step 4: Full play-test (acceptance)**

Enter Play. With `shockIntervalMin/Max` temporarily low (e.g. 8/15) to make events frequent, dock and verify against the spec's acceptance tests:
- Headlines appear in the **Market News** section, readable, each naming a real station, good, and direction.
- A spike headline → that good's price at that station visibly rises, peaks, then falls back; a crash headline → it drops, bottoms, then recovers.
- The affected good's row shows the `!` cue while its shock is active.
- Acting on a headline (sell into a spike at the target / buy into a crash) is reachable in time and clearly more profitable than ignoring it.
- Only ~1–2 shocks active at once; the feed stays legible.
- No console errors; the Step 2/2b trade loop is unaffected.

Then **restore** `shockIntervalMin/Max` to the tuned values (30/60 default) and judge feel: lead time vs travel, shock size, cadence. These are David's calls. Flag clearly that lead time vs jump-drive travel is the key feel knob.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Ship/TradeController.cs
git commit -m "feat(ui): docked news section and shock cue in trade panel"
```

---

## Self-Review

**Spec coverage:**
- News feed (system-wide, docked-only) → Task 4 (`ActiveHeadlines`) + Task 6 (news section). ✓
- Shocks both directions (spike/crash) → Task 1 signed `magnitude` + Task 4 `spikeProbability`. ✓
- News heralds shock (lead time before peak) → Task 1 `riseDuration`/`PeakTime`, announce at ramp start. ✓
- News always true → every scheduled shock writes a headline; no false path. ✓
- Explicit headlines (station, good, direction) → Task 4 `BuildHeadline`. ✓
- Few shocks (1–2 active, 30–60s cadence) → Task 4 `maxActiveShocks` + `spawnIntervalMin/Max`, wired in Task 5. ✓
- Pure shock as time-term added to mid → Task 1 `Contribution` + Task 3 `EffectiveMid`. ✓
- `PriceCurve` stays pure, helpers expose straddle/trend → Task 2. ✓
- Sell/Buy/Trend reflect shock automatically → Task 3 (all route through `EffectiveMid`). ✓
- `HasShock` → `!` cue → Task 3 + Task 6. ✓
- Self-healing director `Instance` → Task 4 `Get()`. ✓
- StationField spawns/wires director → Task 5. ✓
- Time source `Time.timeSinceLevelLoad` → used in Station, MarketDirector. ✓
- Smoothstep ease → Task 1. ✓
- priceFloor still guards a crash from zero → preserved in `SellFromMid`/`BuyFromMid` (Task 2). ✓

**Placeholder scan:** no TBD/TODO/"handle edge cases"/"similar to Task N". All steps carry full code. Divide-by-zero on zero durations is explicitly guarded. ✓

**Type consistency:** method names match across tasks — `SellFromMid`/`BuyFromMid`/`TrendFromMids`/`TrendLookbackSeconds` (Task 2 ↔ Task 3), `AddShock`/`PruneExpiredShocks`/`LiveShockCount`/`HasShock`/`BasePrice`/`ActiveShocks` (Task 3 ↔ Task 4/6), `Get()`/`ActiveHeadlines()`/`spawnIntervalMin`/`spawnIntervalMax`/`maxActiveShocks` (Task 4 ↔ Task 5/6), `Commodities.All`/`DisplayName` (existing). `MarketShock` fields match the spec block. ✓

**Deliberate deviation noted:** director reads `Station.All` instead of a passed array (documented in Task 4 design note) — simpler and matches the established self-heal idiom; StationField only constructs the director. TDD red-green replaced by compile-check + play-test per the project's locked no-automated-tests convention (Global Constraints).

# Market Price Trends Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make each station's price move over time (a learnable wave plus light noise) with a buy/sell spread, so trading becomes a timing decision while still requiring travel.

**Architecture:** A new pure C# class `PriceCurve` holds the per-station market math as a function of time (no per-frame state, so it is deterministic). `Station` owns a `PriceCurve` and exposes current `SellPrice` / `BuyPrice` / `PriceTrend`. `StationField` builds one curve per station with distinct phase/period so the markets move out of step. `TradeController` buys at `SellPrice`, sells at `BuyPrice`, and shows both prices plus a trend cue in the docked OnGUI panel.

**Tech Stack:** Unity 6.3 LTS (6000.3.18f1), URP, C#, IMGUI (`OnGUI`) placeholder UI. No new packages.

## Global Constraints

- **Engine/pipeline:** Unity 6.3 LTS, URP. Do not change versions or add packages.
- **Single commodity:** Do NOT add commodity types. One generic cargo unit only (multiple commodities are a later step).
- **Price visibility:** Docked station only. Do NOT build any market board or remote price display.
- **Naming (fixed meaning):** In the UI, **"Buy"** is the price the player pays (`PriceCurve.SellPriceToPlayer`), **"Sell"** is the price the station pays the player (`PriceCurve.BuyPriceFromPlayer`).
- **No automated tests this step:** project convention is play-testing. Keep `PriceCurve` pure (no Unity dependency beyond `UnityEngine.Mathf`) so it stays test-ready for later. Do NOT add assembly definitions or the test framework.
- **UI style:** placeholder IMGUI (`OnGUI`), no Canvas. Keep the existing high-res scaling (`Mathf.Max(1f, Screen.height / 1080f) * 1.3f`).
- **No em dashes** in code comments or UI strings (house style). Use commas, periods, or parentheses.
- **Tooling reality:** Unity ignores MCP commands while backgrounded. Before any MCP command (recompile/console-read), the director ("the Nudge person") must focus the Unity window. Focusing Unity also auto-imports changed/new `.cs` files and recompiles. A timed-out MCP call may still have run; verify by reading state, do not blindly retry.

---

## File Structure

- **Create** `Assets/Scripts/Station/PriceCurve.cs` — the moving-market math for one station (pure class, function of time).
- **Modify** `Assets/Scripts/Station/Station.cs` — replace the single `int unitPrice` with a `PriceCurve` and add `SellPrice` / `BuyPrice` / `PriceTrend` properties; `Initialize` takes a `PriceCurve`.
- **Modify** `Assets/Scripts/Station/StationField.cs` — rename the per-station `price` to `basePrice`, add tunable market params, build one `PriceCurve` per station with distinct phase/period/seed, pass it to `Initialize`.
- **Modify** `Assets/Scripts/Ship/TradeController.cs` — buy at `SellPrice`, sell at `BuyPrice`; docked panel shows a trend cue and both prices.

---

### Task 1: PriceCurve (the market math)

**Files:**
- Create: `Assets/Scripts/Station/PriceCurve.cs`

**Interfaces:**
- Consumes: nothing (only `UnityEngine.Mathf`).
- Produces: class `PriceCurve` with public fields `basePrice`, `amplitude`, `period`, `phase`, `noiseAmplitude`, `noiseScale`, `seed`, `spread`, `priceFloor` (all `float`), and methods:
  - `float Mid(float t)`
  - `int SellPriceToPlayer(float t)` (price the player pays to buy)
  - `int BuyPriceFromPlayer(float t)` (price the station pays the player)
  - `int Trend(float t)` returns +1 rising / -1 falling / 0 flat

- [ ] **Step 1: Create the file**

Write `Assets/Scripts/Station/PriceCurve.cs`:

```csharp
using UnityEngine;

/// <summary>
/// The moving market price for one station. A pure function of time: given a
/// time t (in seconds), it returns the current prices. No per-frame state, so it
/// is deterministic (same t in, same prices out) and can be reasoned about and
/// tested in isolation.
///
/// The mid-price is a baseline plus a slow sine wave (the learnable trend) plus
/// light coherent noise (Perlin, so it drifts smoothly instead of jittering each
/// frame). Buy and sell prices straddle the mid by half the spread, so a station
/// always pays less to buy cargo from the player than it charges to sell cargo to
/// the player. That makes a same-station round trip lose the spread, so travel is
/// required for profit.
/// </summary>
public class PriceCurve
{
    [Tooltip("Centre price the market oscillates around, in credits.")]
    public float basePrice = 50f;

    [Tooltip("Wave swing above and below the base, in credits.")]
    public float amplitude = 10f;

    [Tooltip("Seconds for one full wave cycle.")]
    public float period = 30f;

    [Tooltip("Wave start offset, as a fraction (0..1) of a cycle.")]
    public float phase = 0f;

    [Tooltip("Noise swing, in credits.")]
    public float noiseAmplitude = 2.5f;

    [Tooltip("How fast the noise drifts (larger = faster).")]
    public float noiseScale = 0.04f;

    [Tooltip("Per-station offset so each station's noise differs.")]
    public float seed = 0f;

    [Tooltip("Gap between buy and sell price, in credits.")]
    public float spread = 9f;

    [Tooltip("Prices never drop below this, in credits.")]
    public float priceFloor = 5f;

    // Seconds to look back when measuring trend direction.
    const float TrendLookback = 1.5f;
    // Mid-price change smaller than this (credits) reads as flat.
    const float TrendDeadband = 0.05f;

    /// <summary>The mid-price at time t.</summary>
    public float Mid(float t)
    {
        float wave = amplitude * Mathf.Sin(2f * Mathf.PI * (t / period + phase));
        // PerlinNoise returns 0..1; recentre to about -1..1, then scale.
        float n = (Mathf.PerlinNoise(seed, t * noiseScale) - 0.5f) * 2f;
        return basePrice + wave + noiseAmplitude * n;
    }

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
}
```

- [ ] **Step 2: Compile check**

Ask the director to focus the Unity window (this auto-imports `PriceCurve.cs` and recompiles). Then read the console.
Run (MCP): `get_console_logs` with `logType: error`, `includeStackTrace: false`, `limit: 20`.
Expected: no compile errors referencing `PriceCurve`. (`PriceCurve` is not yet used anywhere, so a clean compile is the whole check.)

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Station/PriceCurve.cs Assets/Scripts/Station/PriceCurve.cs.meta
git commit -m "feat: add PriceCurve market math for moving station prices"
```

Note: Unity generates the `.meta` on import. If `PriceCurve.cs.meta` does not exist yet (director has not focused Unity), commit just the `.cs` and add the `.meta` in the next commit.

---

### Task 2: Wire the moving market into stations and trading

**Files:**
- Modify: `Assets/Scripts/Station/Station.cs`
- Modify: `Assets/Scripts/Station/StationField.cs`
- Modify: `Assets/Scripts/Ship/TradeController.cs`

**Interfaces:**
- Consumes: `PriceCurve` (from Task 1).
- Produces:
  - `Station.SellPrice` (int), `Station.BuyPrice` (int), `Station.PriceTrend` (int).
  - `Station.Initialize(string displayName, string id, Color tint, Vector3 position, PriceCurve market)`.
  - `StationField` tunable fields: `amplitudeFraction`, `noiseFraction`, `spreadFraction`, `periodMin`, `periodMax`, `noiseScale`, `priceFloor`.

- [ ] **Step 1: Update `Station.cs`**

In `Assets/Scripts/Station/Station.cs`, replace the price field and `Initialize`.

Replace this block:

```csharp
    [Tooltip("Price of one cargo unit at this station, in credits. Buy and sell both use this.")]
    public int unitPrice = 0;

    [Tooltip("Where a ship docks, as an offset from the station centre.")]
    public Vector3 dockLocalOffset = new Vector3(0f, 0f, 40f);

    /// <summary>World-space point a ship aims for when docking (used later).</summary>
    public Vector3 DockPoint => transform.TransformPoint(dockLocalOffset);

    /// <summary>Configure and build the station. Call right after AddComponent.</summary>
    public void Initialize(string displayName, string id, Color tint, Vector3 position, int unitPrice)
    {
        this.displayName = displayName;
        this.id = id;
        this.tint = tint;
        this.unitPrice = unitPrice;
        name = displayName;
        transform.position = position;
        if (!All.Contains(this)) All.Add(this);
        BuildBody();
    }
```

with:

```csharp
    [Tooltip("The moving market for this station. Buy and sell prices derive from it.")]
    public PriceCurve market = new PriceCurve();

    [Tooltip("Where a ship docks, as an offset from the station centre.")]
    public Vector3 dockLocalOffset = new Vector3(0f, 0f, 40f);

    /// <summary>World-space point a ship aims for when docking.</summary>
    public Vector3 DockPoint => transform.TransformPoint(dockLocalOffset);

    /// <summary>Price the player pays to buy one cargo unit right now.</summary>
    public int SellPrice => market.SellPriceToPlayer(Time.timeSinceLevelLoad);

    /// <summary>Price this station pays the player per cargo unit sold right now.</summary>
    public int BuyPrice => market.BuyPriceFromPlayer(Time.timeSinceLevelLoad);

    /// <summary>Recent price direction: +1 rising, -1 falling, 0 flat.</summary>
    public int PriceTrend => market.Trend(Time.timeSinceLevelLoad);

    /// <summary>Configure and build the station. Call right after AddComponent.</summary>
    public void Initialize(string displayName, string id, Color tint, Vector3 position, PriceCurve market)
    {
        this.displayName = displayName;
        this.id = id;
        this.tint = tint;
        this.market = market;
        name = displayName;
        transform.position = position;
        if (!All.Contains(this)) All.Add(this);
        BuildBody();
    }
```

Leave `BuildBody`, `AddPart`, `OnDestroy`, and `EnsureRegistry` unchanged.

- [ ] **Step 2: Update `StationField.cs`**

In `Assets/Scripts/Station/StationField.cs`, make three edits.

(a) Add market tuning fields. After the `stationOffset` field block (before `public Station[] Stations { get; private set; }`), insert:

```csharp
    [Header("Market (price trends)")]
    [Tooltip("Wave swing as a fraction of base price.")]
    public float amplitudeFraction = 0.20f;

    [Tooltip("Light noise swing as a fraction of base price.")]
    public float noiseFraction = 0.05f;

    [Tooltip("Buy/sell spread as a fraction of base price. Kept below the gap between station base prices so travel still pays.")]
    public float spreadFraction = 0.18f;

    [Tooltip("Shortest and longest wave period (seconds). Each station gets a value spread across this range.")]
    public float periodMin = 22f;
    public float periodMax = 38f;

    [Tooltip("How fast the noise drifts.")]
    public float noiseScale = 0.04f;

    [Tooltip("Hard floor so prices never reach zero, in credits.")]
    public float priceFloor = 5f;
```

(b) Rename the per-station price in the `Def` struct from `price` to `basePrice`. Replace the struct with:

```csharp
    struct Def
    {
        public string planetName, stationName, id;
        public Color tint;
        public int basePrice;
        public Def(string planetName, string stationName, string id, Color tint, int basePrice)
        {
            this.planetName = planetName; this.stationName = stationName;
            this.id = id; this.tint = tint; this.basePrice = basePrice;
        }
    }
```

(The `new Def(...)` calls in `Start` already pass 50 / 65 / 80 as the last argument, so they need no change.)

(c) Build a per-station curve and pass it to the station. Replace this line in `Start`:

```csharp
            Station s = SpawnStation(defs[i].stationName, defs[i].id, defs[i].tint, spos, defs[i].price);
```

with:

```csharp
            PriceCurve market = BuildMarket(defs[i].basePrice, i, defs.Length);
            Station s = SpawnStation(defs[i].stationName, defs[i].id, defs[i].tint, spos, market);
```

And replace the `SpawnStation` method with the new signature plus a `BuildMarket` helper:

```csharp
    Station SpawnStation(string displayName, string id, Color tint, Vector3 position, PriceCurve market)
    {
        GameObject go = new GameObject(displayName);
        Station station = go.AddComponent<Station>();
        station.Initialize(displayName, id, tint, position, market);
        return station;
    }

    // Build a moving market for one station. Period and phase are spread across
    // the stations (by index) so the three markets do not move in lockstep.
    PriceCurve BuildMarket(int basePrice, int index, int count)
    {
        float frac = count > 1 ? (float)index / count : 0f;
        return new PriceCurve
        {
            basePrice = basePrice,
            amplitude = basePrice * amplitudeFraction,
            period = Mathf.Lerp(periodMin, periodMax, frac),
            phase = frac,
            noiseAmplitude = basePrice * noiseFraction,
            noiseScale = noiseScale,
            seed = index * 13.7f + 1f,
            spread = basePrice * spreadFraction,
            priceFloor = priceFloor,
        };
    }
```

- [ ] **Step 3: Update `TradeController.cs`**

In `Assets/Scripts/Ship/TradeController.cs`:

(a) Update the class summary comment (first block) to:

```csharp
/// <summary>
/// Player economy. Holds credits and a single cargo type. Buys cargo at the
/// docked station's live SellPrice and sells at its live BuyPrice (the station's
/// buy/sell spread means a same-station round trip loses money, so profit needs
/// travel). Draws a placeholder OnGUI readout plus, when docked, a trade panel
/// showing the market trend and both prices. Lives on the Ship GameObject
/// alongside DockingController. Placeholder UI on purpose (no Canvas).
/// </summary>
```

(b) Replace the can-trade checks and the `Buy`/`Sell` methods:

```csharp
    bool CanBuy(Station s) => s != null && credits >= s.SellPrice && cargoUnits < cargoCapacity;
    bool CanSell(Station s) => s != null && cargoUnits > 0;

    void Buy(Station s)
    {
        if (!CanBuy(s)) return;
        credits -= s.SellPrice;
        cargoUnits += 1;
    }

    void Sell(Station s)
    {
        if (!CanSell(s)) return;
        credits += s.BuyPrice;
        cargoUnits -= 1;
    }
```

(c) Replace the docked-panel portion of `OnGUI` (everything from `// Docked trade panel.` to the end of the method) with:

```csharp
        // Docked trade panel.
        Station s = docking != null ? docking.DockedStation : null;
        if (s == null) return;

        float w = 260f * scale;
        float x = Screen.width * 0.5f - w * 0.5f;
        float y = 80f * scale;
        float rowH = 38f * scale;
        float pad = 4f * scale;
        var pstyle = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(15f * scale) };

        // Trend cue (ASCII so it renders in the default IMGUI font).
        string trend = s.PriceTrend > 0 ? "^ rising"
                     : s.PriceTrend < 0 ? "v falling"
                     : "- steady";
        GUI.Box(new Rect(x, y, w, rowH), "Market: " + trend, pstyle);

        // Both live prices. "Buy" is what you pay, "Sell" is what the station pays you.
        GUI.Box(new Rect(x, y + (rowH + pad), w, rowH),
            "Buy @ " + s.SellPrice + "    Sell @ " + s.BuyPrice, pstyle);

        var bstyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(15f * scale) };
        float by = y + (rowH + pad) * 2f;
        GUI.enabled = CanBuy(s);
        if (GUI.Button(new Rect(x, by, w * 0.5f - pad, rowH), "Buy", bstyle)) Buy(s);
        GUI.enabled = CanSell(s);
        if (GUI.Button(new Rect(x + w * 0.5f + pad, by, w * 0.5f - pad, rowH), "Sell", bstyle)) Sell(s);
        GUI.enabled = true;
```

- [ ] **Step 4: Compile check**

Ask the director to focus Unity (auto-imports and recompiles). Then read the console.
Run (MCP): `get_console_logs` with `logType: error`, `includeStackTrace: false`, `limit: 30`.
Expected: no compile errors. Common slip if it fails: a leftover reference to `unitPrice` (there should be none) or a `Def.price` reference (renamed to `basePrice`).

- [ ] **Step 5: Play-test smoke check (director)**

Ask the director to enter Play mode and verify the basics (this is feel-light, just "does it work"):
- The three stations still spawn and can be reached and docked (F).
- While docked, the panel shows "Market: ..." with a trend that changes, and "Buy @ X    Sell @ Y" where **X is greater than Y** (the spread).
- The Buy / Sell buttons still buy and sell one unit and move credits.
- No errors appear in the console during play.

If anything is broken, fix before committing. If it works, continue.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Station/Station.cs Assets/Scripts/Station/StationField.cs Assets/Scripts/Ship/TradeController.cs
git commit -m "feat: stations use moving PriceCurve market with buy/sell spread"
```

---

### Task 3: Play-test tuning and acceptance

**Files:**
- Modify (only if tuning): `Assets/Scripts/Station/StationField.cs` (the market tuning fields)

This task adds no new behaviour. It is the feel gate: the director plays and judges, and we adjust the tuning numbers until the moving market feels right. All tuning lives in the `StationField` fields from Task 2 (`amplitudeFraction`, `noiseFraction`, `spreadFraction`, `periodMin`, `periodMax`, `noiseScale`, `priceFloor`), so changes are one-line edits, no logic changes.

- [ ] **Step 1: Run the acceptance checklist (director plays)**

Enter Play mode and confirm each item from the spec:
- Prices visibly move while docked; the trend cue matches the movement (loiter a few seconds and watch it rise and fall).
- Buy price is greater than sell price at the same station (the spread is visible).
- A full profitable loop works: buy at a cheap station (Helios, base 50), fly, sell at a dear station (Cobalt, base 80), credits increase.
- Timing pays: waiting for a dip to buy / a peak to sell beats transacting blind.
- No console errors across the three-planet system.

- [ ] **Step 2: Tune to taste (if needed)**

Adjust the `StationField` fields and re-test. Guidance:
- **Swings too wild / too calm:** change `amplitudeFraction` (lower = calmer). Note: if amplitude is large relative to the gap between station base prices, a badly timed trade can lose money. That is intended (timing matters), but lower it if it feels punishing.
- **Cycle too fast / too slow to wait out:** change `periodMin` / `periodMax`.
- **Spread too costly / too trivial:** change `spreadFraction`. Keep it well below the base-price gaps (50 / 65 / 80, so gaps of 15 and 30) or travel stops paying.
- **Noise too jittery:** lower `noiseFraction` or `noiseScale`.

Apply any agreed change as a direct edit to the field default in `StationField.cs`.

- [ ] **Step 3: Commit (only if tuning changed)**

```bash
git add Assets/Scripts/Station/StationField.cs
git commit -m "tune: market price trend feel (amplitude/period/spread)"
```

- [ ] **Step 4: Update the roadmap**

In `docs/ROADMAP.md`, tick the Step 2 item and the "where we are" note once the director confirms acceptance passed.

```bash
git add docs/ROADMAP.md
git commit -m "docs: mark Rung 2 step 2 (market price trends) complete"
```

---

## Notes for the implementer

- **MCP timing:** Unity must be the foreground window to process MCP commands. Before each compile-check, the director focuses Unity (which also imports new/changed scripts and recompiles). A "request timed out" often means the command ran anyway; verify via a console read rather than retrying blindly.
- **`.meta` files:** Unity creates the `.meta` for `PriceCurve.cs` on import. Include it in a commit once it exists.
- **Do not** add commodity types, a market board, assembly definitions, or the test framework in this step. Those are later steps.

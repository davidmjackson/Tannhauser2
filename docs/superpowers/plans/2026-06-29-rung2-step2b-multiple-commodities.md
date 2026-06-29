# Multiple Commodities Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add three goods (Fuel, Grain, Electronics), each produced by one station (cheap there, dear elsewhere), with a shared cargo hold and per-(station, good) moving prices, so trading is about routes as well as timing.

**Architecture:** A new `Commodity` enum plus a `Commodities` helper names the goods. Each `Station` holds one `PriceCurve` per good (the Step 2 class, reused unchanged) and a `homeCommodity`; `StationField` builds those nine markets, giving the home good a low base price and the others a high base price. `TradeController` tracks a per-good hold sharing one capacity and renders one trade row per good.

**Tech Stack:** Unity 6.3 LTS (6000.3.18f1), URP, C#, IMGUI (`OnGUI`) placeholder UI. No new packages.

## Global Constraints

- **Engine/pipeline:** Unity 6.3 LTS, URP. Do not change versions or add packages.
- **Three goods only:** Fuel (Helios), Grain (Verdant), Electronics (Cobalt). Do NOT add more goods.
- **Station specialization:** a station's home good uses the low base price; the other two use the high base price.
- **Shared hold:** one capacity (10) shared across all goods, tracked per good. Total held can never exceed capacity.
- **One unit per click:** one Buy and one Sell button per good; each moves a single unit.
- **Naming (fixed meaning):** "Buy" is the price the player pays (`Station.SellPrice(c)`); "Sell" is what the station pays the player (`Station.BuyPrice(c)`).
- **No automated tests this step:** project convention is play-testing. `PriceCurve` stays pure and unchanged. Do NOT add assembly definitions or the test framework.
- **UI style:** placeholder IMGUI (`OnGUI`), no Canvas. Keep the existing high-res scaling (`Mathf.Max(1f, Screen.height / 1080f) * 1.3f`).
- **Price visibility:** docked station only. No market board for other stations.
- **No em dashes** in code comments or UI strings (house style).
- **Tooling reality:** Unity ignores MCP commands while backgrounded. Before any MCP command, the director ("the Nudge person") focuses the Unity window (which also auto-imports changed/new scripts and recompiles). A timed-out or "connection failed" MCP call may still have run, and "connection failed" right after a code change usually means a recompile is in progress; verify by re-reading, do not blindly retry.

---

## File Structure

- **Create** `Assets/Scripts/Station/Commodity.cs` — the `Commodity` enum and the `Commodities` helper (list + display names).
- **Modify** `Assets/Scripts/Station/Station.cs` — replace the single `PriceCurve` with a per-commodity dictionary plus a `homeCommodity`; per-good price API; new `Initialize`.
- **Modify** `Assets/Scripts/Station/StationField.cs` — map each station to a home good, add `homeBasePrice` / `foreignBasePrice`, build nine markets, pass them in.
- **Modify** `Assets/Scripts/Ship/TradeController.cs` — per-good shared hold, per-good buy/sell, multi-row docked panel.
- `Assets/Scripts/Station/PriceCurve.cs` is reused unchanged.

---

### Task 1: Commodity model

**Files:**
- Create: `Assets/Scripts/Station/Commodity.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `enum Commodity { Fuel, Grain, Electronics }`
  - `static class Commodities` with `Commodity[] All` and `string DisplayName(Commodity c)`.

- [ ] **Step 1: Create the file**

Write `Assets/Scripts/Station/Commodity.cs`:

```csharp
/// <summary>
/// The tradeable goods. Each station produces exactly one of these (its home
/// good), which is cheap where produced and dear elsewhere.
/// </summary>
public enum Commodity
{
    Fuel,
    Grain,
    Electronics,
}

/// <summary>Helpers for enumerating and naming commodities.</summary>
public static class Commodities
{
    /// <summary>All goods, in display order.</summary>
    public static readonly Commodity[] All =
    {
        Commodity.Fuel,
        Commodity.Grain,
        Commodity.Electronics,
    };

    /// <summary>Human-readable name for a good.</summary>
    public static string DisplayName(Commodity c)
    {
        switch (c)
        {
            case Commodity.Fuel: return "Fuel";
            case Commodity.Grain: return "Grain";
            case Commodity.Electronics: return "Electronics";
            default: return c.ToString();
        }
    }
}
```

- [ ] **Step 2: Commit**

(The compile-check is folded into Task 2 to save a Unity focus-nudge, since this enum is unused until then and cannot fail in a way Task 2's check would not also catch.)

```bash
git add Assets/Scripts/Station/Commodity.cs Assets/Scripts/Station/Commodity.cs.meta
git commit -m "feat: add Commodity enum and Commodities helper"
```

Note: Unity generates the `.meta` on import. If it does not exist yet, commit just the `.cs` and add the `.meta` later.

---

### Task 2: Wire commodities into stations and trading

**Files:**
- Modify: `Assets/Scripts/Station/Station.cs`
- Modify: `Assets/Scripts/Station/StationField.cs`
- Modify: `Assets/Scripts/Ship/TradeController.cs`

**Interfaces:**
- Consumes: `Commodity`, `Commodities.All`, `Commodities.DisplayName` (Task 1); `PriceCurve` (Step 2).
- Produces:
  - `Station.SellPrice(Commodity)`, `Station.BuyPrice(Commodity)`, `Station.PriceTrend(Commodity)`, `Station.Produces(Commodity)`.
  - `Station.Initialize(string displayName, string id, Color tint, Vector3 position, Commodity homeCommodity, Dictionary<Commodity, PriceCurve> markets)`.
  - `StationField` fields `homeBasePrice`, `foreignBasePrice`.

- [ ] **Step 1: Update `Station.cs`**

In `Assets/Scripts/Station/Station.cs`, replace this block:

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

with:

```csharp
    [Tooltip("The good this station produces. It is cheap here; the other goods are dear.")]
    public Commodity homeCommodity = Commodity.Fuel;

    // One moving market per good. Set in Initialize.
    Dictionary<Commodity, PriceCurve> markets = new Dictionary<Commodity, PriceCurve>();

    [Tooltip("Where a ship docks, as an offset from the station centre.")]
    public Vector3 dockLocalOffset = new Vector3(0f, 0f, 40f);

    /// <summary>World-space point a ship aims for when docking.</summary>
    public Vector3 DockPoint => transform.TransformPoint(dockLocalOffset);

    /// <summary>Price the player pays to buy one unit of good c right now.</summary>
    public int SellPrice(Commodity c) => markets[c].SellPriceToPlayer(Time.timeSinceLevelLoad);

    /// <summary>Price this station pays the player per unit of good c sold right now.</summary>
    public int BuyPrice(Commodity c) => markets[c].BuyPriceFromPlayer(Time.timeSinceLevelLoad);

    /// <summary>Recent price direction for good c: +1 rising, -1 falling, 0 flat.</summary>
    public int PriceTrend(Commodity c) => markets[c].Trend(Time.timeSinceLevelLoad);

    /// <summary>True if this station produces good c (it is cheap here).</summary>
    public bool Produces(Commodity c) => c == homeCommodity;

    /// <summary>Configure and build the station. Call right after AddComponent.</summary>
    public void Initialize(string displayName, string id, Color tint, Vector3 position,
                           Commodity homeCommodity, Dictionary<Commodity, PriceCurve> markets)
    {
        this.displayName = displayName;
        this.id = id;
        this.tint = tint;
        this.homeCommodity = homeCommodity;
        this.markets = markets;
        name = displayName;
        transform.position = position;
        if (!All.Contains(this)) All.Add(this);
        BuildBody();
    }
```

`System.Collections.Generic` is already imported at the top of the file (for `List`), so `Dictionary` needs no new using. Leave `BuildBody`, `AddPart`, `OnDestroy`, and `EnsureRegistry` unchanged.

- [ ] **Step 2: Update `StationField.cs`**

In `Assets/Scripts/Station/StationField.cs`, make five edits.

(a) Add the Generic using. Replace the first line:

```csharp
using UnityEngine;
```

with:

```csharp
using System.Collections.Generic;
using UnityEngine;
```

(b) Add the two base-price fields. Immediately after the `priceFloor` field, before `public Station[] Stations { get; private set; }`, insert:

```csharp
    [Tooltip("Base price of a station's own (home) good. Low, so it is cheap to buy where produced.")]
    public float homeBasePrice = 40f;

    [Tooltip("Base price of goods not produced at a station. High, so they are dear to sell into.")]
    public float foreignBasePrice = 90f;
```

(c) Replace the `Def` struct (the per-station price becomes a home good):

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

with:

```csharp
    struct Def
    {
        public string planetName, stationName, id;
        public Color tint;
        public Commodity home;
        public Def(string planetName, string stationName, string id, Color tint, Commodity home)
        {
            this.planetName = planetName; this.stationName = stationName;
            this.id = id; this.tint = tint; this.home = home;
        }
    }
```

(d) Replace the `defs` array and the spawn lines inside `Start`. Replace:

```csharp
        var defs = new[]
        {
            new Def("Helios",  "Station Helios",  "STN-H", new Color(1f, 0.55f, 0.15f),  50),
            new Def("Verdant", "Station Verdant", "STN-V", new Color(0.35f, 0.8f, 0.45f), 65),
            new Def("Cobalt",  "Station Cobalt",  "STN-C", new Color(0.3f, 0.6f, 1f),     80),
        };
```

with:

```csharp
        var defs = new[]
        {
            new Def("Helios",  "Station Helios",  "STN-H", new Color(1f, 0.55f, 0.15f),  Commodity.Fuel),
            new Def("Verdant", "Station Verdant", "STN-V", new Color(0.35f, 0.8f, 0.45f), Commodity.Grain),
            new Def("Cobalt",  "Station Cobalt",  "STN-C", new Color(0.3f, 0.6f, 1f),     Commodity.Electronics),
        };
```

And replace:

```csharp
            PriceCurve market = BuildMarket(defs[i].basePrice, i, defs.Length);
            Station s = SpawnStation(defs[i].stationName, defs[i].id, defs[i].tint, spos, market);
```

with:

```csharp
            var markets = BuildMarkets(defs[i].home, i, defs.Length);
            Station s = SpawnStation(defs[i].stationName, defs[i].id, defs[i].tint, spos, defs[i].home, markets);
```

(e) Replace the `SpawnStation` method and the `BuildMarket` method. Replace:

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

with:

```csharp
    Station SpawnStation(string displayName, string id, Color tint, Vector3 position,
                         Commodity home, Dictionary<Commodity, PriceCurve> markets)
    {
        GameObject go = new GameObject(displayName);
        Station station = go.AddComponent<Station>();
        station.Initialize(displayName, id, tint, position, home, markets);
        return station;
    }

    // Build a moving market for each good at one station. The home good gets the
    // low base price (cheap to buy here); the others get the high base price (dear
    // to sell into). Each (station, good) gets a distinct phase/period/seed from a
    // lane index (0..stationCount*goodCount-1) so all the markets drift out of step.
    Dictionary<Commodity, PriceCurve> BuildMarkets(Commodity home, int stationIndex, int stationCount)
    {
        var goods = Commodities.All;
        int laneCount = stationCount * goods.Length;
        var dict = new Dictionary<Commodity, PriceCurve>();
        for (int g = 0; g < goods.Length; g++)
        {
            Commodity c = goods[g];
            float basePrice = (c == home) ? homeBasePrice : foreignBasePrice;
            int lane = stationIndex * goods.Length + g;
            float frac = laneCount > 1 ? (float)lane / laneCount : 0f;
            dict[c] = new PriceCurve
            {
                basePrice = basePrice,
                amplitude = basePrice * amplitudeFraction,
                period = Mathf.Lerp(periodMin, periodMax, frac),
                phase = frac,
                noiseAmplitude = basePrice * noiseFraction,
                noiseScale = noiseScale,
                seed = lane * 13.7f + 1f,
                spread = basePrice * spreadFraction,
                priceFloor = priceFloor,
            };
        }
        return dict;
    }
```

- [ ] **Step 3: Rewrite `TradeController.cs`**

Replace the entire contents of `Assets/Scripts/Ship/TradeController.cs` with:

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player economy. Holds credits and a shared cargo hold tracked per commodity
/// (all goods compete for one capacity). Buys a good at the docked station's live
/// SellPrice and sells at its BuyPrice. With station specialization plus the
/// buy/sell spread, profit comes from carrying a station's cheap home good to a
/// station that pays dearly for it. Draws a placeholder OnGUI readout plus, when
/// docked, a trade panel with one row per good. Lives on the Ship GameObject
/// alongside DockingController. Placeholder UI on purpose (no Canvas).
/// </summary>
[RequireComponent(typeof(DockingController))]
public class TradeController : MonoBehaviour
{
    [Tooltip("Starting credits.")]
    public int credits = 1000;

    [Tooltip("Maximum total cargo units across all goods.")]
    public int cargoCapacity = 10;

    // Units held of each good. All goods share the single cargoCapacity.
    private readonly Dictionary<Commodity, int> hold = new Dictionary<Commodity, int>();

    private DockingController docking;

    void Awake()
    {
        docking = GetComponent<DockingController>();
        foreach (var c in Commodities.All)
            hold[c] = 0;
    }

    int TotalUnits
    {
        get
        {
            int sum = 0;
            foreach (var c in Commodities.All) sum += hold[c];
            return sum;
        }
    }

    bool CanBuy(Station s, Commodity c) => s != null && credits >= s.SellPrice(c) && TotalUnits < cargoCapacity;
    bool CanSell(Station s, Commodity c) => s != null && hold[c] > 0;

    void Buy(Station s, Commodity c)
    {
        if (!CanBuy(s, c)) return;
        credits -= s.SellPrice(c);
        hold[c] += 1;
    }

    void Sell(Station s, Commodity c)
    {
        if (!CanSell(s, c)) return;
        credits += s.BuyPrice(c);
        hold[c] -= 1;
    }

    void OnGUI()
    {
        // OnGUI uses fixed pixel sizes, so scale up on high-res displays
        // (reference height 1080). Keeps the placeholder UI readable.
        float scale = Mathf.Max(1f, Screen.height / 1080f) * 1.3f;

        // Always-on corner readout: credits, total cargo, per-good breakdown.
        string cargoLine = "";
        foreach (var c in Commodities.All)
            cargoLine += Commodities.DisplayName(c) + " " + hold[c] + "   ";
        string readout = "Credits: " + credits
                       + "\nCargo " + TotalUnits + "/" + cargoCapacity
                       + "\n" + cargoLine.TrimEnd();
        var style = new GUIStyle(GUI.skin.box)
        {
            fontSize = Mathf.RoundToInt(15f * scale),
            alignment = TextAnchor.UpperLeft
        };
        GUI.Box(new Rect(10f * scale, 10f * scale, 340f * scale, 92f * scale), readout, style);

        // Docked trade panel: one row per good.
        Station s = docking != null ? docking.DockedStation : null;
        if (s == null) return;

        float w = 540f * scale;
        float x = Screen.width * 0.5f - w * 0.5f;
        float y = 80f * scale;
        float rowH = 40f * scale;
        float pad = 4f * scale;
        float btnW = 60f * scale;
        float infoW = w - (btnW * 2f + pad * 3f);

        var hstyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = Mathf.RoundToInt(15f * scale),
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Box(new Rect(x, y, w, rowH), "Market - " + s.displayName, hstyle);

        var istyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = Mathf.RoundToInt(13f * scale),
            alignment = TextAnchor.MiddleLeft
        };
        var bstyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(14f * scale) };

        for (int i = 0; i < Commodities.All.Length; i++)
        {
            Commodity c = Commodities.All[i];
            float ry = y + (rowH + pad) * (i + 1);

            int tr = s.PriceTrend(c);
            string trend = tr > 0 ? "^" : tr < 0 ? "v" : "-";
            string tag = s.Produces(c) ? " (produced here)" : "";
            string info = " " + Commodities.DisplayName(c) + tag
                        + "    Buy " + s.SellPrice(c)
                        + "    Sell " + s.BuyPrice(c)
                        + "    " + trend
                        + "    held " + hold[c];
            GUI.Box(new Rect(x, ry, infoW, rowH), info, istyle);

            GUI.enabled = CanBuy(s, c);
            if (GUI.Button(new Rect(x + infoW + pad, ry, btnW, rowH), "Buy", bstyle)) Buy(s, c);
            GUI.enabled = CanSell(s, c);
            if (GUI.Button(new Rect(x + infoW + pad * 2f + btnW, ry, btnW, rowH), "Sell", bstyle)) Sell(s, c);
            GUI.enabled = true;
        }
    }
}
```

- [ ] **Step 4: Compile check**

Ask the director to focus Unity (auto-imports `Commodity.cs` and recompiles). Then read the console.
Run (MCP): `get_console_logs` with `logType: error`, `includeStackTrace: false`, `limit: 30`.
Expected: no compile errors. If it fails, the usual cause is a leftover call to the old no-argument `Station.SellPrice` / `BuyPrice` / `PriceTrend` or to `Station.market` (all removed), or a missing `using System.Collections.Generic;` in `StationField.cs`.

- [ ] **Step 5: Play-test smoke check (director)**

Ask the director to enter Play mode and verify the basics:
- All three stations spawn and can be docked (F).
- The docked panel header shows the station name, then three rows (Fuel, Grain, Electronics), each with Buy, Sell, a trend cue, and held count.
- The station's home good is tagged "(produced here)" and is clearly cheaper than the other two.
- Buy and Sell move one unit and adjust credits; buttons grey out when unaffordable / hold full (total 10) / none held.
- The corner readout shows credits, total cargo n/10, and the per-good breakdown.
- No console errors during play.

If anything is broken, fix before committing.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Station/Station.cs Assets/Scripts/Station/StationField.cs Assets/Scripts/Ship/TradeController.cs
git commit -m "feat: three specialized commodities with shared hold and per-good trade UI"
```

---

### Task 3: Play-test tuning and acceptance

**Files:**
- Modify (only if tuning): `Assets/Scripts/Station/StationField.cs` (the market tuning fields, `homeBasePrice`, `foreignBasePrice`)
- Modify (only if layout tuning): `Assets/Scripts/Ship/TradeController.cs` (panel sizes)

This task adds no new behaviour. It is the feel gate: the director plays and judges, and we adjust numbers until routes, spreads, and the panel layout feel right.

- [ ] **Step 1: Run the acceptance checklist (director plays)**

Enter Play mode and confirm each item from the spec:
- Each station's home good is clearly the cheapest place to buy it; the other two are dear there.
- A full route profits: buy Fuel cheap at Helios, fly to Cobalt, sell dear, credits increase.
- Shared hold caps at 10 total across mixed goods; cannot overfill.
- Prices still move per good, trend cues match, Buy > Sell per good (the spread holds).
- Panel shows all three goods with correct prices, trends, and held counts; Buy/Sell work and grey out correctly.
- Corner readout shows credits and per-good cargo correctly.
- No console errors across the three-planet system.

- [ ] **Step 2: Tune to taste (if needed)**

Adjust the `StationField` fields and re-test. Guidance:
- **Routes too lucrative / too thin:** change `homeBasePrice` (40) and `foreignBasePrice` (90). Wider gap = more profit per unit. Keep `homeBasePrice` clearly below `foreignBasePrice`.
- **Swings, period, spread, noise:** same fields and guidance as Step 2 (`amplitudeFraction`, `periodMin`/`periodMax`, `spreadFraction`, `noiseFraction`, `noiseScale`).
- **Panel text clipped or row spacing off:** adjust `w`, `btnW`, `rowH`, or the font sizes in `TradeController.OnGUI`. The corner readout box is `340 * scale` wide by `92 * scale` tall; widen if the per-good line clips.

Apply agreed changes as direct edits to the field defaults / layout constants.

- [ ] **Step 3: Commit (only if tuning changed)**

```bash
git add Assets/Scripts/Station/StationField.cs Assets/Scripts/Ship/TradeController.cs
git commit -m "tune: commodity base prices and trade panel layout"
```

- [ ] **Step 4: Update the roadmap**

In `docs/ROADMAP.md`, tick the Step 2b item and update the "where we are" note once the director confirms acceptance passed.

```bash
git add docs/ROADMAP.md
git commit -m "docs: mark Rung 2 step 2b (multiple commodities) complete"
```

---

## Notes for the implementer

- **MCP timing:** Unity must be foreground to process MCP commands. Before each compile-check, the director focuses Unity (which also imports new/changed scripts and recompiles). "Connection failed" right after a code change usually means a recompile is underway; re-read rather than retrying blindly.
- **`.meta` files:** Unity creates the `.meta` for `Commodity.cs` on import. Include it in a commit once it exists.
- **Do not** add more goods, a market board, assembly definitions, the test framework, or news/shocks in this step. Those are later steps.

# Trade Loop Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the Rung 1 core loop — buy cargo cheap at Station A, fly to Station B, sell it dear, watch credits rise — with a placeholder buy/sell UI.

**Architecture:** A new `TradeController` on the Ship owns credits and a single cargo type and draws all trade UI via `OnGUI`. It reads docked state from a new read-only `DockingController.DockedStation` property and reads price from a new `Station.unitPrice` field. One-way dependencies: docking owns movement state, stations own price, trade reads both and mutates only its own economy.

**Tech Stack:** Unity 6.3 LTS (6000.3.18f1), C#, URP. UI is Unity IMGUI (`OnGUI`), matching the existing docking placeholder. Editor driven via CoderGamester mcp-unity.

## Global Constraints

- **Unity 6.3 LTS only** (6000.3.18f1). Do not use APIs newer than this LTS line.
- **No automated test harness exists.** Verification per task = recompile scripts, confirm Console is clean (no errors/warnings from our code), then manual play-test where behaviour is visible. This matches the project norm; do not introduce a test framework.
- **MCP/Editor workflow rules (from CLAUDE.md lessons):**
  - Unity must be the **foreground window** to process MCP commands. Announce "Running now" before each Editor command so the director can wake Unity (mouse-wiggle). A "request timed out" often means the command actually ran — verify by reading state, do not blindly retry (retries create duplicates).
  - The MCP Server Window must stay open.
  - **Brand-new script files must be imported** before they compile: focus Unity and press Ctrl+R, or call `recompile_scripts`.
  - **Component adds/edits made during Play mode revert on Stop.** Attach components in **edit mode** only, then `save_scene`.
- **UI is placeholder, behaviour only.** `OnGUI`, no Canvas, no art. Do not polish layout/fonts/colours.
- **Single cargo type, fixed prices.** Multiple cargo types and price trends are parked.

---

### Task 1: Station pricing

Add a per-station cargo price and set it for A and B.

**Files:**
- Modify: `Assets/Scripts/Station/Station.cs` (add `unitPrice` field; add param to `Initialize`)
- Modify: `Assets/Scripts/Station/StationField.cs` (add `priceA`/`priceB`; pass through `Spawn`/`Initialize`)

**Interfaces:**
- Produces: `Station.unitPrice` (public int) — price of one cargo unit at this station.
- Produces: `Station.Initialize(string displayName, string id, Color tint, Vector3 position, int unitPrice)` — Initialize now takes price.

- [ ] **Step 1: Add the `unitPrice` field to `Station`**

In `Assets/Scripts/Station/Station.cs`, add this field directly below the existing `tint` field (after line 16, `public Color tint = Color.white;`):

```csharp
    [Tooltip("Price of one cargo unit at this station, in credits. Buy and sell both use this.")]
    public int unitPrice = 0;
```

- [ ] **Step 2: Add the `unitPrice` parameter to `Initialize`**

In the same file, replace the `Initialize` method signature and body so it accepts and stores the price. Change the signature line:

```csharp
    public void Initialize(string displayName, string id, Color tint, Vector3 position, int unitPrice)
```

and add this line inside the method, right after `this.tint = tint;`:

```csharp
        this.unitPrice = unitPrice;
```

- [ ] **Step 3: Add tunable price fields to `StationField`**

In `Assets/Scripts/Station/StationField.cs`, add these fields directly below the existing `colorB` field (after line 16):

```csharp
    [Tooltip("Cargo price at Station A (cheap, buy here).")]
    public int priceA = 50;
    [Tooltip("Cargo price at Station B (dear, sell here).")]
    public int priceB = 80;
```

- [ ] **Step 4: Pass prices through `Start` and `Spawn`**

In the same file, replace the two `Spawn(...)` calls in `Start()`:

```csharp
        StationA = Spawn("Station A", "STN-A", colorA, Vector3.zero, priceA);
        StationB = Spawn("Station B", "STN-B", colorB, new Vector3(0f, 0f, separation), priceB);
```

and replace the `Spawn` method so it forwards the price:

```csharp
    Station Spawn(string displayName, string id, Color tint, Vector3 position, int unitPrice)
    {
        GameObject go = new GameObject(displayName);
        Station station = go.AddComponent<Station>();
        station.Initialize(displayName, id, tint, position, unitPrice);
        return station;
    }
```

- [ ] **Step 5: Recompile and confirm the Console is clean**

Announce "Running now", then call `recompile_scripts`. Then call `get_console_logs` and confirm there are no compile errors or warnings referencing `Station.cs` or `StationField.cs`.
Expected: clean compile. If "request timed out", re-read the Console rather than re-running.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Station/Station.cs Assets/Scripts/Station/StationField.cs
git commit -m "feat: per-station cargo price (Rung 1 trade)"
```

---

### Task 2: Expose the docked station

Give `DockingController` a read-only property so `TradeController` can learn where it is docked.

**Files:**
- Modify: `Assets/Scripts/Ship/DockingController.cs` (add `DockedStation` property)

**Interfaces:**
- Consumes: nothing new.
- Produces: `DockingController.DockedStation` (public `Station`, read-only) — the station currently docked at, or `null` when flying.

- [ ] **Step 1: Add the `DockedStation` property**

In `Assets/Scripts/Ship/DockingController.cs`, add this property directly below the private fields block (after line 25, `private float cooldown;`):

```csharp
    /// <summary>The station the ship is docked at, or null when flying. Read by TradeController.</summary>
    public Station DockedStation => docked ? dockedAt : null;
```

- [ ] **Step 2: Recompile and confirm the Console is clean**

Announce "Running now", then call `recompile_scripts`, then `get_console_logs`.
Expected: clean compile, no errors referencing `DockingController.cs`.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Ship/DockingController.cs
git commit -m "feat: expose DockedStation from DockingController"
```

---

### Task 3: TradeController state and corner readout

Create the economy component with always-on credits/cargo readout, attach it to the Ship, and confirm it renders.

**Files:**
- Create: `Assets/Scripts/Ship/TradeController.cs`
- Modify (scene): attach `TradeController` to the Ship GameObject

**Interfaces:**
- Consumes: `DockingController` (required component on the same GameObject).
- Produces: `TradeController` with public fields `credits` (int), `cargoUnits` (int), `cargoCapacity` (int). Buy/Sell behaviour is added in Task 4.

- [ ] **Step 1: Create `TradeController.cs`**

Create `Assets/Scripts/Ship/TradeController.cs` with exactly this content:

```csharp
using UnityEngine;

/// <summary>
/// Player economy for Rung 1. Holds credits and a single cargo type, and draws a
/// placeholder trade UI via OnGUI. Always shows a corner readout of credits and
/// cargo. The docked Buy/Sell panel is added in Task 4. Lives on the Ship
/// GameObject alongside DockingController. Placeholder UI on purpose (no Canvas).
/// </summary>
[RequireComponent(typeof(DockingController))]
public class TradeController : MonoBehaviour
{
    [Tooltip("Starting credits.")]
    public int credits = 1000;

    [Tooltip("Cargo units currently held.")]
    public int cargoUnits = 0;

    [Tooltip("Maximum cargo units the hold can carry.")]
    public int cargoCapacity = 10;

    private DockingController docking;

    void Awake()
    {
        docking = GetComponent<DockingController>();
    }

    void OnGUI()
    {
        // Always-on corner readout.
        string readout = "Credits: " + credits + "\nCargo: " + cargoUnits + "/" + cargoCapacity;
        var style = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.UpperLeft };
        GUI.Box(new Rect(10f, 10f, 170f, 52f), readout, style);
    }
}
```

- [ ] **Step 2: Import the new script and confirm it compiles**

The file is brand new, so Unity must import it. Announce "Running now", focus Unity and press Ctrl+R (or call `recompile_scripts`). Then call `get_console_logs`.
Expected: clean compile, `TradeController` recognised, no errors.

- [ ] **Step 3: Attach `TradeController` to the Ship (edit mode)**

Make sure the Editor is **stopped** (not in Play mode), or the component will vanish on Stop. Announce "Running now". Find the Ship GameObject (the one with `ShipController` + `DockingController`) via `get_gameobject`, then add the `TradeController` component to it. Then call `save_scene`.
Verify with `get_gameobject` on the Ship: confirm `TradeController` is listed among its components.

- [ ] **Step 4: Play-test the readout**

Announce "Running now", enter Play mode. Confirm the top-left box shows `Credits: 1000` and `Cargo: 0/10`. Exit Play mode.
Expected: readout visible and correct. (Feel note for the director: this is placeholder-ugly on purpose.)

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Ship/TradeController.cs Assets/Scenes
git commit -m "feat: TradeController with credits/cargo readout"
```

(Adjust the scene path if the scene file lives elsewhere; include whatever `save_scene` modified.)

---

### Task 4: Buy and sell

Add the docked Buy/Sell panel and the guarded economy logic, then play-test the full loop.

**Files:**
- Modify: `Assets/Scripts/Ship/TradeController.cs` (add Can/Buy/Sell methods; extend `OnGUI`)

**Interfaces:**
- Consumes: `DockingController.DockedStation` (Task 2), `Station.unitPrice` (Task 1).
- Produces: working buy/sell — clicking Buy/Sell mutates `credits` and `cargoUnits` within guards.

- [ ] **Step 1: Add the guard and transaction methods**

In `Assets/Scripts/Ship/TradeController.cs`, add these four methods directly below `Awake()`:

```csharp
    bool CanBuy(Station s) => s != null && credits >= s.unitPrice && cargoUnits < cargoCapacity;
    bool CanSell(Station s) => s != null && cargoUnits > 0;

    void Buy(Station s)
    {
        if (!CanBuy(s)) return;
        credits -= s.unitPrice;
        cargoUnits += 1;
    }

    void Sell(Station s)
    {
        if (!CanSell(s)) return;
        credits += s.unitPrice;
        cargoUnits -= 1;
    }
```

- [ ] **Step 2: Extend `OnGUI` with the docked panel**

In the same file, replace the entire `OnGUI` method with this version (it keeps the corner readout and adds the docked panel below the existing `DOCKED` banner, which docking draws at y=30):

```csharp
    void OnGUI()
    {
        // Always-on corner readout.
        string readout = "Credits: " + credits + "\nCargo: " + cargoUnits + "/" + cargoCapacity;
        var style = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.UpperLeft };
        GUI.Box(new Rect(10f, 10f, 170f, 52f), readout, style);

        // Docked trade panel.
        Station s = docking != null ? docking.DockedStation : null;
        if (s == null) return;

        float w = 220f;
        float x = Screen.width * 0.5f - w * 0.5f;
        float y = 70f;
        var pstyle = new GUIStyle(GUI.skin.box) { fontSize = 15 };
        GUI.Box(new Rect(x, y, w, 30f), "Cargo price: " + s.unitPrice + " cr/unit", pstyle);

        GUI.enabled = CanBuy(s);
        if (GUI.Button(new Rect(x, y + 35f, w * 0.5f - 2f, 30f), "Buy")) Buy(s);
        GUI.enabled = CanSell(s);
        if (GUI.Button(new Rect(x + w * 0.5f + 2f, y + 35f, w * 0.5f - 2f, 30f), "Sell")) Sell(s);
        GUI.enabled = true;
    }
```

- [ ] **Step 3: Recompile and confirm the Console is clean**

Announce "Running now", call `recompile_scripts`, then `get_console_logs`.
Expected: clean compile, no errors referencing `TradeController.cs`.

- [ ] **Step 4: Play-test the full loop**

Announce "Running now", enter Play mode. Then:
1. Fly to Station A, dock (F). Panel shows `Cargo price: 50 cr/unit`.
2. Click **Buy** repeatedly: credits drop by 50 each click, cargo rises. Buy greys out at `10/10` (hold full) or when credits run short.
3. Undock (F), fly to Station B (1500u away), dock. Panel shows `Cargo price: 80 cr/unit`.
4. Click **Sell** repeatedly: credits rise by 80 each click, cargo falls. Sell greys out at `0/10`.
5. Confirm a full round trip moves credits from 1000 toward ~1300 (a full 10-unit load) with no Console errors.
Exit Play mode.

Expected: all of the above. If buy/sell feels tedious click-by-click, note it — a "buy/sell max" button is a trivial follow-up (parked unless the director asks).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Ship/TradeController.cs
git commit -m "feat: buy/sell cargo at docked station (Rung 1 trade loop)"
```

---

## Notes for the executor

- Stations have no solid colliders yet (known parked limitation) — the ship can fly through bodies. Not in scope here.
- `DockingController` already self-heals the `Station.All` registry after a mid-play recompile; nothing extra needed for trade.
- After Task 4, the Rung 1 acceptance items for trade are met in-Editor. The remaining Rung 1 acceptance item (a Windows build) is a separate step, not part of this plan.

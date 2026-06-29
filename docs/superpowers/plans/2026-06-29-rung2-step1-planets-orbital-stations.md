# Rung 2 Step 1 — Planets, Orbital Stations, Cruise/Jump Drive — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the two-station Rung 1 sandbox into a three-planet system with one orbital station per planet, on-screen markers for all stations with an aim-to-focus state, and an in-system cruise/jump drive.

**Architecture:** Follows the project's code-spawn pattern (bodies built from primitives at play time; in-Editor placement is unreliable here). `StationField` is rewritten to spawn three planets (`Planet`, new) and three stations in a triangle, each with a `TargetMarker`. A `NavTargeting` component on the Ship picks the station the nose points at and flags its marker focused; a `JumpDrive` component reads that focused station, locks heading, and boosts straight to it, suspending player steering via a new flag on `ShipController`.

**Tech Stack:** Unity 6.3 LTS (6000.3.18f1), C#, URP, new Input System (`Keyboard.current`), placeholder UI via `OnGUI` (no Canvas). Editor control via CoderGamester mcp-unity.

## Global Constraints

- **Engine:** Unity 6.3 LTS (6000.3.18f1). Do not change the line.
- **Input:** new Input System only (`Keyboard.current`, `Mouse.current`). Never `UnityEngine.Input.*`.
- **UI:** placeholder `OnGUI` only, scaled with `float scale = Mathf.Max(1f, Screen.height / 1080f) * 1.3f;` (matches `TradeController`/`DockingController`).
- **Material tint:** URP Lit uses `_BaseColor`, not `_Color` (see `Station.AddPart`).
- **No automated tests in this project.** Verification is in-Editor + play-testing. Flight/jump *feel* is the director's call.
- **Scope guard:** in-system jump only. NOT the Rung 4 inter-system hyperdrive. No price trends, no missions, no station colliders, no landable planets, no moving stations.
- **Naming:** the game is Tannhauser. No em dashes in user-facing text.

## mcp-unity workflow rules (apply to every scene/script step)

These come from hard-won project lessons. Bake them into each task:

1. **Unity must be the foreground window** for mcp-unity commands to run. Before a batch, prompt the director to focus Unity. Replies lag, so a timeout often means the command *did* run; verify by reading scene/console state, do not blindly retry (it can duplicate).
2. **New scripts need an import, not just a recompile.** After creating a `.cs` with the Write tool, run `execute_menu_item` with `Assets/Refresh` to generate the `.meta` and import it, THEN `recompile_scripts`. `recompile_scripts` alone compiles but does not AssetDatabase-import, so component adds fail with "type not found."
3. **Confirm a clean compile** with `get_console_logs` (filter errors) before wiring anything.
4. **Add/edit components in EDIT mode, then `save_scene`.** Component adds/edits made in Play mode revert on Stop.
5. The MCP Server Window must stay open the whole time.

## File Structure

**Create:**
- `Assets/Scripts/Station/Planet.cs` — one planet: a tinted procedural sphere, no collider, backdrop only.
- `Assets/Scripts/Ship/NavTargeting.cs` — on the Ship; picks the focused station from aim and flags its marker.
- `Assets/Scripts/Ship/JumpDrive.cs` — on the Ship; the cruise/jump state machine, prompts, and motion.

**Modify:**
- `Assets/Scripts/Station/StationField.cs` — rewrite: spawn 3 planets + 3 stations + 3 markers in a triangle (replaces the 2-station spawn).
- `Assets/Scripts/Station/TargetMarker.cs` — add a `Focused` flag and focused draw style (bold + amber).
- `Assets/Scripts/Ship/ShipController.cs` — add `controlsSuspended` flag with early-returns so JumpDrive can take over the Rigidbody.

**Scene (`Assets/Scenes/SampleScene.unity`):**
- Set new `StationField` field values (the reused `separation` field keeps its old serialized 1500 otherwise).
- Add `NavTargeting` and `JumpDrive` to the Ship GameObject (the one with `ShipController`).

---

## Task 1: Three-planet world layout

Replaces the two-station setup with three planets, each with one station and a marker. Docking and trade must still work at every station.

**Files:**
- Create: `Assets/Scripts/Station/Planet.cs`
- Modify: `Assets/Scripts/Station/StationField.cs` (full rewrite)
- Scene: set `StationField` fields on its GameObject

**Interfaces:**
- Consumes: `Station.Initialize(string displayName, string id, Color tint, Vector3 position, int unitPrice)`, `TargetMarker.target`, `TargetMarker.cam`.
- Produces: `Planet.Initialize(string displayName, Color tint, Vector3 position, float radius)`; `StationField.Stations` (`Station[]`, length 3); three `Station`s registered in `Station.All`; three `TargetMarker` components on the `StationField` GameObject.

- [ ] **Step 1: Create `Planet.cs`**

```csharp
using UnityEngine;

/// <summary>
/// One planet, built from a primitive sphere at spawn. Pure visual backdrop:
/// large, tinted, non-landable, no collider. Placeholder art on purpose.
/// </summary>
public class Planet : MonoBehaviour
{
    public string displayName = "Planet";
    public Color tint = Color.gray;

    /// <summary>Configure and build the planet. Call right after AddComponent.</summary>
    public void Initialize(string displayName, Color tint, Vector3 position, float radius)
    {
        this.displayName = displayName;
        this.tint = tint;
        name = displayName;
        transform.position = position;
        BuildBody(radius);
    }

    void BuildBody(float radius)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(transform, false);
        sphere.transform.localPosition = Vector3.zero;
        // The sphere primitive is 1 unit in diameter, so scale by diameter.
        sphere.transform.localScale = Vector3.one * (radius * 2f);

        // URP Lit uses _BaseColor, not _Color.
        Renderer r = sphere.GetComponent<Renderer>();
        if (r != null)
        {
            Material m = r.material;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
            else m.color = tint;
        }

        // Backdrop only: not landable, no collider.
        Collider col = sphere.GetComponent<Collider>();
        if (col != null) Destroy(col);
    }
}
```

- [ ] **Step 2: Rewrite `StationField.cs`** (replace the whole file)

```csharp
using UnityEngine;

/// <summary>
/// Spawns the Rung 2 system at play time: three planets as backdrops, each with
/// one static station nearby, plus one on-screen TargetMarker per station.
/// Replaces the Rung 1 two-station setup. Code-spawned (in-Editor placement is
/// unreliable in this project). Planets are visual only; stations keep the Rung 1
/// docking and trade behaviour.
/// </summary>
public class StationField : MonoBehaviour
{
    [Tooltip("Triangle leg length between planets, in world units.")]
    public float separation = 3500f;

    [Tooltip("Radius of each planet body, in world units.")]
    public float planetRadius = 300f;

    [Tooltip("Station distance from its planet centre, in world units.")]
    public float stationOffset = 600f;

    public Station[] Stations { get; private set; }

    struct Def
    {
        public string planetName, stationName, id;
        public Color tint;
        public int price;
        public Def(string planetName, string stationName, string id, Color tint, int price)
        {
            this.planetName = planetName; this.stationName = stationName;
            this.id = id; this.tint = tint; this.price = price;
        }
    }

    void Start()
    {
        var defs = new[]
        {
            new Def("Helios",  "Station Helios",  "STN-H", new Color(1f, 0.55f, 0.15f),  50),
            new Def("Verdant", "Station Verdant", "STN-V", new Color(0.35f, 0.8f, 0.45f), 65),
            new Def("Cobalt",  "Station Cobalt",  "STN-C", new Color(0.3f, 0.6f, 1f),     80),
        };

        Vector3[] planetPos = TrianglePositions(separation);
        Stations = new Station[defs.Length];
        var cam = Camera.main;

        for (int i = 0; i < defs.Length; i++)
        {
            SpawnPlanet(defs[i].planetName, defs[i].tint, planetPos[i]);

            Vector3 spos = planetPos[i] + new Vector3(stationOffset, 0f, 0f);
            Station s = SpawnStation(defs[i].stationName, defs[i].id, defs[i].tint, spos, defs[i].price);
            Stations[i] = s;

            var marker = gameObject.AddComponent<TargetMarker>();
            marker.target = s;
            marker.cam = cam;
        }
    }

    // Equilateral triangle in the XZ plane, centred on the origin.
    Vector3[] TrianglePositions(float leg)
    {
        float circum = leg / Mathf.Sqrt(3f);
        return new[]
        {
            AngleToPos(circum, 90f),
            AngleToPos(circum, 210f),
            AngleToPos(circum, 330f),
        };
    }

    Vector3 AngleToPos(float radius, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
    }

    void SpawnPlanet(string displayName, Color tint, Vector3 position)
    {
        GameObject go = new GameObject(displayName);
        Planet planet = go.AddComponent<Planet>();
        planet.Initialize(displayName, tint, position, planetRadius);
    }

    Station SpawnStation(string displayName, string id, Color tint, Vector3 position, int unitPrice)
    {
        GameObject go = new GameObject(displayName);
        Station station = go.AddComponent<Station>();
        station.Initialize(displayName, id, tint, position, unitPrice);
        return station;
    }
}
```

- [ ] **Step 3: Import and compile.** Prompt the director to focus Unity, then run `execute_menu_item` `Assets/Refresh`, then `recompile_scripts`.

Expected: both new/changed types compile.

- [ ] **Step 4: Confirm a clean console.** Run `get_console_logs` (errors only).

Expected: no compile errors referencing `Planet` or `StationField`.

- [ ] **Step 5: Set the StationField fields in the scene.** In EDIT mode, on the GameObject that has `StationField`, set `separation = 3500`, `planetRadius = 300`, `stationOffset = 600` via `update_component`. Then `save_scene`.

Why: `separation` is a reused field name, so it keeps its old serialized value (1500) until overwritten.

- [ ] **Step 6: Play-test (director).** Enter Play. Verify:
  - Three distinctly coloured planets are visible as large spheres.
  - Each planet has one station floating near it (offset to one side).
  - Three markers are on screen, labelled `Station Helios`, `Station Verdant`, `Station Cobalt`, with edge arrows when a station is off screen.
  - You can fly to any station, see "Press F to dock", dock, and the trade panel buys/sells at that station's price (50 / 65 / 80).

If the planets are too big/small or too close/far, note it for Task 4 tuning. Exit Play.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Station/Planet.cs Assets/Scripts/Station/Planet.cs.meta Assets/Scripts/Station/StationField.cs Assets/Scenes/SampleScene.unity
git commit -m "feat: three-planet system layout with one station per planet"
```

---

## Task 2: Aim-to-focus markers

A station's marker becomes focused (bold + amber) when the ship's nose points at it. Exactly one focused at a time.

**Files:**
- Modify: `Assets/Scripts/Station/TargetMarker.cs`
- Create: `Assets/Scripts/Ship/NavTargeting.cs`
- Scene: add `NavTargeting` to the Ship GameObject

**Interfaces:**
- Consumes: `Station.All`, `Station.EnsureRegistry()`, `TargetMarker.target`.
- Produces: `TargetMarker.Focused` (public bool); `NavTargeting.FocusedStation` (public `Station` get; null when none within the cone); `NavTargeting.aimConeDegrees` (public float).

- [ ] **Step 1: Add the focused flag and style to `TargetMarker.cs`.** Add a public field and change `DrawLabel`.

Add this field below `public Camera cam;`:

```csharp
    /// <summary>Set by NavTargeting when the ship is aiming at this marker's station.</summary>
    public bool Focused = false;
```

Replace the existing `DrawLabel` method with:

```csharp
    void DrawLabel(Vector2 guiPos, string text)
    {
        var style = new GUIStyle(GUI.skin.box) { fontSize = 16 };
        if (Focused)
        {
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = new Color(1f, 0.85f, 0.2f); // amber when focused
        }
        Vector2 size = style.CalcSize(new GUIContent(text));
        var rect = new Rect(guiPos.x - size.x * 0.5f, guiPos.y - size.y * 0.5f, size.x, size.y);
        GUI.Box(rect, text, style);
    }
```

- [ ] **Step 2: Create `NavTargeting.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Decides which station the ship is aiming at (the "focused" target) and tells
/// that station's TargetMarker to show its focused state. The focused station is
/// the one with the smallest angle between the ship's forward direction and the
/// direction to the station, within an aim cone. JumpDrive reads FocusedStation as
/// its jump destination. Lives on the Ship GameObject.
/// </summary>
public class NavTargeting : MonoBehaviour
{
    [Tooltip("Half-angle of the aim cone, in degrees. A station within this angle of where the nose points can be focused.")]
    public float aimConeDegrees = 12f;

    /// <summary>The station the ship is aiming at, or null if none is within the cone.</summary>
    public Station FocusedStation { get; private set; }

    private readonly List<TargetMarker> markers = new List<TargetMarker>();

    void Start()
    {
        RefreshMarkers();
    }

    void RefreshMarkers()
    {
        markers.Clear();
        foreach (var m in FindObjectsByType<TargetMarker>(FindObjectsSortMode.None))
            markers.Add(m);
    }

    void Update()
    {
        Station.EnsureRegistry();
        // Markers are spawned in StationField.Start; if Start order or a domain
        // reload left us empty, rescan.
        if (markers.Count == 0) RefreshMarkers();

        FocusedStation = PickFocused();

        foreach (var m in markers)
        {
            if (m == null) continue;
            m.Focused = (m.target != null && m.target == FocusedStation);
        }
    }

    Station PickFocused()
    {
        Station best = null;
        float bestAngle = aimConeDegrees;
        Vector3 forward = transform.forward;
        foreach (var s in Station.All)
        {
            if (s == null) continue;
            Vector3 toStation = s.transform.position - transform.position;
            if (toStation.sqrMagnitude < 0.0001f) continue;
            float angle = Vector3.Angle(forward, toStation);
            if (angle <= bestAngle)
            {
                bestAngle = angle;
                best = s;
            }
        }
        return best;
    }
}
```

- [ ] **Step 3: Import and compile.** Focus Unity, run `Assets/Refresh`, then `recompile_scripts`.

- [ ] **Step 4: Confirm a clean console** with `get_console_logs` (errors only).

Expected: no errors referencing `NavTargeting` or `TargetMarker`.

- [ ] **Step 5: Add `NavTargeting` to the Ship.** In EDIT mode, add a `NavTargeting` component to the Ship GameObject (the one with `ShipController`/`DockingController`). Then `save_scene`.

- [ ] **Step 6: Play-test (director).** Enter Play. Verify:
  - Point the nose at a station: its marker turns bold and amber.
  - Point away: it returns to normal.
  - With two stations near the centre of view, only one (the nearest to dead-centre) is focused at a time.

Exit Play.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Station/TargetMarker.cs Assets/Scripts/Ship/NavTargeting.cs Assets/Scripts/Ship/NavTargeting.cs.meta Assets/Scenes/SampleScene.unity
git commit -m "feat: aim-to-focus station markers"
```

---

## Task 3: Cruise/jump drive

Aim at a station, press J to lock heading and boost to it; no steering during jump; auto-cutout within range or press J to disengage.

**Files:**
- Modify: `Assets/Scripts/Ship/ShipController.cs`
- Create: `Assets/Scripts/Ship/JumpDrive.cs`
- Scene: add `JumpDrive` to the Ship GameObject

**Interfaces:**
- Consumes: `NavTargeting.FocusedStation`, `DockingController.DockedStation`, `ShipController.maxSpeed`, `ShipController.controlsSuspended`, `Station.transform`, `Station.displayName`.
- Produces: `ShipController.controlsSuspended` (public bool); `JumpDrive.IsJumping` (public bool); `JumpDrive.jumpSpeed`, `JumpDrive.cutoutRange` (public floats).

- [ ] **Step 1: Add the control-suspend hook to `ShipController.cs`.**

Add this field in the `[Header("Boost")]` block (or just below `boostMultiplier`):

```csharp
    [Header("Control")]
    [Tooltip("When true, player steering and thrust are suspended (e.g. during a jump). The Rigidbody is driven elsewhere.")]
    public bool controlsSuspended = false;
```

At the very top of `Update()`, before reading the keyboard, add:

```csharp
        if (controlsSuspended) { look = Vector2.zero; return; }
```

At the very top of `FixedUpdate()`, add:

```csharp
        if (controlsSuspended) return;
```

- [ ] **Step 2: Create `JumpDrive.cs`**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// In-system cruise/jump drive (NOT the Rung 4 inter-system hyperdrive). When the
/// ship is aiming at a station (NavTargeting.FocusedStation), pressing J locks the
/// heading toward that station and boosts to a high cruise speed with no steering.
/// The drive auto-cuts-out within range of the target, or the player presses J to
/// disengage early. Lives on the Ship GameObject. Placeholder prompts via OnGUI.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class JumpDrive : MonoBehaviour
{
    [Tooltip("Cruise speed while jumping, in world units per second.")]
    public float jumpSpeed = 800f;

    [Tooltip("Distance from the target station at which the drive auto-cuts-out, in world units.")]
    public float cutoutRange = 350f;

    private Rigidbody rb;
    private ShipController ship;
    private DockingController docking;
    private NavTargeting nav;

    private bool jumping;
    private Station target;
    private Vector3 heading;

    public bool IsJumping => jumping;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ship = GetComponent<ShipController>();
        docking = GetComponent<DockingController>();
        nav = GetComponent<NavTargeting>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        bool pressJ = kb != null && kb.jKey.wasPressedThisFrame;

        if (jumping)
        {
            if (pressJ) Disengage();
            return;
        }

        // Cannot jump while docked.
        if (docking != null && docking.DockedStation != null) return;

        if (pressJ && nav != null && nav.FocusedStation != null)
            Engage(nav.FocusedStation);
    }

    void FixedUpdate()
    {
        if (!jumping) return;

        // Drive straight along the locked heading at cruise speed.
        rb.linearVelocity = heading * jumpSpeed;

        // Auto-cutout within range of the target.
        if (target == null ||
            (target.transform.position - transform.position).sqrMagnitude <= cutoutRange * cutoutRange)
        {
            Disengage();
        }
    }

    void Engage(Station s)
    {
        jumping = true;
        target = s;
        heading = (s.transform.position - transform.position).normalized;

        // Face the travel direction so the nose points where we go.
        transform.rotation = Quaternion.LookRotation(heading, transform.up);

        // Suspend player steering; JumpDrive drives the Rigidbody during the jump.
        if (ship != null) ship.controlsSuspended = true;
    }

    void Disengage()
    {
        jumping = false;
        target = null;
        // Drop to a calm coasting speed so the cutout does not fling the player
        // past the station; flight assist then bleeds it off.
        if (ship != null)
            rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, ship.maxSpeed);
        if (ship != null) ship.controlsSuspended = false;
    }

    void OnGUI()
    {
        float scale = Mathf.Max(1f, Screen.height / 1080f) * 1.3f;

        string text = null;
        if (jumping && target != null)
            text = "J - Disengage   >> " + target.displayName;
        else if (!jumping && nav != null && nav.FocusedStation != null &&
                 (docking == null || docking.DockedStation == null))
            text = "J - Jump to " + nav.FocusedStation.displayName;

        if (text == null) return;

        var style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(18f * scale) };
        Vector2 size = style.CalcSize(new GUIContent(text));
        // Sit above the docking prompt (which uses Screen.height - 80*scale).
        float y = Screen.height - 140f * scale;
        var rect = new Rect(Screen.width * 0.5f - size.x * 0.5f, y, size.x, size.y);
        GUI.Box(rect, text, style);
    }
}
```

- [ ] **Step 3: Import and compile.** Focus Unity, run `Assets/Refresh`, then `recompile_scripts`.

- [ ] **Step 4: Confirm a clean console** with `get_console_logs` (errors only).

Expected: no errors referencing `JumpDrive` or `ShipController`.

- [ ] **Step 5: Add `JumpDrive` to the Ship.** In EDIT mode, add a `JumpDrive` component to the Ship GameObject. Then `save_scene`.

- [ ] **Step 6: Play-test (director).** Enter Play. Verify:
  - Aim at a station (marker focuses): prompt `J - Jump to [name]` appears.
  - Press J: the ship swings to face the station, accelerates hard, steering is dead, and the prompt becomes `J - Disengage >> [name]`.
  - As you near the station, the drive auto-cuts-out and you return to normal flight within range to fly in and dock.
  - Engage again and press J mid-flight: you drop out of jump immediately and can steer.
  - Dock at a station, then confirm the Jump prompt does NOT appear while docked.

This is feel-work. Note if `jumpSpeed`, `cutoutRange`, or `aimConeDegrees` need tuning. Exit Play.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Ship/ShipController.cs Assets/Scripts/Ship/JumpDrive.cs Assets/Scripts/Ship/JumpDrive.cs.meta Assets/Scenes/SampleScene.unity
git commit -m "feat: in-system cruise/jump drive (aim, lock heading, boost, auto-cutout)"
```

---

## Task 4: Acceptance play-test and feel tuning

No new code unless tuning. This is the step's gate.

**Files:**
- Modify (tuning only, if needed): `Assets/Scripts/Station/StationField.cs` (`separation`, `planetRadius`, `stationOffset`), `Assets/Scripts/Ship/JumpDrive.cs` (`jumpSpeed`, `cutoutRange`), `Assets/Scripts/Ship/NavTargeting.cs` (`aimConeDegrees`). Many of these are also editable live on the components in the scene.

- [ ] **Step 1: Full-loop play-test (director).** Enter Play and run a complete loop across the bigger system:
  - Jump from the start point to a station, dock, buy cargo.
  - Jump to a second station, dock, sell, confirm credits rise.
  - Jump to the third station and back, with at least one manual Disengage.
  - Confirm no console errors during the whole loop (`get_console_logs`).

- [ ] **Step 2: Tune by feel (director-directed).** Adjust the tunables above until: distances feel like a system but not tedious, the jump speed feels good, and the auto-cutout drops you at a comfortable approach distance. Apply final values as defaults in the scripts (so a fresh Play uses them), not just on the scene component.

- [ ] **Step 3: Commit any tuning**

```bash
git add -A
git commit -m "tune: Rung 2 step 1 distances and jump-drive feel"
```

- [ ] **Step 4: Open the PR.** Push the branch and open a PR summarising the three-planet system, focus markers, and jump drive. Reference this plan and the spec.

---

## Self-Review

**Spec coverage:**
- Three planets, one station each → Task 1. ✓
- Planets are non-dockable backdrops, no collider → `Planet.BuildBody` destroys the collider; only `Station`s register in `Station.All`. ✓
- Static orbital stations → Task 1 spawns at fixed offsets; nothing moves them. ✓
- Marker on every station, always on, name + distance, edge arrow → reuses `TargetMarker` (existing behaviour) one per station. ✓ (Distance text is existing `TargetMarker` behaviour; label shows name. If the director wants explicit distance numbers added, that is a small follow-up — current marker shows name only. NOTE: spec said "name and distance"; existing marker shows name only, so this is a known minor gap, flagged for Task 1 play-test.)
- Focused state (bold + colour) by aim → Task 2 (`TargetMarker.Focused` + `NavTargeting`). ✓
- Spread triangle ~3000-4000u, tunable → Task 1 `separation = 3500`, tuned in Task 4. ✓
- Jump: aim-focus, J to lock heading + boost, no steering, auto-cutout, manual disengage, not while docked → Task 3 (`JumpDrive`). ✓
- Not the Rung 4 hyperdrive → in-system only, no scene change. ✓
- Out of scope items untouched → no price-trend/mission/collider code. ✓

**Gap found and resolved:** the spec lists marker "name and distance"; the existing `TargetMarker` draws name only. Rather than expand scope silently, this is flagged in Task 1 Step 6 for the director to decide. If wanted, adding distance is a one-line change to `DrawLabel` (append `" " + Mathf.RoundToInt(Vector3.Distance(cam.transform.position, target.transform.position)) + "m"`). Left out of the core tasks to keep them focused; director confirms during play-test.

**Placeholder scan:** no TBD/TODO; all code blocks are complete.

**Type consistency:** `controlsSuspended`, `FocusedStation`, `Focused`, `IsJumping`, `Initialize` signatures match across tasks. `ship.maxSpeed` is public on `ShipController`. `DockedStation` matches `DockingController`. `Station.All`/`EnsureRegistry` match `Station`.

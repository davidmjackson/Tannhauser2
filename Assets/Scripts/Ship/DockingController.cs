using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Docking for Rung 1. Two states: Flying and Docked. While flying, the nearest
/// station within range shows a "Press F to dock" prompt. Pressing F freezes the
/// ship (Rigidbody kinematic, ShipController disabled) and shows a DOCKED banner.
/// Pressing F again undocks. A short cooldown blocks an accidental instant toggle.
/// Lives on the Ship GameObject. Placeholder UI via OnGUI (no Canvas).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DockingController : MonoBehaviour
{
    [Tooltip("How close the ship must be to a station's dock point to dock, in world units.")]
    public float dockRange = 80f;

    [Tooltip("Seconds after a dock or undock during which F is ignored.")]
    public float toggleCooldown = 0.5f;

    private Rigidbody rb;
    private ShipController ship;
    private Station nearby;   // station in range this frame, or null
    private bool docked;
    private Station dockedAt;
    private float cooldown;

    /// <summary>The station the ship is docked at, or null when flying. Read by TradeController.</summary>
    public Station DockedStation => docked ? dockedAt : null;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ship = GetComponent<ShipController>();
    }

    void Update()
    {
        if (cooldown > 0f) cooldown -= Time.deltaTime;

        var kb = Keyboard.current;
        bool pressF = kb != null && kb.fKey.wasPressedThisFrame && cooldown <= 0f;

        if (docked)
        {
            nearby = null;
            if (pressF) Undock();
            return;
        }

        nearby = FindNearestInRange();
        if (nearby != null && pressF) Dock(nearby);
    }

    void Dock(Station s)
    {
        docked = true;
        dockedAt = s;
        rb.isKinematic = true;        // freeze: kills all linear and angular motion
        if (ship != null) ship.enabled = false;
        // Free the mouse so the player can click the trade panel. Flight locks it
        // to aim the nose; docked, we need a normal pointer.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cooldown = toggleCooldown;
    }

    void Undock()
    {
        docked = false;
        dockedAt = null;
        rb.isKinematic = false;
        if (ship != null) ship.enabled = true;
        // Re-lock the mouse for flight aiming.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cooldown = toggleCooldown;
    }

    Station FindNearestInRange()
    {
        Station.EnsureRegistry(); // self-heal if a domain reload wiped the list
        Station best = null;
        float bestSqr = dockRange * dockRange;
        foreach (var s in Station.All)
        {
            if (s == null) continue;
            float sqr = (s.DockPoint - transform.position).sqrMagnitude;
            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                best = s;
            }
        }
        return best;
    }

    void OnGUI()
    {
        float scale = Mathf.Max(1f, Screen.height / 1080f) * 1.3f;
        if (docked && dockedAt != null)
        {
            DrawCenter(30f * scale, "DOCKED - " + dockedAt.displayName + "   (F to undock)");
            return;
        }
        if (nearby != null)
            DrawCenter(Screen.height - 80f * scale, "Press F to dock");
    }

    void DrawCenter(float y, string text)
    {
        float scale = Mathf.Max(1f, Screen.height / 1080f) * 1.3f;
        var style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(18f * scale) };
        Vector2 size = style.CalcSize(new GUIContent(text));
        var rect = new Rect(Screen.width * 0.5f - size.x * 0.5f, y, size.x, size.y);
        GUI.Box(rect, text, style);
    }
}

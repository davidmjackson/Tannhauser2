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

        if (pressJ && nav != null && CanEngage(nav.FocusedStation))
            Engage(nav.FocusedStation);
    }

    // A jump is offered only to a focused station we are not docked at and are
    // farther from than the auto-cutout range. No point jumping somewhere we have
    // already arrived (it would just instantly cut out again).
    bool CanEngage(Station s)
    {
        if (s == null) return false;
        if (docking != null && docking.DockedStation != null) return false;
        float sqr = (s.transform.position - transform.position).sqrMagnitude;
        return sqr > cutoutRange * cutoutRange;
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
        else if (!jumping && nav != null && CanEngage(nav.FocusedStation))
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

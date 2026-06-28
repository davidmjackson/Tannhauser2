using UnityEngine;

/// <summary>
/// Docking for Rung 1. Finds the nearest station within range and shows a
/// "Press F to dock" prompt. The dock/undock action and ship freeze are added
/// next. Lives on the Ship GameObject alongside ShipController. Placeholder UI
/// drawn with OnGUI, matching TargetMarker (no Canvas).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DockingController : MonoBehaviour
{
    [Tooltip("How close the ship must be to a station's dock point to dock, in world units.")]
    public float dockRange = 80f;

    private Rigidbody rb;
    private ShipController ship;
    private Station nearby; // station in range this frame, or null

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ship = GetComponent<ShipController>();
    }

    void Update()
    {
        nearby = FindNearestInRange();
    }

    Station FindNearestInRange()
    {
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
        if (nearby == null) return;
        DrawCenter(Screen.height - 80f, "Press F to dock");
    }

    void DrawCenter(float y, string text)
    {
        var style = new GUIStyle(GUI.skin.box) { fontSize = 18 };
        Vector2 size = style.CalcSize(new GUIContent(text));
        var rect = new Rect(Screen.width * 0.5f - size.x * 0.5f, y, size.x, size.y);
        GUI.Box(rect, text, style);
    }
}

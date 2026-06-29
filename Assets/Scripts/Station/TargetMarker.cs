using UnityEngine;

/// <summary>
/// Minimal on-screen pointer to a target Station, drawn with OnGUI (no Canvas).
/// Shows a label on the station when it is on screen, or a marker pinned to the
/// screen edge pointing toward it when it is off screen. Rung 1 nav aid only;
/// the full HUD (credits, cargo, buy/sell) stays parked for a later step.
/// </summary>
public class TargetMarker : MonoBehaviour
{
    public Station target;
    public Camera cam;

    /// <summary>Set by NavTargeting when the ship is aiming at this marker's station.</summary>
    public bool Focused = false;

    [Tooltip("Padding from the screen edge for the off-screen marker, in pixels.")]
    public float edgePadding = 40f;

    void OnGUI()
    {
        if (target == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(target.transform.position);
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        bool behind = sp.z < 0f;
        // GUI space is top-left origin; screen point is bottom-left origin.
        Vector2 guiPoint = new Vector2(sp.x, Screen.height - sp.y);

        bool onScreen = !behind &&
            sp.x >= 0f && sp.x <= Screen.width &&
            sp.y >= 0f && sp.y <= Screen.height;

        if (onScreen)
        {
            DrawLabel(guiPoint, "[ " + target.displayName + " ]");
            return;
        }

        // Off screen: direction from center toward target, clamped to a padded edge.
        Vector2 dir = guiPoint - center;
        if (behind) dir = -dir; // flip when the target is behind the camera
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();

        float halfW = Screen.width * 0.5f - edgePadding;
        float halfH = Screen.height * 0.5f - edgePadding;
        float scale = Mathf.Min(
            halfW / Mathf.Max(Mathf.Abs(dir.x), 0.0001f),
            halfH / Mathf.Max(Mathf.Abs(dir.y), 0.0001f));
        Vector2 edgePoint = center + dir * scale;

        DrawLabel(edgePoint, ">> " + target.displayName + " >>");
    }

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
}

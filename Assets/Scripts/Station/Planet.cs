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

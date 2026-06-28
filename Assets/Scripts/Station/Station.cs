using UnityEngine;

/// <summary>
/// One space station, built from primitives at spawn so it reads as a station
/// rather than a plain block. Owns its identity (name, id, tint) and a dock
/// point that the docking step (next) will use. Placeholder art on purpose.
/// </summary>
public class Station : MonoBehaviour
{
    public string displayName = "Station";
    public string id = "STN";
    public Color tint = Color.white;

    [Tooltip("Where a ship docks, as an offset from the station centre.")]
    public Vector3 dockLocalOffset = new Vector3(0f, 0f, 40f);

    /// <summary>World-space point a ship aims for when docking (used later).</summary>
    public Vector3 DockPoint => transform.TransformPoint(dockLocalOffset);

    /// <summary>Configure and build the station. Call right after AddComponent.</summary>
    public void Initialize(string displayName, string id, Color tint, Vector3 position)
    {
        this.displayName = displayName;
        this.id = id;
        this.tint = tint;
        name = displayName;
        transform.position = position;
        BuildBody();
    }

    void BuildBody()
    {
        // Core hub: a tall cylinder.
        AddPart(PrimitiveType.Cylinder, Vector3.zero, new Vector3(8f, 12f, 8f));
        // Ring: a flattened cylinder for a recognizable silhouette.
        AddPart(PrimitiveType.Cylinder, Vector3.zero, new Vector3(26f, 1.5f, 26f));
        // Module boxes so it clearly reads as 'built', not a balloon.
        AddPart(PrimitiveType.Cube, new Vector3(0f, 14f, 0f), new Vector3(6f, 6f, 6f));
        AddPart(PrimitiveType.Cube, new Vector3(0f, -14f, 0f), new Vector3(10f, 4f, 10f));
        AddPart(PrimitiveType.Cube, new Vector3(18f, 0f, 0f), new Vector3(8f, 3f, 3f));
        AddPart(PrimitiveType.Cube, new Vector3(-18f, 0f, 0f), new Vector3(8f, 3f, 3f));
    }

    void AddPart(PrimitiveType type, Vector3 localPos, Vector3 localScale)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.transform.SetParent(transform, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = localScale;

        // Tint the placeholder material. URP Lit uses _BaseColor, not _Color.
        Renderer r = part.GetComponent<Renderer>();
        if (r != null)
        {
            Material m = r.material;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
            else m.color = tint;
        }

        // No colliders yet; the docking step handles approach later.
        Collider col = part.GetComponent<Collider>();
        if (col != null) Destroy(col);
    }
}

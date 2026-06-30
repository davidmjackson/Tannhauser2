using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One space station, built from primitives at spawn so it reads as a station
/// rather than a plain block. Owns its identity (name, id, tint) and a dock
/// point that the docking step uses. Placeholder art on purpose.
/// </summary>
public class Station : MonoBehaviour
{
    /// <summary>All live stations, for nav and docking queries.</summary>
    public static readonly List<Station> All = new List<Station>();

    public string displayName = "Station";
    public string id = "STN";
    public Color tint = Color.white;

    [Tooltip("The good this station produces. It is cheap here; the other goods are dear.")]
    public Commodity homeCommodity = Commodity.Fuel;

    // One moving market per good. Set in Initialize.
    Dictionary<Commodity, PriceCurve> markets = new Dictionary<Commodity, PriceCurve>();

    // Event shocks currently targeting this station (each carries its commodity).
    // The MarketDirector adds shocks here and prunes expired ones.
    readonly List<MarketShock> activeShocks = new List<MarketShock>();

    /// <summary>Read-only view of this station's active shocks (for the news feed).</summary>
    public IReadOnlyList<MarketShock> ActiveShocks => activeShocks;

    [Tooltip("Where a ship docks, as an offset from the station centre.")]
    public Vector3 dockLocalOffset = new Vector3(0f, 0f, 40f);

    /// <summary>World-space point a ship aims for when docking.</summary>
    public Vector3 DockPoint => transform.TransformPoint(dockLocalOffset);

    float Now => Time.timeSinceLevelLoad;

    /// <summary>
    /// Mid-price for good c at time t, with any active event shocks for that good
    /// folded in (summed) before the spread is applied.
    /// </summary>
    float EffectiveMid(Commodity c, float t)
    {
        float mid = markets[c].Mid(t);
        for (int i = 0; i < activeShocks.Count; i++)
            if (activeShocks[i].commodity == c)
                mid += activeShocks[i].Contribution(t);
        return mid;
    }

    /// <summary>Price the player pays to buy one unit of good c right now.</summary>
    public int SellPrice(Commodity c) => markets[c].SellFromMid(EffectiveMid(c, Now));

    /// <summary>Price this station pays the player per unit of good c sold right now.</summary>
    public int BuyPrice(Commodity c) => markets[c].BuyFromMid(EffectiveMid(c, Now));

    /// <summary>Recent price direction for good c: +1 rising, -1 falling, 0 flat.</summary>
    public int PriceTrend(Commodity c)
    {
        float t = Now;
        float look = markets[c].TrendLookbackSeconds;
        return markets[c].TrendFromMids(EffectiveMid(c, t), EffectiveMid(c, t - look));
    }

    /// <summary>Centre (base) price of good c, used to size shock magnitudes.</summary>
    public float BasePrice(Commodity c) => markets[c].basePrice;

    /// <summary>True if an event shock for good c is active right now.</summary>
    public bool HasShock(Commodity c)
    {
        float t = Now;
        for (int i = 0; i < activeShocks.Count; i++)
            if (activeShocks[i].commodity == c && activeShocks[i].IsActive(t))
                return true;
        return false;
    }

    /// <summary>Add a shock targeting this station. Called by the MarketDirector.</summary>
    public void AddShock(MarketShock shock) => activeShocks.Add(shock);

    /// <summary>Drop shocks that have fully decayed. Called by the MarketDirector.</summary>
    public void PruneExpiredShocks(float t) => activeShocks.RemoveAll(sh => t >= sh.EndTime);

    /// <summary>How many shocks are active here right now (for the active-count cap).</summary>
    public int LiveShockCount(float t)
    {
        int n = 0;
        for (int i = 0; i < activeShocks.Count; i++)
            if (activeShocks[i].IsActive(t)) n++;
        return n;
    }

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

    void OnDestroy()
    {
        All.Remove(this);
    }

    /// <summary>
    /// Rebuilds All by scanning the scene, but only when it looks empty.
    /// A Unity domain reload (for example a script recompile during play) wipes
    /// the static list, and stations only register on spawn, so a reload can
    /// leave it empty even though stations still exist. Callers self-heal by
    /// calling this. No-op in normal play once the list is populated.
    /// </summary>
    public static void EnsureRegistry()
    {
        if (All.Count > 0) return;
        foreach (var s in FindObjectsByType<Station>(FindObjectsSortMode.None))
            if (!All.Contains(s)) All.Add(s);
    }
}

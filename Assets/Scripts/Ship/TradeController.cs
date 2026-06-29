using UnityEngine;

/// <summary>
/// Player economy. Holds credits and a single cargo type. Buys cargo at the
/// docked station's live SellPrice and sells at its live BuyPrice (the station's
/// buy/sell spread means a same-station round trip loses money, so profit needs
/// travel). Draws a placeholder OnGUI readout plus, when docked, a trade panel
/// showing the market trend and both prices. Lives on the Ship GameObject
/// alongside DockingController. Placeholder UI on purpose (no Canvas).
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

    bool CanBuy(Station s) => s != null && credits >= s.SellPrice && cargoUnits < cargoCapacity;
    bool CanSell(Station s) => s != null && cargoUnits > 0;

    void Buy(Station s)
    {
        if (!CanBuy(s)) return;
        credits -= s.SellPrice;
        cargoUnits += 1;
    }

    void Sell(Station s)
    {
        if (!CanSell(s)) return;
        credits += s.BuyPrice;
        cargoUnits -= 1;
    }

    void OnGUI()
    {
        // OnGUI uses fixed pixel sizes, so scale everything up on high-res displays
        // (reference height 1080). Keeps the placeholder UI readable at any resolution.
        float scale = Mathf.Max(1f, Screen.height / 1080f) * 1.3f;

        // Always-on corner readout.
        string readout = "Credits: " + credits + "\nCargo: " + cargoUnits + "/" + cargoCapacity;
        var style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(16f * scale), alignment = TextAnchor.UpperLeft };
        GUI.Box(new Rect(10f * scale, 10f * scale, 200f * scale, 60f * scale), readout, style);

        // Docked trade panel.
        Station s = docking != null ? docking.DockedStation : null;
        if (s == null) return;

        float w = 260f * scale;
        float x = Screen.width * 0.5f - w * 0.5f;
        float y = 80f * scale;
        float rowH = 38f * scale;
        float pad = 4f * scale;
        var pstyle = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(15f * scale) };

        // Trend cue (ASCII so it renders in the default IMGUI font).
        string trend = s.PriceTrend > 0 ? "^ rising"
                     : s.PriceTrend < 0 ? "v falling"
                     : "- steady";
        GUI.Box(new Rect(x, y, w, rowH), "Market: " + trend, pstyle);

        // Both live prices. "Buy" is what you pay, "Sell" is what the station pays you.
        GUI.Box(new Rect(x, y + (rowH + pad), w, rowH),
            "Buy @ " + s.SellPrice + "    Sell @ " + s.BuyPrice, pstyle);

        var bstyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(15f * scale) };
        float by = y + (rowH + pad) * 2f;
        GUI.enabled = CanBuy(s);
        if (GUI.Button(new Rect(x, by, w * 0.5f - pad, rowH), "Buy", bstyle)) Buy(s);
        GUI.enabled = CanSell(s);
        if (GUI.Button(new Rect(x + w * 0.5f + pad, by, w * 0.5f - pad, rowH), "Sell", bstyle)) Sell(s);
        GUI.enabled = true;
    }
}

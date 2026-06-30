using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player economy. Holds credits and a shared cargo hold tracked per commodity
/// (all goods compete for one capacity). Buys a good at the docked station's live
/// SellPrice and sells at its BuyPrice. With station specialization plus the
/// buy/sell spread, profit comes from carrying a station's cheap home good to a
/// station that pays dearly for it. Draws a placeholder OnGUI readout plus, when
/// docked, a trade panel with one row per good. Lives on the Ship GameObject
/// alongside DockingController. Placeholder UI on purpose (no Canvas).
/// </summary>
[RequireComponent(typeof(DockingController))]
public class TradeController : MonoBehaviour
{
    [Tooltip("Starting credits.")]
    public int credits = 1000;

    [Tooltip("Maximum total cargo units across all goods.")]
    public int cargoCapacity = 10;

    // Units held of each good. All goods share the single cargoCapacity.
    private readonly Dictionary<Commodity, int> hold = new Dictionary<Commodity, int>();

    private DockingController docking;

    void Awake()
    {
        docking = GetComponent<DockingController>();
        foreach (var c in Commodities.All)
            hold[c] = 0;
    }

    int TotalUnits
    {
        get
        {
            int sum = 0;
            foreach (var c in Commodities.All) sum += hold[c];
            return sum;
        }
    }

    bool CanBuy(Station s, Commodity c) => s != null && credits >= s.SellPrice(c) && TotalUnits < cargoCapacity;
    bool CanSell(Station s, Commodity c) => s != null && hold[c] > 0;

    void Buy(Station s, Commodity c)
    {
        if (!CanBuy(s, c)) return;
        credits -= s.SellPrice(c);
        hold[c] += 1;
    }

    void Sell(Station s, Commodity c)
    {
        if (!CanSell(s, c)) return;
        credits += s.BuyPrice(c);
        hold[c] -= 1;
    }

    void OnGUI()
    {
        // OnGUI uses fixed pixel sizes, so scale up on high-res displays
        // (reference height 1080). Keeps the placeholder UI readable.
        float scale = Mathf.Max(1f, Screen.height / 1080f) * 1.3f;

        // Always-on corner readout: credits, total cargo, per-good breakdown.
        string cargoLine = "";
        foreach (var c in Commodities.All)
            cargoLine += Commodities.DisplayName(c) + " " + hold[c] + "   ";
        string readout = "Credits: " + credits
                       + "\nCargo " + TotalUnits + "/" + cargoCapacity
                       + "\n" + cargoLine.TrimEnd();
        var style = new GUIStyle(GUI.skin.box)
        {
            fontSize = Mathf.RoundToInt(15f * scale),
            alignment = TextAnchor.UpperLeft
        };
        GUI.Box(new Rect(10f * scale, 10f * scale, 340f * scale, 92f * scale), readout, style);

        // Docked trade panel: one row per good.
        Station s = docking != null ? docking.DockedStation : null;
        if (s == null) return;

        float w = 540f * scale;
        float x = Screen.width * 0.5f - w * 0.5f;
        float y = 80f * scale;
        float rowH = 40f * scale;
        float pad = 4f * scale;
        float btnW = 60f * scale;
        float infoW = w - (btnW * 2f + pad * 3f);

        var hstyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = Mathf.RoundToInt(15f * scale),
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Box(new Rect(x, y, w, rowH), "Market - " + s.displayName, hstyle);

        var istyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = Mathf.RoundToInt(13f * scale),
            alignment = TextAnchor.MiddleLeft
        };
        var bstyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(14f * scale) };

        for (int i = 0; i < Commodities.All.Length; i++)
        {
            Commodity c = Commodities.All[i];
            float ry = y + (rowH + pad) * (i + 1);

            int tr = s.PriceTrend(c);
            string trend = tr > 0 ? "^" : tr < 0 ? "v" : "-";
            string tag = s.Produces(c) ? " (produced here)" : "";
            string cue = s.HasShock(c) ? "  !" : "";
            string info = " " + Commodities.DisplayName(c) + tag + cue
                        + "    Buy " + s.SellPrice(c)
                        + "    Sell " + s.BuyPrice(c)
                        + "    " + trend
                        + "    held " + hold[c];
            GUI.Box(new Rect(x, ry, infoW, rowH), info, istyle);

            GUI.enabled = CanBuy(s, c);
            if (GUI.Button(new Rect(x + infoW + pad, ry, btnW, rowH), "Buy", bstyle)) Buy(s, c);
            GUI.enabled = CanSell(s, c);
            if (GUI.Button(new Rect(x + infoW + pad * 2f + btnW, ry, btnW, rowH), "Sell", bstyle)) Sell(s, c);
            GUI.enabled = true;
        }

        // News section: system-wide active headlines, below the market rows.
        float newsY = y + (rowH + pad) * (Commodities.All.Length + 1) + pad * 2f;

        var newsHeaderStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = Mathf.RoundToInt(15f * scale),
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Box(new Rect(x, newsY, w, rowH), "Market News", newsHeaderStyle);

        var newsStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = Mathf.RoundToInt(13f * scale),
            alignment = TextAnchor.MiddleLeft
        };

        MarketDirector director = MarketDirector.Get();
        var headlines = director != null ? director.ActiveHeadlines() : null;

        if (headlines == null || headlines.Count == 0)
        {
            float ny = newsY + (rowH + pad);
            GUI.Box(new Rect(x, ny, w, rowH), " No market news.", newsStyle);
        }
        else
        {
            for (int i = 0; i < headlines.Count; i++)
            {
                float ny = newsY + (rowH + pad) * (i + 1);
                GUI.Box(new Rect(x, ny, w, rowH), " " + headlines[i], newsStyle);
            }
        }
    }
}

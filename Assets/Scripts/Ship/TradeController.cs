using UnityEngine;

/// <summary>
/// Player economy for Rung 1. Holds credits and a single cargo type, and draws a
/// placeholder trade UI via OnGUI. Always shows a corner readout of credits and
/// cargo. The docked Buy/Sell panel is added in Task 4. Lives on the Ship
/// GameObject alongside DockingController. Placeholder UI on purpose (no Canvas).
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

    void OnGUI()
    {
        // Always-on corner readout.
        string readout = "Credits: " + credits + "\nCargo: " + cargoUnits + "/" + cargoCapacity;
        var style = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.UpperLeft };
        GUI.Box(new Rect(10f, 10f, 170f, 52f), readout, style);
    }
}

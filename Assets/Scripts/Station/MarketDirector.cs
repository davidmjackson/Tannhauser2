using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Schedules one-off market shocks and owns the news feed. On a timer, while
/// fewer than maxActiveShocks are live, it picks a random station, good, and
/// direction, sizes a signed magnitude from the good's base price, writes a plain
/// headline, and hands the shock to the target station (which folds it into its
/// price). Each frame it also prunes shocks that have fully decayed.
///
/// Stations are read from the self-healing Station.All registry. This director
/// also self-heals its own static Instance so a domain reload during play (which
/// wipes statics) is recovered by re-finding the scene object.
/// </summary>
public class MarketDirector : MonoBehaviour
{
    [Tooltip("Shortest and longest gap between new shocks, in seconds.")]
    public float spawnIntervalMin = 30f;
    public float spawnIntervalMax = 60f;

    [Tooltip("Most shocks allowed active across the whole system at once.")]
    public int maxActiveShocks = 2;

    [Tooltip("Lead time from headline to peak, in seconds (player's warning window).")]
    public float riseDuration = 12f;

    [Tooltip("Time from peak back to normal, in seconds.")]
    public float decayDuration = 12f;

    [Tooltip("Peak deviation as a fraction of the good's base price. Larger than the " +
             "~20% natural wave so a shock clearly dominates while active.")]
    public float magnitudeFractionMin = 0.45f;
    public float magnitudeFractionMax = 0.70f;

    [Tooltip("Chance a shock is an upward spike (sell-into). The rest are crashes (buy-into).")]
    [Range(0f, 1f)] public float spikeProbability = 0.5f;

    static MarketDirector instance;

    // Next time (timeSinceLevelLoad) a shock is allowed to spawn.
    float nextSpawnTime;

    /// <summary>
    /// Self-healing accessor. Returns the scene's director, re-finding it if a
    /// domain reload cleared the static reference. Null if none exists.
    /// </summary>
    public static MarketDirector Get()
    {
        if (instance == null) instance = FindFirstObjectByType<MarketDirector>();
        return instance;
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // First shock after one normal interval, so the market opens calm.
        nextSpawnTime = Time.timeSinceLevelLoad + Random.Range(spawnIntervalMin, spawnIntervalMax);
    }

    void Update()
    {
        float t = Time.timeSinceLevelLoad;
        Station.EnsureRegistry();

        // Prune decayed shocks everywhere.
        for (int i = 0; i < Station.All.Count; i++)
            Station.All[i].PruneExpiredShocks(t);

        // Spawn a new shock if it is time and we are under the active cap.
        if (t >= nextSpawnTime && ActiveCount(t) < maxActiveShocks)
        {
            ScheduleShock(t);
            nextSpawnTime = t + Random.Range(spawnIntervalMin, spawnIntervalMax);
        }
    }

    int ActiveCount(float t)
    {
        int n = 0;
        for (int i = 0; i < Station.All.Count; i++)
            n += Station.All[i].LiveShockCount(t);
        return n;
    }

    void ScheduleShock(float t)
    {
        if (Station.All.Count == 0) return;

        Station target = Station.All[Random.Range(0, Station.All.Count)];
        Commodity c = Commodities.All[Random.Range(0, Commodities.All.Length)];
        bool spike = Random.value < spikeProbability;

        float frac = Random.Range(magnitudeFractionMin, magnitudeFractionMax);
        float magnitude = target.BasePrice(c) * frac * (spike ? 1f : -1f);

        var shock = new MarketShock
        {
            commodity = c,
            headline = BuildHeadline(target, c, spike),
            announceTime = t,
            riseDuration = riseDuration,
            decayDuration = decayDuration,
            magnitude = magnitude,
        };
        target.AddShock(shock);
    }

    // Plain-language headline naming the good, station, and direction (always true).
    static string BuildHeadline(Station s, Commodity c, bool spike)
    {
        string good = Commodities.DisplayName(c);
        if (spike)
            return good + " shortage at " + s.displayName + " (prices spiking, sell here)";
        return good + " glut at " + s.displayName + " (prices crashing, buy here)";
    }

    /// <summary>Union of active headlines across the system, for the docked news panel.</summary>
    public List<string> ActiveHeadlines()
    {
        var lines = new List<string>();
        float t = Time.timeSinceLevelLoad;
        Station.EnsureRegistry();
        for (int i = 0; i < Station.All.Count; i++)
        {
            var shocks = Station.All[i].ActiveShocks;
            for (int j = 0; j < shocks.Count; j++)
                if (shocks[j].IsActive(t)) lines.Add(shocks[j].headline);
        }
        return lines;
    }
}

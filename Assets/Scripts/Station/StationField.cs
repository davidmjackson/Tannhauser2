using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns the Rung 2 system at play time: three planets as backdrops, each with
/// one static station nearby, plus one on-screen TargetMarker per station.
/// Replaces the Rung 1 two-station setup. Code-spawned (in-Editor placement is
/// unreliable in this project). Planets are visual only; stations keep the Rung 1
/// docking and trade behaviour.
/// </summary>
public class StationField : MonoBehaviour
{
    [Tooltip("Triangle leg length between planets, in world units.")]
    public float separation = 3500f;

    [Tooltip("Radius of each planet body, in world units.")]
    public float planetRadius = 300f;

    [Tooltip("Station distance from its planet centre, in world units.")]
    public float stationOffset = 600f;

    [Header("Market (price trends)")]
    [Tooltip("Wave swing as a fraction of base price.")]
    public float amplitudeFraction = 0.20f;

    [Tooltip("Light noise swing as a fraction of base price.")]
    public float noiseFraction = 0.05f;

    [Tooltip("Buy/sell spread as a fraction of base price. Kept below the gap between station base prices so travel still pays.")]
    public float spreadFraction = 0.18f;

    [Tooltip("Shortest and longest wave period (seconds). Each station gets a value spread across this range.")]
    public float periodMin = 22f;
    public float periodMax = 38f;

    [Tooltip("How fast the noise drifts.")]
    public float noiseScale = 0.04f;

    [Tooltip("Hard floor so prices never reach zero, in credits.")]
    public float priceFloor = 5f;

    [Tooltip("Base price of a station's own (home) good. Low, so it is cheap to buy where produced.")]
    public float homeBasePrice = 40f;

    [Tooltip("Base price of goods not produced at a station. High, so they are dear to sell into.")]
    public float foreignBasePrice = 90f;

    [Header("Market (event shocks)")]
    [Tooltip("Shortest and longest gap between news-driven price shocks, in seconds.")]
    public float shockIntervalMin = 30f;
    public float shockIntervalMax = 60f;

    [Tooltip("Most shocks active across the system at once.")]
    public int maxActiveShocks = 2;

    public Station[] Stations { get; private set; }

    struct Def
    {
        public string planetName, stationName, id;
        public Color tint;
        public Commodity home;
        public Def(string planetName, string stationName, string id, Color tint, Commodity home)
        {
            this.planetName = planetName; this.stationName = stationName;
            this.id = id; this.tint = tint; this.home = home;
        }
    }

    void Start()
    {
        var defs = new[]
        {
            new Def("Helios",  "Station Helios",  "STN-H", new Color(1f, 0.55f, 0.15f),  Commodity.Fuel),
            new Def("Verdant", "Station Verdant", "STN-V", new Color(0.35f, 0.8f, 0.45f), Commodity.Grain),
            new Def("Cobalt",  "Station Cobalt",  "STN-C", new Color(0.3f, 0.6f, 1f),     Commodity.Electronics),
        };

        Vector3[] planetPos = TrianglePositions(separation);
        Stations = new Station[defs.Length];
        var cam = Camera.main;

        for (int i = 0; i < defs.Length; i++)
        {
            SpawnPlanet(defs[i].planetName, defs[i].tint, planetPos[i]);

            Vector3 spos = planetPos[i] + new Vector3(stationOffset, 0f, 0f);
            var markets = BuildMarkets(defs[i].home, i, defs.Length);
            Station s = SpawnStation(defs[i].stationName, defs[i].id, defs[i].tint, spos, defs[i].home, markets);
            Stations[i] = s;

            var marker = gameObject.AddComponent<TargetMarker>();
            marker.target = s;
            marker.cam = cam;
        }

        // Stations exist now, so start the news/shock director. It reads the
        // self-healing Station.All registry, so it needs no station list passed in.
        var director = gameObject.AddComponent<MarketDirector>();
        director.spawnIntervalMin = shockIntervalMin;
        director.spawnIntervalMax = shockIntervalMax;
        director.maxActiveShocks = maxActiveShocks;
    }

    // Equilateral triangle in the XZ plane, centred on the origin.
    Vector3[] TrianglePositions(float leg)
    {
        float circum = leg / Mathf.Sqrt(3f);
        return new[]
        {
            AngleToPos(circum, 90f),
            AngleToPos(circum, 210f),
            AngleToPos(circum, 330f),
        };
    }

    Vector3 AngleToPos(float radius, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
    }

    void SpawnPlanet(string displayName, Color tint, Vector3 position)
    {
        GameObject go = new GameObject(displayName);
        Planet planet = go.AddComponent<Planet>();
        planet.Initialize(displayName, tint, position, planetRadius);
    }

    Station SpawnStation(string displayName, string id, Color tint, Vector3 position,
                         Commodity home, Dictionary<Commodity, PriceCurve> markets)
    {
        GameObject go = new GameObject(displayName);
        Station station = go.AddComponent<Station>();
        station.Initialize(displayName, id, tint, position, home, markets);
        return station;
    }

    // Build a moving market for each good at one station. The home good gets the
    // low base price (cheap to buy here); the others get the high base price (dear
    // to sell into). Each (station, good) gets a distinct phase/period/seed from a
    // lane index (0..stationCount*goodCount-1) so all the markets drift out of step.
    Dictionary<Commodity, PriceCurve> BuildMarkets(Commodity home, int stationIndex, int stationCount)
    {
        var goods = Commodities.All;
        int laneCount = stationCount * goods.Length;
        var dict = new Dictionary<Commodity, PriceCurve>();
        for (int g = 0; g < goods.Length; g++)
        {
            Commodity c = goods[g];
            float basePrice = (c == home) ? homeBasePrice : foreignBasePrice;
            int lane = stationIndex * goods.Length + g;
            float frac = laneCount > 1 ? (float)lane / laneCount : 0f;
            dict[c] = new PriceCurve
            {
                basePrice = basePrice,
                amplitude = basePrice * amplitudeFraction,
                period = Mathf.Lerp(periodMin, periodMax, frac),
                phase = frac,
                noiseAmplitude = basePrice * noiseFraction,
                noiseScale = noiseScale,
                seed = lane * 13.7f + 1f,
                spread = basePrice * spreadFraction,
                priceFloor = priceFloor,
            };
        }
        return dict;
    }
}

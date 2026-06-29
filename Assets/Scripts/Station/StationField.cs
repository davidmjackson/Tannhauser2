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

    public Station[] Stations { get; private set; }

    struct Def
    {
        public string planetName, stationName, id;
        public Color tint;
        public int basePrice;
        public Def(string planetName, string stationName, string id, Color tint, int basePrice)
        {
            this.planetName = planetName; this.stationName = stationName;
            this.id = id; this.tint = tint; this.basePrice = basePrice;
        }
    }

    void Start()
    {
        var defs = new[]
        {
            new Def("Helios",  "Station Helios",  "STN-H", new Color(1f, 0.55f, 0.15f),  50),
            new Def("Verdant", "Station Verdant", "STN-V", new Color(0.35f, 0.8f, 0.45f), 65),
            new Def("Cobalt",  "Station Cobalt",  "STN-C", new Color(0.3f, 0.6f, 1f),     80),
        };

        Vector3[] planetPos = TrianglePositions(separation);
        Stations = new Station[defs.Length];
        var cam = Camera.main;

        for (int i = 0; i < defs.Length; i++)
        {
            SpawnPlanet(defs[i].planetName, defs[i].tint, planetPos[i]);

            Vector3 spos = planetPos[i] + new Vector3(stationOffset, 0f, 0f);
            PriceCurve market = BuildMarket(defs[i].basePrice, i, defs.Length);
            Station s = SpawnStation(defs[i].stationName, defs[i].id, defs[i].tint, spos, market);
            Stations[i] = s;

            var marker = gameObject.AddComponent<TargetMarker>();
            marker.target = s;
            marker.cam = cam;
        }
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

    Station SpawnStation(string displayName, string id, Color tint, Vector3 position, PriceCurve market)
    {
        GameObject go = new GameObject(displayName);
        Station station = go.AddComponent<Station>();
        station.Initialize(displayName, id, tint, position, market);
        return station;
    }

    // Build a moving market for one station. Period and phase are spread across
    // the stations (by index) so the three markets do not move in lockstep.
    PriceCurve BuildMarket(int basePrice, int index, int count)
    {
        float frac = count > 1 ? (float)index / count : 0f;
        return new PriceCurve
        {
            basePrice = basePrice,
            amplitude = basePrice * amplitudeFraction,
            period = Mathf.Lerp(periodMin, periodMax, frac),
            phase = frac,
            noiseAmplitude = basePrice * noiseFraction,
            noiseScale = noiseScale,
            seed = index * 13.7f + 1f,
            spread = basePrice * spreadFraction,
            priceFloor = priceFloor,
        };
    }
}

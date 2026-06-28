using UnityEngine;

/// <summary>
/// Spawns the two Rung 1 stations at play time and keeps references to them.
/// Mirrors ReferenceField's code-spawn approach (in-Editor placement is
/// unreliable here). The on-screen TargetMarker is attached in Task 2.
/// </summary>
public class StationField : MonoBehaviour
{
    [Tooltip("Distance between Station A and Station B, in world units.")]
    public float separation = 1500f;

    [Tooltip("Tint for Station A.")]
    public Color colorA = new Color(0.3f, 0.6f, 1f);    // blue
    [Tooltip("Tint for Station B.")]
    public Color colorB = new Color(1f, 0.55f, 0.15f);  // orange

    public Station StationA { get; private set; }
    public Station StationB { get; private set; }

    void Start()
    {
        StationA = Spawn("Station A", "STN-A", colorA, Vector3.zero);
        StationB = Spawn("Station B", "STN-B", colorB, new Vector3(0f, 0f, separation));

        var marker = gameObject.AddComponent<TargetMarker>();
        marker.target = StationB;
        marker.cam = Camera.main;
    }

    Station Spawn(string displayName, string id, Color tint, Vector3 position)
    {
        GameObject go = new GameObject(displayName);
        Station station = go.AddComponent<Station>();
        station.Initialize(displayName, id, tint, position);
        return station;
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Decides which station the ship is aiming at (the "focused" target) and tells
/// that station's TargetMarker to show its focused state. The focused station is
/// the one with the smallest angle between the ship's forward direction and the
/// direction to the station, within an aim cone. JumpDrive reads FocusedStation as
/// its jump destination. Lives on the Ship GameObject.
/// </summary>
public class NavTargeting : MonoBehaviour
{
    [Tooltip("Half-angle of the aim cone, in degrees. A station within this angle of where the nose points can be focused.")]
    public float aimConeDegrees = 12f;

    /// <summary>The station the ship is aiming at, or null if none is within the cone.</summary>
    public Station FocusedStation { get; private set; }

    private readonly List<TargetMarker> markers = new List<TargetMarker>();

    void Start()
    {
        RefreshMarkers();
    }

    void RefreshMarkers()
    {
        markers.Clear();
        foreach (var m in FindObjectsByType<TargetMarker>(FindObjectsSortMode.None))
            markers.Add(m);
    }

    void Update()
    {
        Station.EnsureRegistry();
        // Markers are spawned in StationField.Start; if Start order or a domain
        // reload left us empty, rescan.
        if (markers.Count == 0) RefreshMarkers();

        FocusedStation = PickFocused();

        foreach (var m in markers)
        {
            if (m == null) continue;
            m.Focused = (m.target != null && m.target == FocusedStation);
        }
    }

    Station PickFocused()
    {
        Station best = null;
        float bestAngle = aimConeDegrees;
        Vector3 forward = transform.forward;
        foreach (var s in Station.All)
        {
            if (s == null) continue;
            Vector3 toStation = s.transform.position - transform.position;
            if (toStation.sqrMagnitude < 0.0001f) continue;
            float angle = Vector3.Angle(forward, toStation);
            if (angle <= bestAngle)
            {
                bestAngle = angle;
                best = s;
            }
        }
        return best;
    }
}

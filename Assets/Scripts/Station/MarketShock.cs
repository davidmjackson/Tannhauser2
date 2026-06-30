using UnityEngine;

/// <summary>
/// One market event: a temporary, signed bump on a single good's mid-price at a
/// single station. Pure value object (no Unity scene dependency, no per-frame
/// state), so the same time t in always gives the same contribution out, matching
/// the deterministic PriceCurve design.
///
/// The pulse eases up from zero at announceTime, peaks at `magnitude` after
/// riseDuration (the lead time the player gets from the headline), then eases back
/// to zero over decayDuration. A positive magnitude is a price spike (sell into
/// it); a negative magnitude is a crash (buy into it). Which station the shock
/// targets is tracked by which station's list it lives in, not stored here.
/// </summary>
public class MarketShock
{
    public Commodity commodity;   // which good this shock moves
    public string headline;       // e.g. "Fuel shortage at Station Helios (...)"
    public float announceTime;    // t when it appears and the ramp starts
    public float riseDuration;    // ramp time, announce -> peak (the lead time)
    public float decayDuration;   // peak -> back to normal
    public float magnitude;       // signed peak deviation in credits (+spike / -crash)

    /// <summary>t at which the deviation reaches its full magnitude.</summary>
    public float PeakTime => announceTime + riseDuration;

    /// <summary>t at which the shock has fully decayed back to normal.</summary>
    public float EndTime => PeakTime + decayDuration;

    /// <summary>True while the shock is contributing anything to the price.</summary>
    public bool IsActive(float t) => t >= announceTime && t < EndTime;

    /// <summary>
    /// Signed deviation to add to the mid-price at time t. Zero outside the active
    /// window; a smoothstep ramp up to `magnitude` at the peak, then a smoothstep
    /// ramp back down to zero. Durations of zero collapse to an instant edge
    /// (no divide-by-zero).
    /// </summary>
    public float Contribution(float t)
    {
        if (t < announceTime || t >= EndTime) return 0f;

        if (t < PeakTime)
        {
            float u = riseDuration > 0f ? (t - announceTime) / riseDuration : 1f;
            return magnitude * Mathf.SmoothStep(0f, 1f, u);
        }

        float d = decayDuration > 0f ? (t - PeakTime) / decayDuration : 1f;
        return magnitude * (1f - Mathf.SmoothStep(0f, 1f, d));
    }
}

using UnityEngine;

/// <summary>
/// The moving market price for one station. A pure function of time: given a
/// time t (in seconds), it returns the current prices. No per-frame state, so it
/// is deterministic (same t in, same prices out) and can be reasoned about and
/// tested in isolation.
///
/// The mid-price is a baseline plus a slow sine wave (the learnable trend) plus
/// light coherent noise (Perlin, so it drifts smoothly instead of jittering each
/// frame). Buy and sell prices straddle the mid by half the spread, so a station
/// always pays less to buy cargo from the player than it charges to sell cargo to
/// the player. That makes a same-station round trip lose the spread, so travel is
/// required for profit.
/// </summary>
public class PriceCurve
{
    [Tooltip("Centre price the market oscillates around, in credits.")]
    public float basePrice = 50f;

    [Tooltip("Wave swing above and below the base, in credits.")]
    public float amplitude = 10f;

    [Tooltip("Seconds for one full wave cycle.")]
    public float period = 30f;

    [Tooltip("Wave start offset, as a fraction (0..1) of a cycle.")]
    public float phase = 0f;

    [Tooltip("Noise swing, in credits.")]
    public float noiseAmplitude = 2.5f;

    [Tooltip("How fast the noise drifts (larger = faster).")]
    public float noiseScale = 0.04f;

    [Tooltip("Per-station offset so each station's noise differs.")]
    public float seed = 0f;

    [Tooltip("Gap between buy and sell price, in credits.")]
    public float spread = 9f;

    [Tooltip("Prices never drop below this, in credits.")]
    public float priceFloor = 5f;

    // Seconds to look back when measuring trend direction.
    const float TrendLookback = 1.5f;
    // Mid-price change smaller than this (credits) reads as flat.
    const float TrendDeadband = 0.05f;

    /// <summary>The mid-price at time t.</summary>
    public float Mid(float t)
    {
        float wave = amplitude * Mathf.Sin(2f * Mathf.PI * (t / period + phase));
        // PerlinNoise returns 0..1; recentre to about -1..1, then scale.
        float n = (Mathf.PerlinNoise(seed, t * noiseScale) - 0.5f) * 2f;
        return basePrice + wave + noiseAmplitude * n;
    }

    /// <summary>Price the player pays to buy one unit (mid plus half the spread).</summary>
    public int SellPriceToPlayer(float t)
    {
        return Mathf.RoundToInt(Mathf.Max(priceFloor, Mid(t) + spread * 0.5f));
    }

    /// <summary>Price the station pays the player per unit sold (mid minus half the spread).</summary>
    public int BuyPriceFromPlayer(float t)
    {
        return Mathf.RoundToInt(Mathf.Max(priceFloor, Mid(t) - spread * 0.5f));
    }

    /// <summary>Recent direction of the mid-price: +1 rising, -1 falling, 0 flat.</summary>
    public int Trend(float t)
    {
        float delta = Mid(t) - Mid(t - TrendLookback);
        if (delta > TrendDeadband) return 1;
        if (delta < -TrendDeadband) return -1;
        return 0;
    }
}

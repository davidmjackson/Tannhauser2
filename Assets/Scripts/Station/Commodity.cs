/// <summary>
/// The tradeable goods. Each station produces exactly one of these (its home
/// good), which is cheap where produced and dear elsewhere.
/// </summary>
public enum Commodity
{
    Fuel,
    Grain,
    Electronics,
}

/// <summary>Helpers for enumerating and naming commodities.</summary>
public static class Commodities
{
    /// <summary>All goods, in display order.</summary>
    public static readonly Commodity[] All =
    {
        Commodity.Fuel,
        Commodity.Grain,
        Commodity.Electronics,
    };

    /// <summary>Human-readable name for a good.</summary>
    public static string DisplayName(Commodity c)
    {
        switch (c)
        {
            case Commodity.Fuel: return "Fuel";
            case Commodity.Grain: return "Grain";
            case Commodity.Electronics: return "Electronics";
            default: return c.ToString();
        }
    }
}

using System.Globalization;

namespace AVS.Util;

/// <summary>
/// Contains extension methods for common operations involving float conversions and percentage calculations.
/// </summary>
public static class CommonExtensions
{
    /// <summary>
    /// Converts a float into a string using the universal decimal sign (.)
    /// </summary>
    /// <param name="f">Float to convert</param>
    /// <returns>Converted float</returns>
    public static string ToStr(this float f)
    {
        return f.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parses a string into a float using the universal decimal sign (.)
    /// </summary>
    /// <param name="s">String to try parse</param>
    /// <param name="f">Resulting float</param>
    /// <returns>True on success</returns>
    public static bool ToFloat(this string s, out float f)
    {
        return float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out f);
    }

    /// <summary>
    /// Converts a string into a float using the universal decimal sign (.)
    /// </summary>
    /// <param name="s">String to parse</param>
    /// <returns>Converted float</returns>
    public static float ToFloat(this string s)
    {
        return float.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Produces a percentage string from a float value.
    /// If the max value is zero, it returns "-%".
    /// Rounds the percentage to two decimal places.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="max"></param>
    /// <returns>String in the form "[v]%" where [v] is either 1.23 or - </returns>
    public static string Percentage(this float x, float max)
    {
        if (max == 0f) return "-%";

        return (x / max).ToString("#.#%", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Produces a percentage string from a LiveMixin's current health status.
    /// </summary>
    /// <remarks>
    /// If the <paramref name="live"/> is null, has no max health or is invincible,
    /// it returns "-%".
    /// </remarks>
    /// <param name="live">Live mixin to produce the health percent of</param>
    /// <returns>String in the form "[v]%" where [v] is either 1.23 or - </returns>
    public static string Percentage(this LiveMixin? live)
    {
        if (live == null) return "-%";
        if (live.maxHealth <= 0 || live.invincible) return "-%";
        return (live.health / live.maxHealth).ToString("#.#%", CultureInfo.CurrentCulture);
    }
}
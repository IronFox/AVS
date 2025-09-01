using AVS.Interfaces;
using AVS.Util;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AVS.Log
{
    /// <summary>
    /// Marker type used to represent an omitted parameter slot in <see cref="LogParameters{T0, T1, T2, T3}"/>.
    /// </summary>
    public readonly struct Void { }

    /// <summary>
    /// Provides a set of static methods for creating and working with strongly-typed log parameter objects.
    /// </summary>
    public static class Params
    {
        /// <summary>
        /// Shorthand for no parameters.
        /// </summary>
        public static LogParameters? None => null;

        /// <summary>
        /// Creates a strongly-typed parameter container with one value.
        /// </summary>
        /// <typeparam name="T0">The type of the first parameter.</typeparam>
        /// <param name="p0">The first parameter value.</param>
        /// <returns>A <see cref="LogParameters{T0, T1, T2, T3}"/> instance containing the provided value.</returns>
        public static LogParameters<T0, Void, Void, Void> Of<T0>(T0 p0) => new(p0);

        /// <summary>
        /// Creates a strongly-typed parameter container with two values.
        /// </summary>
        /// <typeparam name="T0">The type of the first parameter.</typeparam>
        /// <typeparam name="T1">The type of the second parameter.</typeparam>
        /// <param name="p0">The first parameter value.</param>
        /// <param name="p1">The second parameter value.</param>
        /// <returns>A <see cref="LogParameters{T0, T1, T2, T3}"/> instance containing the provided values.</returns>
        public static LogParameters<T0, T1, Void, Void> Of<T0, T1>(T0 p0, T1 p1) => new(p0, p1);

        /// <summary>
        /// Creates a strongly-typed parameter container with three values.
        /// </summary>
        /// <typeparam name="T0">The type of the first parameter.</typeparam>
        /// <typeparam name="T1">The type of the second parameter.</typeparam>
        /// <typeparam name="T2">The type of the third parameter.</typeparam>
        /// <param name="p0">The first parameter value.</param>
        /// <param name="p1">The second parameter value.</param>
        /// <param name="p2">The third parameter value.</param>
        /// <returns>A <see cref="LogParameters{T0, T1, T2, T3}"/> instance containing the provided values.</returns>
        public static LogParameters<T0, T1, T2, Void> Of<T0, T1, T2>(T0 p0, T1 p1, T2 p2) => new(p0, p1, p2);

        /// <summary>
        /// Creates a strongly-typed parameter container with four values.
        /// </summary>
        /// <typeparam name="T0">The type of the first parameter.</typeparam>
        /// <typeparam name="T1">The type of the second parameter.</typeparam>
        /// <typeparam name="T2">The type of the third parameter.</typeparam>
        /// <typeparam name="T3">The type of the fourth parameter.</typeparam>
        /// <param name="p0">The first parameter value.</param>
        /// <param name="p1">The second parameter value.</param>
        /// <param name="p2">The third parameter value.</param>
        /// <param name="p3">The fourth parameter value.</param>
        /// <returns>A <see cref="LogParameters{T0, T1, T2, T3}"/> instance containing the provided values.</returns>
        public static LogParameters<T0, T1, T2, T3> Of<T0, T1, T2, T3>(T0 p0, T1 p1, T2 p2, T3 p3) => new(p0, p1, p2, p3);
    }

    /// <summary>
    /// Base type for strongly-typed log parameter containers.
    /// </summary>
    /// <remarks>
    /// Use the <see cref="Params"/> helper methods to create instances.
    /// </remarks>
    public abstract record LogParameters : INullTestableType
    {
        /// <summary>
        /// Builds a comma-separated string of arguments suitable for insertion into a log message.
        /// </summary>
        /// <returns>A string representation of the contained arguments.</returns>
        public abstract string ToArgumentString();
    }

    /// <summary>
    /// Strongly-typed container for up to four log parameters.
    /// </summary>
    /// <typeparam name="T0">Type of the first parameter.</typeparam>
    /// <typeparam name="T1">Type of the second parameter.</typeparam>
    /// <typeparam name="T2">Type of the third parameter.</typeparam>
    /// <typeparam name="T3">Type of the fourth parameter.</typeparam>
    /// <param name="P0">The first parameter value.</param>
    /// <param name="P1">The second parameter value.</param>
    /// <param name="P2">The third parameter value.</param>
    /// <param name="P3">The fourth parameter value.</param>
    public record LogParameters<T0, T1, T2, T3>
        (
            T0 P0 = default!,
            T1 P1 = default!,
            T2 P2 = default!,
            T3 P3 = default!
        ) : LogParameters
    {
        /// <summary>
        /// Converts a value to a stable, log-friendly string representation.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="o">The value to convert.</param>
        /// <returns>A string suited for logging, handling primitives, strings, Unity objects and enumerables.</returns>
        private static string ToString<T>(T o)
        {
            switch (o)
            {
                case null:
                    return "null";
                case string s:
                    return $"\"{s}\"";
                case char c:
                    return $"'{c}'";
                case float f:
                    return f.ToString("G9", System.Globalization.CultureInfo.InvariantCulture);
                case double d:
                    return d.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
                case decimal m:
                    return m.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case byte b:
                    return b.ToString();
                case sbyte sb:
                    return sb.ToString();
                case short sh:
                    return sh.ToString();
                case ushort us:
                    return us.ToString();
                case int i:
                    return i.ToString();
                case uint ui:
                    return ui.ToString();
                case long l:
                    return l.ToString();
                case ulong ul:
                    return ul.ToString();

                case bool b:
                    return b ? "true" : "false";
                case UnityEngine.Object uo:
                    return uo.NiceName();
                case IEnumerable ie:
                    {
                        var it = ie.GetEnumerator();
                        using var e = it as IDisposable;
                        List<string> asStrings = [];
                        while (it.MoveNext())
                        {
                            asStrings.Add(ToString(it.Current));
                        }
                        return "[" + string.Join(", ", asStrings) + "]";
                    }
                default:
                    return $"`{o}`";
            }
        }

        /// <summary>
        /// Builds a comma-separated string of the non-<see cref="Void"/> parameters in their log-friendly form.
        /// </summary>
        /// <returns>A string containing the serialized parameters, separated by commas.</returns>
        public override string ToArgumentString()
        {
            List<string> args = [];
            if (P0 is not Void) args.Add(ToString(P0));
            if (P1 is not Void) args.Add(ToString(P1));
            if (P2 is not Void) args.Add(ToString(P2));
            if (P3 is not Void) args.Add(ToString(P3));
            return string.Join(", ", args);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AVS.Crafting;

/// <summary>
/// Path to a tab or module in the crafting interface.
/// </summary>
public readonly struct CraftPath : IEquatable<CraftPath>, IEnumerable<string>
{
    private readonly string[]? segments;

    /// <summary>
    /// The segments of the crafting path.
    /// </summary>
    public string[] Segments => segments ?? [];

    /// <summary>
    /// Gets a value indicating whether the collection of segments is empty.
    /// </summary>
    public bool IsEmpty => segments is null || segments.Length == 0;

    /// <summary>
    /// Gets the number of segments in the collection.
    /// </summary>
    public int Length => segments?.Length ?? 0;

    /// <summary>
    /// An empty crafting path, representing the root or no specific path.
    /// </summary>
    public static CraftPath Empty { get; } = default;

    /// <summary>
    /// Gets the last segment of the path. An empty path returns an empty string.
    /// </summary>
    public string Last => IsEmpty ? "" : segments![segments.Length - 1];

    /// <summary>
    /// Gets the segment preceding the last segment in the collection.
    /// </summary>
    public string Previous => Length < 1 ? "" : segments![segments.Length - 2];

    /// <summary>
    /// Gets the parent path, which is the path without the last segment.
    /// </summary>
    public CraftPath Parent => IsEmpty ? Empty : new CraftPath(Segments.Take(Length - 1));

    /// <summary>
    /// Gets an enumerable collection of <see cref="CraftPath"/> objects representing the ancestors of the
    /// current path.
    /// </summary>
    public IEnumerable<CraftPath> Ancestors
    {
        get
        {
            if (IsEmpty)
                yield break;
            for (var i = 0; i < Length; i++)
                yield return new CraftPath(Segments.Take(i + 1));
        }
    }

    /// <summary>
    /// Constructs a new <see cref="CraftPath"/> with the specified segments.
    /// </summary>
    public CraftPath(IEnumerable<string> segments)
    {
        segments = segments.Select(Sanitize).Where(s => !string.IsNullOrEmpty(s)).ToList();
        this.segments =
            segments.Any()
                ? segments.ToArray()
                : null;
    }

    /// <summary>
    /// Constructs a new <see cref="CraftPath"/> with the specified segments.
    /// </summary>
    public CraftPath(params string[] segments)
        : this(segments.AsEnumerable())
    {
    }

    internal static string Sanitize(string segment)
    {
        if (string.IsNullOrEmpty(segment))
            return segment;
        var s = segment.Trim();
        if (s.Contains("/"))
            throw new ArgumentException("Segment cannot contain / characters.", nameof(segment));
        return s;
    }

    /// <summary>
    /// Appends a new segment to the crafting path.
    /// </summary>
    /// <param name="segment">Segment to append. Must not be empty (after trim) and must not contain / characters</param>
    /// <returns>New crafting path with the given segment appended</returns>
    public CraftPath Append(string segment)
    {
        segment = Sanitize(segment);
        if (string.IsNullOrEmpty(segment))
            return this;

        if (IsEmpty)
        {
            return new CraftPath(segment);
        }
        else
        {
            var newSegments = new string[segments!.Length + 1];
            Array.Copy(segments, newSegments, segments.Length);
            newSegments[segments.Length] = segment;
            return new CraftPath(newSegments);
        }
    }


    /// <inheritdoc/>
    public override string ToString() => string.Join("/", Segments);

    /// <inheritdoc/>
    public bool Equals(CraftPath other)
    {
        if (IsEmpty && other.IsEmpty)
            return true;
        if (IsEmpty || other.IsEmpty)
            return false;
        if (segments!.Length != other.segments!.Length)
            return false;
        for (var i = 0; i < segments.Length; i++)
            if (!segments[i].Equals(other.segments[i]))
                return false;

        return true;
    }

    /// <summary>
    /// Determines if two <see cref="CraftPath"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="CraftPath"/> to compare.</param>
    /// <param name="right">The second <see cref="CraftPath"/> to compare.</param>
    /// <returns>True if the two <see cref="CraftPath"/> instances are equal; otherwise, false.</returns>
    public static bool operator ==(CraftPath left, CraftPath right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="CraftPath"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="CraftPath"/> to compare.</param>
    /// <param name="right">The second <see cref="CraftPath"/> to compare.</param>
    /// <returns>True if the two <see cref="CraftPath"/> instances are not equal; otherwise, false.</returns>
    public static bool operator !=(CraftPath left, CraftPath right) => !(left == right);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CraftPath other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        if (IsEmpty)
            return 0;
        var hash = 17;
        foreach (var segment in Segments)
            hash = hash * 31 + segment.GetHashCode();
        return hash;
    }

    /// <inheritdoc/>
    public IEnumerator<string> GetEnumerator()
    {
        if (IsEmpty)
            yield break;
        foreach (var segment in segments!)
            yield return segment;
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Concatenates a specified segment to the end of the given <see cref="CraftPath"/>.
    /// </summary>
    /// <param name="path">The <see cref="CraftPath"/> to which the segment will be appended.</param>
    /// <param name="segment">The segment to append to the <see cref="CraftPath"/>. Cannot be null or empty.</param>
    /// <returns>A new <see cref="CraftPath"/> instance with the segment appended.</returns>
    public static CraftPath operator +(CraftPath path, string segment) => path.Append(segment);
}
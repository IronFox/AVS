using System.Collections.Generic;
using UnityEngine;

namespace AVS.Util.Math;

/// <summary>
/// Represents a two-dimensional axis-aligned bounding box defined by ranges along the X and Y axes.
/// </summary>
/// <remarks>This structure is immutable and provides properties to access the center, size, and boundaries 
/// (minimum and maximum points) of the bounding box. It is useful for spatial calculations,  such as collision
/// detection or defining regions in 2D space.</remarks>
/// <param name="X"></param>
/// <param name="Y"></param>
public readonly record struct Bounds2(BoundsRange X, BoundsRange Y)
{
    /// <summary>
    /// Gets the center point as a two-dimensional vector.
    /// </summary>
    public Vector2 Center => new Vector2(X.GetCenter(), Y.GetCenter());
    /// <summary>
    /// Gets the size represented as a two-dimensional vector.
    /// </summary>
    public Vector2 Size => new Vector2(X.GetSize(), Y.GetSize());

    /// <summary>
    /// Gets the minimum values of the X and Y components as a <see cref="Vector2"/>.
    /// </summary>
    public Vector2 Min => new Vector2(X.Min, Y.Min);
    /// <summary>
    /// Gets the maximum values of the X and Y components as a <see cref="Vector2"/>.
    /// </summary>
    public Vector2 Max => new Vector2(X.Max, Y.Max);

    /// <inheritdoc/>
    public override string ToString() => $"Bounds2 @{Center} s={Size}";


    /// <summary>
    /// Creates a <see cref="Bounds2"/> instance that encompasses the specified collection of vertices.
    /// </summary>
    /// <param name="vertices">A collection of <see cref="Vector2"/> points to calculate the bounds for. Must not be null or empty.</param>
    /// <returns>A <see cref="Bounds2"/> that represents the smallest bounding box containing all the specified vertices.</returns>
    public static Bounds2 From(IEnumerable<Vector2> vertices)
        => BoundsBuilder2.From(vertices).Baked;
}

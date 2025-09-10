using System.Collections.Generic;
using UnityEngine;


namespace AVS.Util.Math;

/// <summary>
/// Represents a two-dimensional, mutable bounding box defined by two bounded ranges.
/// </summary>
/// <remarks>This class provides functionality for dynamically constructing and modifying a bounding box in two
/// dimensions. It supports operations such as including points or collections of points to expand the bounds, and
/// provides properties for accessing the bounds' minimum, maximum, center, size, and area. The bounds can also be
/// "baked" into an immutable <see cref="Bounds2"/> instance.</remarks>
public class BoundsBuilder2
{
    /// <summary>
    /// Gets the builder for defining a bounded range along the X axis.
    /// </summary>
    public BoundedRangeBuilder X { get; }
    /// <summary>
    /// Gets the builder for defining a bounded range along the Y axis.
    /// </summary>
    public BoundedRangeBuilder Y { get; }
    /// <summary>
    /// Gets the unique identifier for the entity.
    /// </summary>
    public int Id { get; }

    private static int idCounter = 0;

    /// <summary>
    /// Gets the minimum values of the X and Y components as a <see cref="Vector2"/>.
    /// </summary>
    public Vector2 Min => new Vector2(X.Min, Y.Min);
    /// <summary>
    /// Gets the maximum values of the X and Y components as a <see cref="Vector2"/>.
    /// </summary>
    public Vector2 Max => new Vector2(X.Max, Y.Max);
    /// <summary>
    /// Gets the center point as a two-dimensional vector.
    /// </summary>
    public Vector2 Center => new Vector2(X.GetCenter(), Y.GetCenter());
    /// <summary>
    /// Gets the size represented as a two-dimensional vector, where each component is the center value of the
    /// corresponding axis.
    /// </summary>
    public Vector2 Size => new Vector2(X.GetCenter(), Y.GetCenter());
    /// <summary>
    /// Gets the area calculated as the product of the center values of the X and Y components.
    /// </summary>
    public float Area => X.GetCenter() * Y.GetCenter();
    /// <summary>
    /// Gets the precomputed bounds based on the current X and Y ranges.
    /// </summary>
    public Bounds2 Baked => new Bounds2(X.Range, Y.Range);


    private BoundsBuilder2(BoundedRangeBuilder x, BoundedRangeBuilder y)
    {
        (X, Y) = (x, y);
        Id = idCounter++;
    }

    /// <inheritdoc/>
    public override string ToString()
        => $"#{Id}({Min}, {Max})";

    /// <summary>
    /// Gets an empty instance of <see cref="BoundsBuilder2"/> with no defined ranges.
    /// </summary>
    public static BoundsBuilder2 Empty => new BoundsBuilder2(BoundedRangeBuilder.Empty, BoundedRangeBuilder.Empty);


    /// <summary>
    /// Expands the bounds to include the specified 2D vector.
    /// </summary>
    /// <param name="v">The 2D vector to include in the bounds.</param>
    /// <returns>The updated <see cref="BoundsBuilder2"/> instance, allowing for method chaining.</returns>
    public BoundsBuilder2 Include(Vector2 v)
    {
        X.Include(v.x);
        Y.Include(v.y);
        return this;
    }

    /// <summary>
    /// Expands the bounds to include the specified collection of vertices.
    /// </summary>
    /// <param name="vertices">A collection of <see cref="Vector2"/> instances to include in the bounds.</param>
    /// <returns>The current <see cref="BoundsBuilder2"/> instance, allowing for method chaining.</returns>
    public BoundsBuilder2 Include(IEnumerable<Vector2> vertices)
    {
        foreach (var v in vertices)
            Include(v);
        return this;
    }

    /// <summary>
    /// Creates a new <see cref="BoundsBuilder2"/> instance that encompasses the specified vertices.
    /// </summary>
    /// <param name="vertices">A collection of <see cref="Vector2"/> points to include in the bounds.  The collection must not be null.</param>
    /// <returns>A <see cref="BoundsBuilder2"/> instance that includes the specified vertices.</returns>
    public static BoundsBuilder2 From(IEnumerable<Vector2> vertices)
    {
        return Empty.Include(vertices);
    }
}

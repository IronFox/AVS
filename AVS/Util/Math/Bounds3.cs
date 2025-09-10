using UnityEngine;

namespace AVS.Util.Math;

/// <summary>
/// Represents a three-dimensional axis-aligned bounding box defined by ranges along the X, Y, and Z axes.
/// </summary>
/// <remarks>The <see cref="Bounds3"/> structure provides methods and properties for working with 3D bounding
/// boxes,  including calculating the center, size, and extents, as well as performing operations such as translation 
/// and containment checks. It is immutable and designed for efficient comparisons and transformations.</remarks>
/// <param name="X"></param>
/// <param name="Y"></param>
/// <param name="Z"></param>
public readonly record struct Bounds3(BoundsRange X, BoundsRange Y, BoundsRange Z)
{
    /// <summary>
    /// Gets the center point of the object as a three-dimensional vector.
    /// </summary>
    public Vector3 Center => new Vector3(X.GetCenter(), Y.GetCenter(), Z.GetCenter());
    /// <summary>
    /// Gets the size of the object as a three-dimensional vector.
    /// </summary>
    public Vector3 Size => new Vector3(X.GetSize(), Y.GetSize(), Z.GetSize());

    /// <summary>
    /// Gets the minimum values of the X, Y, and Z components as a <see cref="Vector3"/>.
    /// </summary>
    public Vector3 Min => new Vector3(X.Min, Y.Min, Z.Min);
    /// <summary>
    /// Gets the maximum values of the X, Y, and Z components as a <see cref="Vector3"/>.
    /// </summary>
    public Vector3 Max => new Vector3(X.Max, Y.Max, Z.Max);


    /// <summary>
    /// Creates a <see cref="Bounds3"/> instance from the specified <see cref="Bounds"/>.
    /// </summary>
    /// <param name="b">The <see cref="Bounds"/> object to convert. Represents the minimum and maximum points of a 3D bounding box.</param>
    /// <returns>A new <see cref="Bounds3"/> instance that corresponds to the specified <see cref="Bounds"/>.</returns>
    public static Bounds3 From(Bounds b)
    {
        return new Bounds3(new BoundsRange(b.min.x, b.max.x), new BoundsRange(b.min.y, b.max.y), new BoundsRange(b.min.z, b.max.z));
    }

    /// <inheritdoc/>
    public override string ToString() => $"Bounds3 @{Center} s={Size}";


    /// <summary>
    /// Returns a new <see cref="Bounds3"/> instance translated by the specified vector.
    /// </summary>
    /// <param name="delta">The translation vector, where each component specifies the amount to translate along the corresponding axis.</param>
    /// <returns>A new <see cref="Bounds3"/> instance translated by the specified vector.</returns>
    public Bounds3 TranslatedBy(Vector3 delta)
        => new Bounds3(
            X.TranslatedBy(delta.x),
            Y.TranslatedBy(delta.y),
            Z.TranslatedBy(delta.z)
            );

    /// <summary>
    /// Determines whether the current <see cref="Bounds3"/> fully contains the specified <paramref name="other"/>
    /// bounds.
    /// </summary>
    /// <param name="other">The <see cref="Bounds3"/> to check for containment within the current bounds.</param>
    /// <returns><see langword="true"/> if the specified <paramref name="other"/> bounds are fully contained within the current
    /// bounds; otherwise, <see langword="false"/>.</returns>
    public bool Contains(Bounds3 other)
        => X.Contains(other.X)
           && Y.Contains(other.Y)
           && Z.Contains(other.Z);

    /// <summary>
    /// Determines whether the specified Bounds3 is fully contained within this Bounds3
    /// assuming it was at the same center.
    /// </summary>
    /// <param name="other">The Bounds3 instance to check for containment.</param>
    /// <returns>True if the specified Bounds3 is fully contained within this Bounds3, if it were located in the same center.</returns>
    public bool ContainsCentered(Bounds3 other)
        => X.ContainsCentered(other.X)
           && Y.ContainsCentered(other.Y)
           && Z.ContainsCentered(other.Z);

    /// <summary>
    /// Creates a new axis-aligned bounding box centered at the specified position with the given size.
    /// </summary>
    /// <param name="center">The center point of the bounding box, represented as a <see cref="Vector3"/>.</param>
    /// <param name="size">The dimensions of the bounding box, represented as a <see cref="Vector3"/>. Each component must be non-negative.</param>
    /// <returns>A <see cref="Bounds3"/> instance representing the axis-aligned bounding box.</returns>
    public static Bounds3 CenterBox(Vector3 center, Vector3 size)
        => new Bounds3(
            BoundsRange.Centered(center.x, size.x),
            BoundsRange.Centered(center.y, size.y),
            BoundsRange.Centered(center.z, size.z)
        );
}

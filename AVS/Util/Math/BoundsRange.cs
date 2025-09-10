namespace AVS.Util.Math;

/// <summary>
/// Represents a range of floating-point values defined by a minimum and maximum boundary.
/// </summary>
/// <remarks>
/// A <see cref="BoundsRange"/> is an immutable structure used to describe a contiguous range of
/// values. It provides properties for analyzing the range, such as size, center, and whether
/// the range is empty. Methods for checking if a value or another range overlaps with this
/// instance are also available.
/// </remarks>
/// <seealso cref="System.IEquatable{T}"/>
public readonly record struct BoundsRange(float Min, float Max) : IBoundsRange
{
    /// <inheritdoc/>
    public override string ToString() => this.IsEmpty() ? "<empty>" : $"[{Min}, {Max}]";


    /// <summary>
    /// Returns a new <see cref="BoundsRange"/> instance translated by the specified delta.
    /// </summary>
    /// <param name="delta">The amount by which to translate the range. Positive values shift the range upward, and negative values shift it
    /// downward.</param>
    /// <returns>A new <see cref="BoundsRange"/> with both the minimum and maximum values adjusted by the specified delta.</returns>
    public BoundsRange TranslatedBy(float delta) => new BoundsRange(Min + delta, Max + delta);


    /// <summary>
    /// Creates a <see cref="BoundsRange"/> instance that is centered on the specified value and has the specified size.
    /// </summary>
    /// <param name="center">The central point of the range.</param>
    /// <param name="size">The total size or width of the range. Must be a non-negative value.</param>
    /// <returns>A new <see cref="BoundsRange"/> instance centered at the specified value with the given size.</returns>
    public static BoundsRange Centered(float center, float size)
    {
        return new BoundsRange(center - size / 2f, center + size / 2f);
    }
}

using UnityEngine;


namespace AVS.Util.Math;

/// <summary>
/// Represents a utility class to construct and manage a bounded range of floating-point values.
/// </summary>
/// <remarks>
/// A bounded range is defined by a minimum and maximum value. This class provides methods to
/// calculate properties like size and center of the range, check its emptiness, and include
/// additional values or other bounded ranges to dynamically extend its boundaries.
/// </remarks>
public class BoundedRangeBuilder : IBoundsRange
{
    /// <summary>
    /// Gets the minimum value in the dataset.
    /// </summary>
    public float Min { get; private set; }

    /// <summary>
    /// Gets the maximum value allowed for the operation.
    /// </summary>
    public float Max { get; private set; }

    /// <summary>
    /// Gets an empty <see cref="BoundedRangeBuilder"/> instance with no valid range.
    /// </summary>
    public static BoundedRangeBuilder Empty => new BoundedRangeBuilder(float.MaxValue, float.MinValue);

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundedRangeBuilder"/> class with the specified minimum and maximum
    /// values.
    /// </summary>
    /// <remarks>The <paramref name="min"/> and <paramref name="max"/> parameters define the bounds of the
    /// range.  Ensure that <paramref name="max"/> is not less than <paramref name="min"/> to avoid invalid range
    /// definitions.</remarks>
    /// <param name="min">The minimum value of the range.</param>
    /// <param name="max">The maximum value of the range. Must be greater than or equal to <paramref name="min"/>.</param>
    public BoundedRangeBuilder(float min, float max)
        => (Min, Max) = (min, max);

    /// <summary>
    /// Gets the bounds of the current range as an instance of <see cref="BoundsRange"/>.
    /// </summary>
    /// <remarks>
    /// The range is defined by its minimum and maximum values. This property provides
    /// a compact representation of the current range.
    /// </remarks>
    public BoundsRange Range => new BoundsRange(Min, Max);

    /// <summary>
    /// Updates the range to include the specified value.
    /// </summary>
    /// <param name="value">The value to include in the range. If the value is less than the current minimum, the minimum is updated. If the
    /// value is greater than the current maximum, the maximum is updated.</param>
    public void Include(float value)
    {
        Min = Mathf.Min(Min, value);
        Max = Mathf.Max(Max, value);
    }

    /// <summary>
    /// Expands the current range to include the range defined by the specified <see cref="BoundedRangeBuilder"/>.
    /// </summary>
    /// <remarks>After calling this method, the current range will encompass the minimum and maximum values of
    /// both the original range and the specified range.</remarks>
    /// <param name="other">The <see cref="BoundedRangeBuilder"/> whose range will be included. If the specified range is empty, this method
    /// has no effect.</param>
    public void Include(BoundedRangeBuilder other)
    {
        if (other.IsEmpty())
            return;
        Min = Mathf.Min(Min, other.Min);
        Max = Mathf.Max(Max, other.Max);
    }


}


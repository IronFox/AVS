namespace AVS.Util.Math
{
    /// <summary>
    /// Represents a range defined by a minimum and maximum value.
    /// </summary>
    /// <remarks>This interface is typically used to describe the bounds of a dataset or a range of values.
    /// Implementations of this interface should ensure that <see cref="Min"/> is less than or equal to <see
    /// cref="Max"/>.</remarks>
    public interface IBoundsRange
    {
        /// <summary>
        /// Gets the minimum value in the dataset.
        /// </summary>
        public float Min { get; }

        /// <summary>
        /// Gets the maximum value in the dataset.
        /// </summary>
        public float Max { get; }

    }



    /// <summary>
    /// Provides extension methods for working with objects that implement the <see cref="IBoundsRange"/> interface.
    /// </summary>
    /// <remarks>This static class includes methods for calculating the size, center, and emptiness of a range
    /// defined by minimum and maximum values. These methods are designed to operate on any type that implements the
    /// <see cref="IBoundsRange"/> interface.</remarks>
    public static class BoundsRangeExt
    {


        /// <summary>
        /// Gets the size, calculated as the difference between the maximum and minimum values.
        /// </summary>
        public static float GetSize<T>(this T self) where T : IBoundsRange
            => self.Max - self.Min;

        /// <summary>
        /// Gets the center point of the range represented by the minimum and maximum values.
        /// </summary>
        public static float GetCenter<T>(this T self) where T : IBoundsRange
            => (self.Min + self.Max) / 2f;

        /// <summary>
        /// Gets a value indicating whether the range is empty.
        /// </summary>
        /// <remarks>A range is considered empty if the minimum value is greater than the maximum value.</remarks>
        public static bool IsEmpty<T>(this T self) where T : IBoundsRange
            => self.Min > self.Max;


        /// <summary>
        /// Determines whether the specified value is within the range defined by this instance.
        /// </summary>
        /// <param name="value">The value to check for inclusion in the range.</param>
        /// <param name="self">The instance of <see cref="IBoundsRange"/> to check against.</param>
        /// <returns><see langword="true"/> if the specified value is within the range; otherwise, <see langword="false"/>
        /// </returns>
        public static bool Contains<T>(this T self, float value) where T : IBoundsRange
            => value >= self.Min && value <= self.Max;

        /// <summary>
        /// Determines whether the specified range is fully contained within the current range.
        /// </summary>
        /// <param name="other">The <see cref="BoundsRange"/> to check for containment.</param>
        /// <param name="self">The instance of <see cref="IBoundsRange"/> to check against.</param>
        /// <returns><see langword="true"/> if the specified range is fully contained within the current range; otherwise, <see
        /// langword="false"/>.</returns>
        public static bool Contains<T0, T1>(this T0 self, T1 other) where T0 : IBoundsRange where T1 : IBoundsRange
            => other.Min >= self.Min && other.Max <= self.Max;

        /// <summary>
        /// Determines whether the given <see cref="BoundsRange"/> is fully contained within this <see cref="BoundsRange"/>, assuming it is centered within this range.
        /// </summary>
        /// <param name="other">The range to check for containment.</param>
        /// <param name="self">The instance of <see cref="IBoundsRange"/> to check against.</param>
        /// <returns>True if the specified range would fit while located in the same center, otherwise false.</returns>
        public static bool ContainsCentered<T0, T1>(this T0 self, T1 other) where T0 : IBoundsRange where T1 : IBoundsRange
            => other.GetSize() <= self.GetSize();

        /// <summary>
        /// Determines whether the range represented by the current instance overlaps with the range of another object.
        /// </summary>
        /// <remarks>Two ranges are considered to overlap if their boundaries intersect, meaning the
        /// minimum value of one range is less than or equal to the maximum value of the other range, and vice
        /// versa.</remarks>
        /// <typeparam name="T0">The type of the current instance, which must implement <see cref="IBoundsRange"/>.</typeparam>
        /// <typeparam name="T1">The type of the other object, which must implement <see cref="IBoundsRange"/>.</typeparam>
        /// <param name="self">The current instance representing a range.</param>
        /// <param name="other">The other object representing a range to compare against.</param>
        /// <returns><see langword="true"/> if the range of <paramref name="self"/> overlaps with the range of <paramref
        /// name="other"/>; otherwise, <see langword="false"/>.</returns>
        public static bool Overlaps<T0, T1>(this T0 self, T1 other) where T0 : IBoundsRange where T1 : IBoundsRange
            => other.Min <= self.Max && other.Max >= self.Min;


    }
}
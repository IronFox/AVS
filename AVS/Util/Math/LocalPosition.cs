using UnityEngine;

namespace AVS.Util.Math
{
    /// <summary>
    /// Represents a position relative to a specific origin in 3D space.
    /// </summary>
    /// <remarks>A <see cref="LocalPosition"/> encapsulates a 3D position vector and the associated origin
    /// transform. It can be converted to a global position using the <see cref="ToGlobal"/> method.</remarks>
    /// <param name="LocalCoordinates">The local coordinates</param>
    /// <param name="Origin">The origin which the coordinates are relative to</param>
    public readonly record struct LocalPosition(Vector3 LocalCoordinates, Transform Origin)
    {

        /// <summary>
        /// Linearly interpolates between two <see cref="LocalPosition"/> instances without clamping the interpolation
        /// factor.
        /// </summary>
        /// <remarks>This method does not clamp the value of <paramref name="t"/>. If clamping is
        /// required,  consider using an alternative method or manually constrain the value of <paramref
        /// name="t"/>.</remarks>
        /// <param name="a">The starting <see cref="LocalPosition"/>.</param>
        /// <param name="b">The ending <see cref="LocalPosition"/>.</param>
        /// <param name="t">The interpolation factor. Values less than 0 extrapolate beyond <paramref name="a"/>,  and values greater
        /// than 1 extrapolate beyond <paramref name="b"/>.</param>
        /// <returns>A new <see cref="LocalPosition"/> that represents the interpolated position between  <paramref name="a"/>
        /// and <paramref name="b"/> based on the interpolation factor <paramref name="t"/>.</returns>
        public static LocalPosition LerpUnclamped(LocalPosition a, LocalPosition b, float t)
        {
            return new LocalPosition(Vector3.LerpUnclamped(a.LocalCoordinates, b.LocalCoordinates, t), a.Origin);
        }

        /// <summary>
        /// Converts the current local position to a global position.
        /// </summary>
        /// <returns>A <see cref="GlobalPosition"/> representing the position in global coordinates.</returns>
        public GlobalPosition ToGlobal()
            => new GlobalPosition(Origin.TransformPoint(LocalCoordinates));

        /// <inheritdoc/>
        public override string ToString() => $"L{LocalCoordinates}";
    }
}

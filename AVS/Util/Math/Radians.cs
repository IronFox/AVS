using System;
using UnityEngine;

namespace AVS.Util.Math
{
    /// <summary>
    /// An angle in radians.
    /// </summary>
    public readonly record struct Radians(float Value) : IAngle, IComparable<Radians>
    {
        /// <summary>
        /// Converts degrees to radiants.
        /// </summary>
        public static implicit operator Radians(Degrees deg)
            => new Radians(deg.Value * Mathf.Deg2Rad);

        /// <summary>
        /// Adds two <see cref="Radians"/> instances together.
        /// </summary>
        /// <param name="a">The first <see cref="Radians"/> instance to add.</param>
        /// <param name="b">The second <see cref="Radians"/> instance to add.</param>
        /// <returns>A new <see cref="Radians"/> instance representing the sum of <paramref name="a"/> and <paramref name="b"/>.</returns>
        public static Radians operator +(Radians a, Radians b)
            => new Radians(a.Value + b.Value);
        /// <summary>
        /// Subtracts one <see cref="Radians"/> value from another.
        /// </summary>
        /// <param name="a">The minuend, representing the first <see cref="Radians"/> value.</param>
        /// <param name="b">The subtrahend, representing the second <see cref="Radians"/> value to subtract from <paramref name="a"/>.</param>
        /// <returns>A new <see cref="Radians"/> instance representing the result of the subtraction.</returns>
        public static Radians operator -(Radians a, Radians b)
            => new Radians(a.Value - b.Value);

        /// <inheritdoc/>
        public override string ToString() => $"{Value.ToStr()} rad";

        /// <inheritdoc/>
        public int CompareTo(Radians other)
            => Value.CompareTo(other.Value);

        /// <summary>
        /// Gets the sine of the current value.
        /// </summary>
        public float Sin => Mathf.Sin(Value);
        /// <summary>
        /// Gets the cosine of the current value.
        /// </summary>
        public float Cos => Mathf.Cos(Value);

        /// <summary>
        /// Multiplies the specified <see cref="Radians"/> value by a scalar.
        /// </summary>
        /// <param name="a">The <see cref="Radians"/> value to be multiplied.</param>
        /// <param name="b">The scalar value to multiply by.</param>
        /// <returns>A new <see cref="Radians"/> instance representing the product of the original value and the scalar.</returns>
        public static Radians operator *(Radians a, float b)
            => new Radians(a.Value * b);

        /// <summary>
        /// Multiplies a scalar value by a <see cref="Radians"/> instance, scaling the angle.
        /// </summary>
        /// <param name="a">The scalar value to multiply.</param>
        /// <param name="b">The <see cref="Radians"/> instance representing the angle to be scaled.</param>
        /// <returns>A new <see cref="Radians"/> instance representing the scaled angle.</returns>
        public static Radians operator *(float a, Radians b)
            => new Radians(a * b.Value);

        /// <summary>
        /// Divides the value of the specified <see cref="Radians"/> instance by a scalar value.
        /// </summary>
        /// <param name="a">The <see cref="Radians"/> instance to be divided.</param>
        /// <param name="b">The scalar value by which to divide the <see cref="Radians"/> value.</param>
        /// <returns>A new <see cref="Radians"/> instance representing the result of the division.</returns>
        public static Radians operator /(Radians a, float b)
            => new Radians(a.Value / b);

        /// <summary>
        /// Divides a scalar value by a <see cref="Radians"/> value and returns the resulting <see cref="Radians"/>.
        /// </summary>
        /// <param name="a">The scalar value to divide.</param>
        /// <param name="b">The <see cref="Radians"/> value to divide by. Must not have a <see cref="Radians.Value"/> of zero.</param>
        /// <returns>A new <see cref="Radians"/> representing the result of the division.</returns>
        public static Radians operator /(float a, Radians b)
            => new Radians(a / b.Value);

        /// <summary>
        /// Negates the value of the specified <see cref="Radians"/> instance.
        /// </summary>
        /// <param name="a">The <see cref="Radians"/> instance to negate.</param>
        /// <returns>A new <see cref="Radians"/> instance with the negated value.</returns>
        public static Radians operator -(Radians a)
            => new Radians(-a.Value);

        /// <summary>
        /// Gets the current value of the angle in degrees.
        /// </summary>
        public Degrees Deg => this;

        /// <summary>
        /// Generates a random angle within the specified range of radians.
        /// </summary>
        /// <param name="min">The minimum angle, in radians, that the result can be. Must be less than or equal to <paramref name="max"/>.</param>
        /// <param name="max">The maximum angle, in radians, that the result can be. Must be greater than or equal to <paramref
        /// name="min"/>.</param>
        /// <returns>A <see cref="Radians"/> instance representing a random angle between <paramref name="min"/> and <paramref
        /// name="max"/> (inclusive).</returns>
        public static Radians RandomIn(Radians min, Radians max)
            => new Radians(UnityEngine.Random.Range(min.Value, max.Value));
    }
}

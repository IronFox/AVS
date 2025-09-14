using System;
using System.Globalization;
using UnityEngine;

namespace AVS.Util.Math
{
    /// <summary>
    /// An angle in degrees.
    /// </summary>
    public readonly record struct Degrees(float Value) : IAngle, IComparable<Degrees>
    {
        /// <summary>
        /// Gets a <see cref="Degrees"/> instance representing zero degrees.
        /// </summary>
        public static Degrees Zero => new Degrees(0f);
        /// <summary>
        /// Gets a <see cref="Degrees"/> instance representing 180 degrees.
        /// </summary>
        public static Degrees OneEighty => new Degrees(180f);

        /// <summary>
        /// Gets a <see cref="Degrees"/> instance representing 360 degrees.
        /// </summary>
        public static Degrees ThreeSixty => new Degrees(360f);

        /// <summary>
        /// Gets a <see cref="Degrees"/> instance representing 30 degrees.
        /// </summary>
        public static Degrees Thirty => new Degrees(30f);
        /// <summary>
        /// Gets a <see cref="Degrees"/> instance representing an angle of 45 degrees.
        /// </summary>
        public static Degrees FortyFive => new Degrees(45f);
        /// <summary>
        /// Gets a <see cref="Degrees"/> instance representing 90 degrees.
        /// </summary>
        public static Degrees Ninety => new Degrees(90f);


        /// <summary>
        /// Creates a <see cref="Degrees"/> instance representing the rotation around the Y-axis  extracted from the
        /// specified quaternion.
        /// </summary>
        /// <param name="rotation">The quaternion from which to extract the Y-axis rotation.</param>
        /// <returns>A <see cref="Degrees"/> instance representing the Y-axis rotation in degrees.</returns>
        public static Degrees FromEulerY(Quaternion rotation)
            => new Degrees(rotation.eulerAngles.y);
        /// <summary>
        /// Creates a <see cref="Degrees"/> instance representing the rotation around the X-axis  extracted from the
        /// specified quaternion.
        /// </summary>
        /// <param name="rotation">The quaternion from which to extract the X-axis rotation.</param>
        /// <returns>A <see cref="Degrees"/> instance representing the rotation around the X-axis.</returns>
        public static Degrees FromEulerX(Quaternion rotation)
            => new Degrees(rotation.eulerAngles.x);
        /// <summary>
        /// Creates a <see cref="Degrees"/> instance representing the rotation around the Z-axis extracted from the
        /// specified quaternion.
        /// </summary>
        /// <param name="rotation">The quaternion from which to extract the Z-axis rotation.</param>
        /// <returns>A <see cref="Degrees"/> instance representing the Z-axis rotation in degrees.</returns>
        public static Degrees FromEulerZ(Quaternion rotation)
            => new Degrees(rotation.eulerAngles.z);

        /// <summary>
        /// Adds two <see cref="Degrees"/> instances and returns the result.
        /// </summary>
        /// <param name="a">The first <see cref="Degrees"/> instance to add.</param>
        /// <param name="b">The second <see cref="Degrees"/> instance to add.</param>
        /// <returns>A new <see cref="Degrees"/> instance representing the sum of <paramref name="a"/> and <paramref name="b"/>.</returns>
        public static Degrees operator +(Degrees a, Degrees b)
            => new Degrees(a.Value + b.Value);
        /// <summary>
        /// Subtracts one <see cref="Degrees"/> instance from another and returns the result.
        /// </summary>
        /// <param name="a">The minuend, representing the first <see cref="Degrees"/> instance.</param>
        /// <param name="b">The subtrahend, representing the second <see cref="Degrees"/> instance to subtract from <paramref
        /// name="a"/>.</param>
        /// <returns>A new <see cref="Degrees"/> instance representing the difference between <paramref name="a"/> and <paramref
        /// name="b"/>.</returns>
        public static Degrees operator -(Degrees a, Degrees b)
            => new Degrees(a.Value - b.Value);

        /// <summary>
        /// Multiplies the specified angle by a scalar value.
        /// </summary>
        /// <param name="a">The angle, in degrees, to be multiplied.</param>
        /// <param name="b">The scalar value by which to multiply the angle.</param>
        /// <returns>A new <see cref="Degrees"/> instance representing the result of the multiplication.</returns>
        public static Degrees operator *(Degrees a, float b)
            => new Degrees(a.Value * b);
        /// <summary>
        /// Multiplies a scalar value by an angle in degrees.
        /// </summary>
        /// <param name="a">The scalar value to multiply.</param>
        /// <param name="b">The angle, in degrees, to be scaled.</param>
        /// <returns>A new <see cref="Degrees"/> instance representing the scaled angle.</returns>
        public static Degrees operator *(float a, Degrees b)
            => new Degrees(a * b.Value);
        /// <summary>
        /// Divides the value of the specified <see cref="Degrees"/> instance by a scalar value.
        /// </summary>
        /// <param name="a">The <see cref="Degrees"/> instance to be divided.</param>
        /// <param name="b">The scalar value by which to divide the <paramref name="a"/> value. Must not be zero.</param>
        /// <returns>A new <see cref="Degrees"/> instance representing the result of the division.</returns>
        public static Degrees operator /(Degrees a, float b)
            => new Degrees(a.Value / b);
        /// <summary>
        /// Divides a scalar value by an angle, returning a new <see cref="Degrees"/> instance.
        /// </summary>
        /// <param name="a">The scalar value to divide.</param>
        /// <param name="b">The <see cref="Degrees"/> instance representing the divisor.</param>
        /// <returns>A new <see cref="Degrees"/> instance representing the result of the division.</returns>
        public static Degrees operator /(float a, Degrees b)
            => new Degrees(a / b.Value);
        /// <summary>
        /// Negates the value of the specified <see cref="Degrees"/> instance.
        /// </summary>
        /// <param name="a">The <see cref="Degrees"/> instance to negate.</param>
        /// <returns>A new <see cref="Degrees"/> instance with the negated value.</returns>
        public static Degrees operator -(Degrees a)
            => new Degrees(-a.Value);

        /// <summary>
        /// Converts an instance of <see cref="Radians"/> to an instance of <see cref="Degrees"/>.
        /// </summary>
        public static implicit operator Degrees(Radians rad)
            => new Degrees(rad.Value * Mathf.Rad2Deg);

        /// <inheritdoc/>
        public override string ToString() => $"{Value.ToString("#.#", CultureInfo.InvariantCulture)}°";

        /// <inheritdoc/>
        public int CompareTo(Degrees other)
            => Value.CompareTo(other.Value);

        /// <summary>
        /// Wraps the current angle value to the range of -180 to 180 degrees.
        /// </summary>
        /// <remarks>This method ensures that the angle remains within the standard range for representing
        /// directional angles, wrapping values outside the range of -180 to 180 degrees back into it.</remarks>
        /// <returns>A new <see cref="Degrees"/> instance with the angle wrapped to the range of -180 to 180 degrees.</returns>
        public Degrees WrapToPlusMinus180()
        {
            return new Degrees(Value.WrapTo(-180f, 180f, 360f));
        }

        /// <summary>
        /// Gets the current instance of the <see cref="Radians"/> class.
        /// </summary>
        public Radians Rad => this;

        /// <summary>
        /// Generates a random angle within the specified range of degrees.
        /// </summary>
        /// <remarks>The range is inclusive of the lower bound and exclusive of the upper bound. Ensure
        /// that <paramref name="a"/> is less than or equal to <paramref name="b"/> to avoid unexpected
        /// results.</remarks>
        /// <param name="a">The lower bound of the range, in degrees.</param>
        /// <param name="b">The upper bound of the range, in degrees.</param>
        /// <returns>A <see cref="Degrees"/> instance representing a random angle between <paramref name="a"/> and <paramref
        /// name="b"/>.</returns>
        public static Degrees RandomIn(Degrees a, Degrees b)
            => new Degrees(UnityEngine.Random.Range(a.Value, b.Value));
    }
}

using System;
using UnityEngine;

namespace AVS.VehicleComponents
{
    /// <summary>
    /// A full color definition, including HSB (Hue, Saturation, Brightness) values and the corresponding RGB color.
    /// </summary>
    public readonly struct VehicleColor : IEquatable<VehicleColor>
    {
        /// <summary>
        /// The HSB (Hue, Saturation, Brightness) representation of the color.
        /// Zero if not supplied. ASV does nothing with this value.
        /// </summary>
        public Vector3 HSB { get; }
        /// <summary>
        /// The applied RGB color.
        /// </summary>
        public Color RGB { get; }

        /// <summary>
        /// Default vehicle color.
        /// </summary>
        public static VehicleColor Default { get; } = new VehicleColor(Color.white, new Vector3(0, 0, 1));

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleColor"/> class with the specified HSB values and
        /// corresponding color.
        /// </summary>
        /// <param name="hsb">A <see cref="Vector3"/> representing the hue, saturation, and brightness (HSB) values of the color. Optional.</param>
        /// <param name="rgb">A <see cref="RGB"/> representing the corresponding color in the RGB color space.</param>
        public VehicleColor(Color rgb, Vector3 hsb = default)
        {
            HSB = hsb;
            RGB = rgb;
        }

        /// <summary>
        /// Determines whether the specified <see cref="VehicleColor"/> is equal to the current <see cref="VehicleColor"/>.
        /// </summary>
        /// <param name="other">The <see cref="VehicleColor"/> to compare with the current <see cref="VehicleColor"/>.</param>
        /// <returns>true if the specified <see cref="VehicleColor"/> is equal to the current <see cref="VehicleColor"/>; otherwise, false.</returns>
        public bool Equals(VehicleColor other)
        {
            return HSB.Equals(other.HSB) && RGB.Equals(other.RGB);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="VehicleColor"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current <see cref="VehicleColor"/>.</param>
        /// <returns>true if the specified object is equal to the current <see cref="VehicleColor"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is VehicleColor other && Equals(other);
        }

        /// <summary>
        /// Serves as a hash function for the <see cref="VehicleColor"/> type.
        /// </summary>
        /// <returns>A hash code for the current <see cref="VehicleColor"/>.</returns>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + HSB.GetHashCode();
            hash = hash * 23 + RGB.GetHashCode();
            return hash;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"VehicleColor(HSB: {HSB}, RGB: {RGB})";
        }

        /// <inheritdoc/>
        public static bool operator ==(VehicleColor left, VehicleColor right)
        {
            return left.Equals(right);
        }
        /// <inheritdoc/>
        public static bool operator !=(VehicleColor left, VehicleColor right)
        {
            return !(left == right);
        }
    }
}

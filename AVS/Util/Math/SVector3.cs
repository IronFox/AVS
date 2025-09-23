using Newtonsoft.Json;
using UnityEngine;

namespace AVS.Util.Math
{
    /// <summary>
    /// Serializable substitute for UnityEngine.Vector3
    /// </summary>
    public readonly record struct SVector3(float X, float Y, float Z)
    {
        /// <summary>
        /// Gets a vector with all components set to zero.
        /// </summary>
        public static SVector3 Zero => new SVector3(0f, 0f, 0f);
        /// <summary>
        /// Gets a vector with all components set to one.
        /// </summary>
        public static SVector3 One => new SVector3(1f, 1f, 1f);

        /// <summary>
        /// Converts an <see cref="SVector3"/> instance to a <see cref="Vector3"/> instance.
        /// </summary>
        /// <param name="sv">The <see cref="SVector3"/> instance to convert.</param>
        public static implicit operator Vector3(SVector3 sv) => new Vector3(sv.X, sv.Y, sv.Z);
        /// <summary>
        /// Implicitly converts a <see cref="Vector3"/> to an <see cref="SVector3"/>.
        /// </summary>
        /// <param name="v">The <see cref="Vector3"/> instance to convert.</param>
        public static implicit operator SVector3(Vector3 v) => new SVector3(v.x, v.y, v.z);


        /// <summary>
        /// Gets the length of the vector.
        /// </summary>
        [JsonIgnore]
        public float Length => Mathf.Sqrt(SquaredLength);

        /// <summary>
        /// Gets the squared length of the vector.
        /// </summary>
        [JsonIgnore]
        public float SquaredLength => X * X + Y * Y + Z * Z;

        /// <inheritdoc/>
        public override string ToString() => $"({X.ToStr()}, {Y.ToStr()}, {Z.ToStr()})";
    }
}

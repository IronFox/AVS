using UnityEngine;

namespace AVS.Util.Math
{
    /// <summary>
    /// Represents a global position in 3D space.
    /// </summary>
    /// <remarks>This structure encapsulates a 3D position using a <see cref="Vector3"/>.  It is immutable and
    /// can be used to represent positions in a global coordinate system.</remarks>
    /// <param name="GlobalCoordinates"></param>
    public readonly record struct GlobalPosition(Vector3 GlobalCoordinates)
    {
        /// <summary>
        /// Gets the X-coordinate of the global position.
        /// </summary>
        public float X => GlobalCoordinates.x;
        /// <summary>
        /// Gets the Y-coordinate of the object in global space.
        /// </summary>
        public float Y => GlobalCoordinates.y;
        /// <summary>
        /// Gets the Z-coordinate value from the global coordinate system.
        /// </summary>
        public float Z => GlobalCoordinates.z;
        /// <summary>
        /// Creates a <see cref="Math.GlobalPosition"/> instance from the specified <see cref="Transform"/>.
        /// </summary>
        /// <param name="t">The <see cref="Transform"/> whose position will be used to create the <see cref="Math.GlobalPosition"/>.</param>
        /// <returns>A new <see cref="Math.GlobalPosition"/> representing the position of the specified <see cref="Transform"/>.</returns>
        public static GlobalPosition Of(Transform t)
            => new(t.position);

        /// <summary>
        /// Creates a <see cref="GlobalPosition"/> instance from the specified <see cref="RaycastHit"/>.
        /// </summary>
        /// <param name="hit">The <see cref="RaycastHit"/> containing the point data to initialize the <see cref="GlobalPosition"/>.</param>
        /// <returns>A new <see cref="GlobalPosition"/> representing the position of the <paramref name="hit"/>.</returns>
        public static GlobalPosition Of(RaycastHit hit)
            => new(hit.point);

        /// <summary>
        /// Creates a <see cref="GlobalPosition"/> instance from the specified <see cref="GameObject"/>.
        /// </summary>
        /// <param name="go">The <see cref="GameObject"/> from which to create the <see cref="GlobalPosition"/>.</param>
        /// <returns>A <see cref="GlobalPosition"/> representing the global position of the specified <see cref="GameObject"/>.</returns>
        public static GlobalPosition Of(GameObject go)
            => Of(go.transform);

        /// <summary>
        /// Creates a <see cref="GlobalPosition"/> instance based on the specified component's transform.
        /// </summary>
        /// <param name="c">The component whose transform is used to determine the global position. Cannot be <see langword="null"/>.</param>
        /// <returns>A <see cref="GlobalPosition"/> representing the global position of the specified component.</returns>
        public static GlobalPosition Of(Component c)
            => Of(c.transform);


        /// <summary>
        /// Projects the global coordinates onto the Y plane, returning a flattened representation.
        /// </summary>
        /// <returns>A <see cref="Vector2"/> representing the 2D projection of the global coordinates.</returns>
        public Vector2 Flat() => GlobalCoordinates.Flat();

        /// <summary>
        /// Creates a new <see cref="GlobalPosition"/> with the same X and Z coordinates as the current instance  and
        /// the Y coordinate of the specified <paramref name="p"/>.
        /// </summary>
        /// <param name="p">The <see cref="GlobalPosition"/> whose Y coordinate will be used in the new position.</param>
        /// <returns>A new <see cref="GlobalPosition"/> with the updated Y coordinate.</returns>
        public GlobalPosition AtYOf(GlobalPosition p) => AtGlobalY(p.Y);
        /// <summary>
        /// Calculates the global position at the specified Y-coordinate of the given transform.
        /// </summary>
        /// <param name="t">The transform whose Y-coordinate is used to determine the global position.</param>
        /// <returns>A <see cref="GlobalPosition"/> representing the position at the specified Y-coordinate.</returns>
        public GlobalPosition AtYOf(Transform t) => AtGlobalY(t.position.y);


        /// <summary>
        /// Converts the current global position to a local position relative to the specified transform.
        /// </summary>
        /// <param name="transform">The transform to which the global position will be converted.</param>
        /// <returns>A <see cref="LocalPosition"/> representing the position relative to the specified transform.</returns>
        public LocalPosition ToLocal(Transform transform)
            => new LocalPosition(transform.InverseTransformPoint(GlobalCoordinates), transform);
        /// <summary>
        /// Converts the global position of the specified <see cref="Behaviour"/> to a local position.
        /// </summary>
        /// <param name="c">The <see cref="Component"/> whose global position will be converted.</param>
        /// <returns>A <see cref="LocalPosition"/> representing the position of the specified <see cref="Behaviour"/>  relative
        /// to the local coordinate system.</returns>
        public LocalPosition ToLocal(Component c)
            => ToLocal(c.transform);

        /// <summary>
        /// Adds a <see cref="Vector3"/> offset to a <see cref="GlobalPosition"/> and returns the resulting position.
        /// </summary>
        /// <param name="a">The initial <see cref="GlobalPosition"/> to which the offset will be applied.</param>
        /// <param name="b">The <see cref="Vector3"/> representing the offset to apply to the global coordinates.</param>
        /// <returns>A new <see cref="GlobalPosition"/> representing the result of adding the offset <paramref name="b"/> to
        /// <paramref name="a"/>.</returns>
        public static GlobalPosition operator +(GlobalPosition a, Vector3 b) => new(a.GlobalCoordinates + b);
        /// <summary>
        /// Subtracts the global coordinates of one <see cref="GlobalPosition"/> from another.
        /// </summary>
        /// <param name="a">The minuend, representing the first <see cref="GlobalPosition"/>.</param>
        /// <param name="b">The subtrahend, representing the second <see cref="GlobalPosition"/>.</param>
        /// <returns>A <see cref="Vector3"/> representing the difference between the global coordinates of <paramref name="a"/>
        /// and <paramref name="b"/>.</returns>
        public static Vector3 operator -(GlobalPosition a, GlobalPosition b) => a.GlobalCoordinates - b.GlobalCoordinates;

        /// <inheritdoc/>
        public override string ToString() => $"G{GlobalCoordinates}";

        /// <summary>
        /// Converts a nullable 3D vector representing global coordinates into a <see cref="GlobalPosition"/> object.
        /// </summary>
        /// <param name="position">The nullable <see cref="Vector3"/> representing the global coordinates. If null, the method returns null.</param>
        /// <returns>A <see cref="GlobalPosition"/> object representing the specified global coordinates, or null if <paramref
        /// name="position"/> is null.</returns>
        public static GlobalPosition? FromGlobalCoordinates(Vector3? position)
        {
            if (position.IsNull())
                return null;
            return new GlobalPosition(position.Value);
        }

        /// <summary>
        /// Creates a ray originating from the current position and extending in the specified direction.
        /// </summary>
        /// <param name="dir">The direction vector in which the ray extends. This vector should be normalized for accurate results.</param>
        /// <returns>A <see cref="Ray"/> instance starting at the current position and pointing in the specified direction.</returns>
        public Ray RayInDirection(Vector3 dir)
            => new Ray(GlobalCoordinates, dir);

        /// <summary>
        /// Creates a new <see cref="GlobalPosition"/> with the specified global Y-coordinate,  while retaining the
        /// current X and Z coordinates.
        /// </summary>
        /// <param name="y">The global Y-coordinate to set for the new position.</param>
        /// <returns>A <see cref="GlobalPosition"/> instance with the specified Y-coordinate and the current X and Z coordinates.</returns>
        public GlobalPosition AtGlobalY(float y)
            => new GlobalPosition(new Vector3(X, y, Z));
    }
}

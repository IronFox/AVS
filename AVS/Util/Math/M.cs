using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using UnityEngine;

namespace AVS.Util.Math;

/// <summary>
/// Math utility functions
/// </summary>
public static class M
{
    /// <summary>
    /// The square root of 2
    /// </summary>
    public static float Sqrt2 { get; } = 1.4142135623730950488016887242097f;

    /// <summary>
    /// Projects a 3D vector onto the XZ plane and returns the resulting 2D vector normalized.
    /// </summary>
    /// <param name="source">The 3D vector to project and normalize.</param>
    /// <returns>A normalized 2D vector representing the projection of the input vector onto the XZ plane.</returns>
    public static Vector2 FlatNormalized(this Vector3 source) => Flat(source).normalized;

    /// <summary>
    /// Projects a 3D vector onto a 2D plane by discarding the Y component.
    /// </summary>
    /// <param name="source">The 3D vector to project onto the XZ plane.</param>
    /// <returns>A 2D vector containing the X and Z components of the source vector.</returns>
    public static Vector2 Flat(this Vector3 source) => new Vector2(source.x, source.z);

    /// <summary>
    /// Converts a 2D vector into a 3D vector by setting the Y component to <paramref name="y"/>.
    /// </summary>
    /// <param name="flat">The 2D vector to convert, where <c>x</c> becomes the X component and <c>y</c> becomes the Z component of the
    /// resulting 3D vector.</param>
    /// <param name="y">The Y component of the resulting 3D vector. Defaults to 0 if not specified.</param>
    /// <returns>A 3D vector with the X and Z components derived from the input vector and the Y component set to 0.</returns>
    public static Vector3 UnFlat(this Vector2 flat, float y = 0) => new Vector3(flat.x, y, flat.y);

    /// <summary>
    /// Calculates a 2D vector that is perpendicular to the specified vector.
    /// </summary>
    /// <param name="flatAxis">The input vector for which to calculate the perpendicular vector.</param>
    /// <returns>A <see cref="Vector2"/> that is perpendicular to <paramref name="flatAxis"/>.</returns>
    public static Vector2 FlatNormal(this Vector2 flatAxis) => new Vector2(-flatAxis.y, flatAxis.x);

    /// <summary>
    /// Creates a new <see cref="Vector3"/> instance with all components set to the specified value.
    /// </summary>
    /// <param name="v">The value to assign to the X, Y, and Z components of the vector.</param>
    /// <returns>A <see cref="Vector3"/> where all components are equal to <paramref name="v"/>.</returns>
    public static Vector3 V3(float v) => new Vector3(v, v, v);

    /// <summary>
    /// Creates a new <see cref="Vector3"/> instance with the specified X, Y, and Z components.
    /// </summary>
    /// <param name="x">The X component of the vector.</param>
    /// <param name="y">The Y component of the vector.</param>
    /// <param name="z">The Z component of the vector.</param>
    /// <returns>A <see cref="Vector3"/> with the specified components.</returns>
    public static Vector3 V3(float x, float y, float z) => new Vector3(x, y, z);
    /// <summary>
    /// Creates a new <see cref="Vector4"/> instance with all components set to the specified value.
    /// </summary>
    /// <param name="v">The value to assign to all components of the vector.</param>
    /// <returns>A <see cref="Vector4"/> where each component is equal to <paramref name="v"/>.</returns>
    public static Vector4 V4(float v) => new Vector4(v, v, v, v);
    /// <summary>
    /// Creates a new <see cref="Vector4"/> instance with the first three components set to the specified value and the
    /// fourth component set to a separate value.
    /// </summary>
    /// <param name="xyz">The value to assign to the X, Y, and Z components of the vector.</param>
    /// <param name="w">The value to assign to the W component of the vector.</param>
    /// <returns>A <see cref="Vector4"/> where the X, Y, and Z components are set to <paramref name="xyz"/> and the W component
    /// is set to <paramref name="w"/>.</returns>
    public static Vector4 V4(float xyz, float w) => new Vector4(xyz, xyz, xyz, w);
    /// <summary>
    /// Creates a new <see cref="Vector4"/> instance from a <see cref="Vector3"/> and a specified w-component.
    /// </summary>
    /// <param name="v">The <see cref="Vector3"/> representing the x, y, and z components of the resulting <see cref="Vector4"/>.</param>
    /// <param name="w">The w-component to include in the resulting <see cref="Vector4"/>.</param>
    /// <returns>A <see cref="Vector4"/> with the x, y, and z components from <paramref name="v"/> and the w-component set to
    /// <paramref name="w"/>.</returns>
    public static Vector4 V4(Vector3 v, float w) => new Vector4(v.x, v.y, v.z, w);
    /// <summary>
    /// Creates a new <see cref="Vector4"/> instance with the specified components.
    /// </summary>
    /// <param name="x">The value for the X component of the vector.</param>
    /// <param name="y">The value for the Y component of the vector.</param>
    /// <param name="z">The value for the Z component of the vector.</param>
    /// <param name="w">The value for the W component of the vector.</param>
    /// <returns>A <see cref="Vector4"/> initialized with the specified X, Y, Z, and W components.</returns>
    public static Vector4 V4(float x, float y, float z, float w) => new Vector4(x, y, z, w);

    /// <summary>
    /// Clamps the specified value to the range [0, 1].
    /// </summary>
    /// <param name="x">The value to clamp.</param>
    /// <returns>The clamped value, which will be between 0 and 1 inclusive.</returns>
    public static float Saturate(float x) => Mathf.Clamp01(x);

    /// <summary>
    /// Performs linear interpolation between two values.
    /// </summary>
    /// <param name="a">The starting value of the interpolation.</param>
    /// <param name="b">The ending value of the interpolation.</param>
    /// <param name="t">The interpolation factor, where 0 represents <paramref name="a"/> and 1 represents <paramref name="b"/>. Values
    /// outside the range [0, 1] will extrapolate beyond the start or end values.</param>
    /// <returns>The interpolated value, calculated as a weighted average of <paramref name="a"/> and <paramref name="b"/>.</returns>
    public static float Interpolate(float a, float b, float t) => a * (1f - t) + b * t;
    /// <summary>
    /// Linearly interpolates between two vectors without clamping the interpolation factor.
    /// </summary>
    /// <param name="a">The starting vector.</param>
    /// <param name="b">The ending vector.</param>
    /// <param name="t">The interpolation factor. Values less than 0 extrapolate beyond <paramref name="a"/>,  and values greater than 1
    /// extrapolate beyond <paramref name="b"/>.</param>
    /// <returns>A vector that is the linear interpolation of <paramref name="a"/> and <paramref name="b"/>  based on the
    /// interpolation factor <paramref name="t"/>.</returns>
    public static Vector3 Interpolate(Vector3 a, Vector3 b, float t) => Vector3.LerpUnclamped(a, b, t);
    /// <summary>
    /// Calculates the square of the specified number.
    /// </summary>
    /// <param name="x">The number to be squared.</param>
    /// <returns>The square of <paramref name="x"/>.</returns>
    public static float Sqr(float x) => x * x;
    /// <summary>
    /// Calculates the squared magnitude of the specified vector.
    /// </summary>
    /// <param name="x">The vector for which to calculate the squared magnitude.</param>
    /// <returns>The squared magnitude of the vector, equivalent to the dot product of the vector with itself.</returns>
    public static float Sqr(Vector3 x) => Vector3.Dot(x, x);
    /// <summary>
    /// Returns the absolute value of the specified single-precision floating-point number.
    /// </summary>
    /// <param name="x">A single-precision floating-point number.</param>
    /// <returns>The absolute value of <paramref name="x"/>. If <paramref name="x"/> is negative, the result is -<paramref
    /// name="x"/>; otherwise, it is <paramref name="x"/>.</returns>
    public static float Abs(float x) => Mathf.Abs(x);
    /// <summary>
    /// Returns a new <see cref="Vector3"/> with each component set to the absolute value of the corresponding component
    /// in the input vector.
    /// </summary>
    /// <param name="v">The input vector whose components will be converted to their absolute values.</param>
    /// <returns>A <see cref="Vector3"/> where each component is the absolute value of the corresponding component in <paramref
    /// name="v"/>.</returns>
    public static Vector3 Abs(Vector3 v) => V3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    /// <summary>
    /// Returns the largest component of the specified vector.
    /// </summary>
    /// <param name="v">The vector from which to determine the maximum component.</param>
    /// <returns>The largest value among the <see cref="Vector3.x"/>, <see cref="Vector3.y"/>, and <see cref="Vector3.z"/>
    /// components of the vector.</returns>
    public static float MaxAxis(this Vector3 v) => Max(v.x, v.y, v.z);
    /// <summary>
    /// Returns the larger of two single-precision floating-point numbers.
    /// </summary>
    /// <param name="x">The first of two single-precision floating-point numbers to compare.</param>
    /// <param name="y">The second of two single-precision floating-point numbers to compare.</param>
    /// <returns>The larger of <paramref name="x"/> and <paramref name="y"/>. If the values are equal, <paramref name="x"/> is
    /// returned.</returns>
    public static float Max(float x, float y) => Mathf.Max(x, y);
    /// <summary>
    /// Returns the largest value among the three specified single-precision floating-point numbers.
    /// </summary>
    /// <param name="x">The first value to compare.</param>
    /// <param name="y">The second value to compare.</param>
    /// <param name="z">The third value to compare.</param>
    /// <returns>The largest of the three specified values.</returns>
    public static float Max(float x, float y, float z) => Mathf.Max(x, Mathf.Max(y, z));
    /// <summary>
    /// Returns a new <see cref="Vector3"/> where each component is the maximum value between the corresponding
    /// component of <paramref name="x"/> and <paramref name="y"/>.
    /// </summary>
    /// <param name="x">The input <see cref="Vector3"/> to compare.</param>
    /// <param name="y">The scalar value to compare against each component of <paramref name="x"/>.</param>
    /// <returns>A <see cref="Vector3"/> where each component is the greater of the corresponding component of <paramref
    /// name="x"/> and <paramref name="y"/>.</returns>
    public static Vector3 Max(Vector3 x, float y) => Max(x, V3(y));
    /// <summary>
    /// Returns a <see cref="Vector3"/> whose components are the maximum values of the corresponding components of two
    /// specified vectors.
    /// </summary>
    /// <param name="a">The first vector to compare.</param>
    /// <param name="b">The second vector to compare.</param>
    /// <returns>A <see cref="Vector3"/> where each component is the greater value of the corresponding components in <paramref
    /// name="a"/> and <paramref name="b"/>.</returns>
    public static Vector3 Max(Vector3 a, Vector3 b)
        => new Vector3(Max(a.x, b.x), Max(a.y, b.y), Max(a.z, b.z));

    /// <summary>
    /// Returns the smaller of two single-precision floating-point numbers.
    /// </summary>
    /// <param name="x">The first of two numbers to compare.</param>
    /// <param name="y">The second of two numbers to compare.</param>
    /// <returns>The smaller of <paramref name="x"/> and <paramref name="y"/>. If the values are equal, <paramref name="x"/> is
    /// returned.</returns>
    public static float Min(float x, float y) => Mathf.Min(x, y);

    /// <summary>
    /// Interpolates smoothly from 0 to 1 based on x compared to a and b.
    /// https://developer.download.nvidia.com/cg/smoothstep.html
    /// </summary>
    /// <param name="a">Minimum reference value(s)</param>
    /// <param name="b">Maximum reference value(s)</param>
    /// <param name="x">Value to compute from</param>
    /// <returns>Interpolated value in [0,1]</returns>
    public static float Smoothstep(float a, float b, float x)
    {
        return Smooth((x - a) / (b - a));
    }

    /// <summary>
    /// Uses the interpolation mechanic of <see cref="Smoothstep(float, float, float)" />
    /// to transform a value in [0,1] to a smooth version in [0,1]
    /// </summary>
    /// <param name="t">Input value in [0,1]. Values outside this range are clamped</param>
    /// <returns>Smooth value</returns>
    public static float Smooth(float t)
    {
        t = Saturate(t);
        return t * t * (3f - (2f * t));
    }

    /// <summary>
    /// Performs a linear interpolation step between two values based on a given input.
    /// </summary>
    /// <remarks>This method does not clamp the result to the range [0, 1]. If clamping is required, the
    /// caller must handle it explicitly.</remarks>
    /// <param name="a">The starting value of the range.</param>
    /// <param name="b">The ending value of the range. Must be greater than <paramref name="a"/>.</param>
    /// <param name="x">The input value to interpolate. Typically expected to be within the range [<paramref name="a"/>, <paramref
    /// name="b"/>].</param>
    /// <returns>A value representing the normalized position of <paramref name="x"/> within the range [<paramref name="a"/>,
    /// <paramref name="b"/>]. Returns a value less than 0 if <paramref name="x"/> is less than <paramref name="a"/>, or
    /// greater than 1 if <paramref name="x"/> exceeds <paramref name="b"/>.</returns>
    public static float LinearStep(float a, float b, float x)
    {
        return (x - a) / (b - a);
    }

    /// <summary>
    /// Calculates the dot product of two 3D vectors.
    /// </summary>
    /// <param name="right">The first vector in the dot product operation.</param>
    /// <param name="delta">The second vector in the dot product operation.</param>
    /// <returns>The dot product of the two vectors as a single-precision floating-point value.</returns>
    public static float Dot(Vector3 right, Vector3 delta)
        => Vector3.Dot(right, delta);

    /// <summary>
    /// Calculates the distance from a point to a ray and the position along the ray where the distance is measured.
    /// </summary>
    /// <remarks>The returned <see cref="RayDistance"/> includes: <list type="bullet"> <item>
    /// <description><c>Along</c>: The scalar value representing the position along the ray's direction
    /// vector.</description> </item> <item> <description><c>Distance</c>: The perpendicular distance from the point to
    /// the ray.</description> </item> </list> The ray's direction is assumed to be normalized. If the direction is not
    /// normalized, the <c>Along</c> value  will be scaled accordingly.</remarks>
    /// <param name="ray">The ray to measure the distance from. The ray is defined by an origin and a direction.</param>
    /// <param name="point">The point in 3D space to measure the distance to.</param>
    /// <returns>A <see cref="RayDistance"/> object containing the position along the ray and the perpendicular distance  from
    /// the point to the ray.</returns>
    public static RayDistance Distance(Ray ray, Vector3 point)
    {
        var d = point - ray.origin;
        var along = Dot(ray.direction, d);
        var onRay = ray.GetPoint(along);
        var cross = point - onRay;
        return new RayDistance(along, cross.magnitude);
    }

    /// <summary>
    /// Calculates the Euclidean distance between two 3D points.
    /// </summary>
    /// <param name="a">The first point represented as a <see cref="Vector3"/>.</param>
    /// <param name="b">The second point represented as a <see cref="Vector3"/>.</param>
    /// <returns>The distance between the two points as a <see cref="float"/>.</returns>
    public static float Distance(Vector3 a, Vector3 b)
        => Vector3.Distance(a, b);

    /// <summary>
    /// Calculates the squared distance between two 3D points.
    /// </summary>
    /// <remarks>The squared distance is calculated without taking the square root, making it more efficient 
    /// than calculating the actual distance. This is useful in scenarios where the exact distance  is not required,
    /// such as comparing relative distances.</remarks>
    /// <param name="a">The first point represented as a <see cref="Vector3"/>.</param>
    /// <param name="b">The second point represented as a <see cref="Vector3"/>.</param>
    /// <returns>The squared distance between the two points as a <see cref="float"/>.</returns>
    public static float SqrDistance(Vector3 a, Vector3 b)
        => Sqr(a.x - b.x) + Sqr(a.y - b.y) + Sqr(a.z - b.z);

    /// <summary>
    /// Converts an angle from degrees to radians.
    /// </summary>
    /// <param name="deg">The angle in degrees to convert.</param>
    /// <returns>The equivalent angle in radians.</returns>
    public static float DegToRad(float deg)
        => deg * Mathf.Deg2Rad;
    /// <summary>
    /// Converts an angle from radians to degrees.
    /// </summary>
    /// <param name="rad">The angle in radians to convert.</param>
    /// <returns>The equivalent angle in degrees.</returns>
    public static float RadToDeg(float rad)
        => rad * Mathf.Rad2Deg;


    /// <summary>
    /// Solves a quadratic equation of the form ax² + bx + c = 0 and returns the solution(s), if any.
    /// </summary>
    /// <remarks>This method handles special cases where the quadratic coefficient (<paramref name="a"/>) is
    /// approximately zero: - If both <paramref name="a"/> and <paramref name="b"/> are approximately zero, the equation
    /// is invalid, and no solutions are returned. - If <paramref name="a"/> is approximately zero but <paramref
    /// name="b"/> is not, the equation is treated as a linear equation, and a single solution is returned.</remarks>
    /// <param name="a">The coefficient of the quadratic term (x²). Must not be approximately zero.</param>
    /// <param name="b">The coefficient of the linear term (x).</param>
    /// <param name="c">The constant term.</param>
    /// <returns>A <see cref="QuadraticSolution"/> object representing the solution(s) to the quadratic equation. If the equation
    /// has no real solutions, the result will indicate no solutions. If the equation has one solution, the result will
    /// contain that solution. If the equation has two solutions, the result will contain both solutions.</returns>
    public static QuadraticSolution SolveQuadraticEquation(float a, float b, float c)
    {
        if (Mathf.Abs(a) < Mathf.Epsilon)
        {
            //0 = b * x + c
            //x = -c / b
            if (Mathf.Abs(b) < Mathf.Epsilon)
                return default;
            return QuadraticSolution.One(-c / b);
        }


        var root = b * b - 4 * a * c;
        if (root < 0)
            return default;
        var a2 = a * 2;
        if (root <= Mathf.Epsilon)
            return QuadraticSolution.One(-b / a2);

        root = Mathf.Sqrt(root);
        var x0 = (-b - root) / a2;
        var x1 = (-b + root) / a2;
        return QuadraticSolution.Two(x0, x1);
    }

    /// <summary>
    /// Rounds the specified floating-point value to the given number of fractional digits.
    /// </summary>
    /// <param name="v">The floating-point value to round.</param>
    /// <param name="digits">The number of fractional digits to round to. Must be zero or greater.</param>
    /// <returns>The rounded value with the specified number of fractional digits.</returns>
    public static float Round(float v, int digits)
    {
        float scale = Mathf.Pow(10, digits);
        return Mathf.Round(v * scale) / scale;
    }

    /// <summary>
    /// Creates a new <see cref="Vector2"/> instance with both components set to the specified value.
    /// </summary>
    /// <param name="v">The value to assign to both the X and Y components of the vector.</param>
    /// <returns>A <see cref="Vector2"/> where both components are equal to <paramref name="v"/>.</returns>
    public static Vector2 V2(float v) => new Vector2(v, v);

    /// <summary>
    /// Creates a new <see cref="Vector2"/> instance with the specified X and Y components.
    /// </summary>
    /// <param name="x">The X component of the vector.</param>
    /// <param name="y">The Y component of the vector.</param>
    /// <returns>A <see cref="Vector2"/> with the specified X and Y components.</returns>
    public static Vector2 V2(float x, float y) => new Vector2(x, y);

    /// <summary>
    /// Raises each component of the specified vector to the given power.
    /// </summary>
    /// <param name="v">The vector whose components will be raised to the specified power.</param>
    /// <param name="exp">The exponent to which each component of the vector is raised.</param>
    /// <returns>A new <see cref="Vector3"/> where each component is the result of raising the corresponding component of
    /// <paramref name="v"/> to the power of <paramref name="exp"/>.</returns>
    public static Vector3 Pow(Vector3 v, float exp)
    {
        return V3(
            Mathf.Pow(v.x, exp),
            Mathf.Pow(v.y, exp),
            Mathf.Pow(v.z, exp)
            );
    }

    /// <summary>
    /// Multiplies the corresponding components of two vectors.
    /// </summary>
    /// <param name="v">The first vector to multiply.</param>
    /// <param name="w">The second vector to multiply.</param>
    /// <returns>A <see cref="Vector3"/> where each component is the product of the corresponding components of <paramref
    /// name="v"/> and <paramref name="w"/>.</returns>
    public static Vector3 Mult(Vector3 v, Vector3 w)
    {
        return V3(
            v.x * w.x,
            v.y * w.y,
            v.z * w.z
            );
    }

    /// <summary>
    /// Computes the component-wise product of three 3D vectors.
    /// </summary>
    /// <param name="u">The first vector to multiply.</param>
    /// <param name="v">The second vector to multiply.</param>
    /// <param name="w">The third vector to multiply.</param>
    /// <returns>A <see cref="Vector3"/> representing the component-wise product of <paramref name="u"/>, <paramref name="v"/>,
    /// and <paramref name="w"/>.</returns>
    public static Vector3 Mult(Vector3 u, Vector3 v, Vector3 w)
    {
        return V3(
            u.x * v.x * w.x,
            u.y * v.y * w.y,
            u.z * v.z * w.z
            );
    }

    /// <summary>
    /// Multiplies the corresponding components of four <see cref="Vector3"/> instances.
    /// </summary>
    /// <param name="u">The first <see cref="Vector3"/> instance.</param>
    /// <param name="v">The second <see cref="Vector3"/> instance.</param>
    /// <param name="w">The third <see cref="Vector3"/> instance.</param>
    /// <param name="x">The fourth <see cref="Vector3"/> instance.</param>
    /// <returns>A new <see cref="Vector3"/> where each component is the product of the corresponding components of  <paramref
    /// name="u"/>, <paramref name="v"/>, <paramref name="w"/>, and <paramref name="x"/>.</returns>
    public static Vector3 Mult(Vector3 u, Vector3 v, Vector3 w, Vector3 x)
    {
        return V3(
            u.x * v.x * w.x * x.x,
            u.y * v.y * w.y * x.y,
            u.z * v.z * w.z * x.z
            );
    }

    /// <summary>
    /// Returns the signed minimum of the specified value and a limit.
    /// </summary>
    /// <remarks>This method ensures that the result has the same sign as <paramref name="signedValue"/>.  If
    /// the absolute value of <paramref name="signedValue"/> is less than or equal to <paramref name="limit"/>,  the
    /// result is <paramref name="signedValue"/>. Otherwise, the result is the product of the sign of  <paramref
    /// name="signedValue"/> and <paramref name="limit"/>.</remarks>
    /// <param name="signedValue">The value whose sign is preserved and whose magnitude is compared to the limit.</param>
    /// <param name="limit">The maximum allowable magnitude for the result. Must be non-negative.</param>
    /// <returns>The result of clamping the magnitude of <paramref name="signedValue"/> to the specified <paramref
    /// name="limit"/>,  while preserving its sign.</returns>
    public static float SignedMin(float signedValue, float limit)
    {
        return Mathf.Sign(signedValue) * Mathf.Min(Mathf.Abs(signedValue), limit);
    }

    /// <summary>
    /// Adjusts the position of a vector to ensure it is at least a specified minimum distance from a reference point.
    /// </summary>
    /// <remarks>If the distance between <paramref name="from"/> and <paramref name="what"/> is zero, the
    /// method returns <paramref name="what"/> unchanged.</remarks>
    /// <param name="from">The reference point from which the distance is measured.</param>
    /// <param name="what">The vector to adjust if it is too close to the reference point.</param>
    /// <param name="toMinDistance">The minimum allowable distance between <paramref name="from"/> and <paramref name="what"/>.</param>
    /// <returns>The adjusted vector if <paramref name="what"/> is closer than <paramref name="toMinDistance"/> to <paramref
    /// name="from"/>; otherwise, returns <paramref name="what"/> unchanged.</returns>
    public static Vector3 Push(Vector3 from, Vector3 what, float toMinDistance)
    {
        var fromToWhat = what - from;
        float d2 = fromToWhat.sqrMagnitude;
        if (d2 > Sqr(toMinDistance))
            return what;
        if (d2 == 0)    //can't push
            return what;
        var d = Mathf.Sqrt(d2);
        return from + fromToWhat * toMinDistance / d;
    }


    /// <summary>
    /// Clamps a value to ensure it falls within the specified range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum allowable value.</param>
    /// <param name="max">The maximum allowable value.</param>
    /// <returns>The clamped value, which will be no less than <paramref name="min"/> and no greater than <paramref name="max"/>.</returns>
    public static float Clamp(float value, float min, float max)
        => Mathf.Clamp(value, min, max);

    /// <summary>
    /// Compares the components of two <see cref="Vector3"/> instances and determines if each component of the first
    /// vector is greater than the corresponding component of the second vector.
    /// </summary>
    /// <param name="a">The first <see cref="Vector3"/> to compare.</param>
    /// <param name="b">The second <see cref="Vector3"/> to compare.</param>
    /// <returns>A <see cref="Bool3"/> where each component indicates whether the corresponding component of <paramref name="a"/>
    /// is greater than that of <paramref name="b"/>.</returns>
    public static Bool3 Greater(this Vector3 a, Vector3 b)
        => new Bool3(a.x > b.x, a.y > b.y, a.z > b.z);

    /// <summary>
    /// Compares two <see cref="Vector3"/> instances component-wise to determine if each component of the first vector 
    /// is greater than or equal to the corresponding component of the second vector.
    /// </summary>
    /// <param name="a">The first <see cref="Vector3"/> to compare.</param>
    /// <param name="b">The second <see cref="Vector3"/> to compare.</param>
    /// <returns>A <see cref="Bool3"/> where each component indicates whether the corresponding component of <paramref name="a"/>
    /// is greater than or equal to the corresponding component of <paramref name="b"/>.</returns>
    public static Bool3 GreaterOrEqual(this Vector3 a, Vector3 b)
        => new Bool3(a.x >= b.x, a.y >= b.y, a.z >= b.z);

    /// <summary>
    /// Determines whether the corresponding components of two <see cref="Vector3"/> instances are equal.
    /// </summary>
    /// <param name="a">The first <see cref="Vector3"/> instance to compare.</param>
    /// <param name="b">The second <see cref="Vector3"/> instance to compare.</param>
    /// <returns>A <see cref="Bool3"/> instance where each component indicates whether the corresponding components of <paramref
    /// name="a"/> and <paramref name="b"/> are equal.</returns>
    public static Bool3 Equal(this Vector3 a, Vector3 b)
        => new Bool3(a.x == b.x, a.y == b.y, a.z == b.z);

    /// <summary>
    /// Compares the components of two <see cref="Vector3"/> instances and determines if each component of the first
    /// vector is less than the corresponding component of the second vector.
    /// </summary>
    /// <param name="a">The first <see cref="Vector3"/> to compare.</param>
    /// <param name="b">The second <see cref="Vector3"/> to compare.</param>
    /// <returns>A <see cref="Bool3"/> where each component indicates whether the corresponding component of <paramref name="a"/>
    /// is less than the corresponding component of <paramref name="b"/>.</returns>
    public static Bool3 Less(this Vector3 a, Vector3 b)
        => new Bool3(a.x < b.x, a.y < b.y, a.z < b.z);

    /// <summary>
    /// Compares two <see cref="Vector3"/> instances component-wise and determines if each component of the first vector
    /// is less than or equal to the corresponding component of the second vector.
    /// </summary>
    /// <param name="a">The first <see cref="Vector3"/> to compare.</param>
    /// <param name="b">The second <see cref="Vector3"/> to compare.</param>
    /// <returns>A <see cref="Bool3"/> where each component indicates whether the corresponding component of <paramref name="a"/>
    /// is less than or equal to the corresponding component of <paramref name="b"/>.</returns>
    public static Bool3 LessOrEqual(this Vector3 a, Vector3 b)
        => new Bool3(a.x <= b.x, a.y <= b.y, a.z <= b.z);

    /// <summary>
    /// Converts the specified single-precision floating-point value to its string representation  using the invariant
    /// culture.
    /// </summary>
    /// <param name="v">The single-precision floating-point value to convert.</param>
    /// <returns>A string representation of the specified value formatted using the invariant culture.</returns>
    public static string ToStr(this float v)
        => v.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts the specified double-precision floating-point value to its string representation  using the invariant
    /// culture.
    /// </summary>
    /// <param name="v">The double-precision floating-point value to convert.</param>
    /// <returns>A string representation of the specified value formatted using the invariant culture.</returns>
    public static string ToStr(this double v)
        => v.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Creates a grayscale color with the specified intensity.
    /// </summary>
    /// <param name="intensity">The intensity of the grayscale color. Must be a value between 0.0 and 1.0, where 0.0 represents black and 1.0
    /// represents white.</param>
    /// <returns>A <see cref="Color"/> instance representing the grayscale color with the specified intensity.</returns>
    public static Color Gray(float intensity)
        => new Color(intensity, intensity, intensity, 1f);


    /// <summary>
    /// Creates a new <see cref="Color"/> instance with the specified alpha value while preserving the original color's
    /// RGB components.
    /// </summary>
    /// <param name="color">The base color whose red, green, and blue components will be used.</param>
    /// <param name="alpha">The alpha (transparency) value to apply to the new color. Must be between 0.0 and 1.0, inclusive.</param>
    /// <returns>A new <see cref="Color"/> instance with the same RGB components as <paramref name="color"/> and the specified
    /// alpha value.</returns>
    public static Color Color(Color color, float alpha)
        => new Color(color.r, color.g, color.b, alpha);

    /// <summary>
    /// Scales the red, green, and blue components of the specified color by the given factor.
    /// </summary>
    /// <param name="color">The color whose RGB components will be scaled.</param>
    /// <param name="scale">The factor by which to scale the RGB components. Must be a non-negative value.</param>
    /// <returns>A new <see cref="Color"/> with the scaled RGB components. The alpha component remains unchanged.</returns>
    public static Color ScaleRGB(Color color, float scale)
        => new Color(
            color.r * scale,
            color.g * scale,
            color.b * scale,
            color.a
        );


    /// <summary>
    /// Parses a string into a float using the universal decimal sign (.)
    /// </summary>
    /// <param name="s">String to try parse</param>
    /// <param name="f">Resulting float</param>
    /// <returns>True on success</returns>
    public static bool ToFloat(this string s, out float f) =>
        float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out f);

    /// <summary>
    /// Converts a string into a float using the universal decimal sign (.)
    /// </summary>
    /// <param name="s">String to parse</param>
    /// <returns>Converted float</returns>
    public static float ToFloat(this string s) => float.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);

    /// <summary>
    /// Produces a percentage string from a float value.
    /// If the max value is zero, it returns "-%".
    /// Rounds the percentage to two decimal places.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="max"></param>
    /// <returns>String in the form "[v]%" where [v] is either 1.23 or - </returns>
    public static string Percentage(this float x, float max)
    {
        if (max == 0f) return "-%";

        return (x / max).ToString("#.#%", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Produces a percentage string from a LiveMixin's current health status.
    /// </summary>
    /// <remarks>
    /// If the <paramref name="live"/> is null, has no max health or is invincible,
    /// it returns "-%".
    /// </remarks>
    /// <param name="live">Live mixin to produce the health percent of</param>
    /// <returns>String in the form "[v]%" where [v] is either 1.23 or - </returns>
    public static string Percentage(this LiveMixin? live)
    {
        if (live.IsNull()) return "-%";
        if (live.maxHealth <= 0 || live.invincible) return "-%";
        return (live.health / live.maxHealth).ToString("#.#%", CultureInfo.CurrentCulture);
    }
}


/// <summary>
/// Represents the solution(s) to a quadratic equation, which may include zero, one, or two real roots.
/// </summary>
/// <remarks>This type encapsulates the roots of a quadratic equation as nullable floating-point values. If a root
/// is not present, the corresponding value will be <see langword="null" />. Use the provided properties and methods to
/// determine the nature of the solution.</remarks>
/// <param name="X0">The first solution, if any. Never <see langword="null" /> if <paramref name="X1"/> is not <see langword="null" /></param>
/// <param name="X1">The second solution, if any. Always <see langword="null" /> if <paramref name="X0"/> is <see langword="null" /></param>
public readonly record struct QuadraticSolution(float? X0, float? X1)
{
    /// <summary>
    /// Gets a value indicating whether any solution exists.
    /// </summary>
    public bool HasAnySolution => X0.HasValue;

    private static bool IsNonNegative([NotNullWhen(true)] float? x)
        => x.HasValue && x.Value >= 0;

    /// <summary>
    /// Gets the smallest non-negative value among <see cref="X0"/> and <see cref="X1"/>, or <c>null</c> if neither
    /// value is non-negative.
    /// </summary>
    public float? SmallestNonNegative =>
        IsNonNegative(X0)
            ? IsNonNegative(X1)
                ? Mathf.Min(X1.Value, X1.Value)
                : X0.Value
            : IsNonNegative(X1)
                ? X1.Value
                : (float?)null;

    /// <summary>
    /// Creates a <see cref="QuadraticSolution"/> instance representing the two solutions of a quadratic equation.
    /// </summary>
    /// <param name="x0">The first solution of the quadratic equation.</param>
    /// <param name="x1">The second solution of the quadratic equation.</param>
    /// <returns>A <see cref="QuadraticSolution"/> containing the specified solutions.</returns>

    public static QuadraticSolution Two(float x0, float x1)
        => new(x0, x1);

    /// <summary>
    /// Creates a <see cref="QuadraticSolution"/> representing a single real root of a quadratic equation.
    /// </summary>
    /// <param name="x">The value of the single real root.</param>
    /// <returns>A <see cref="QuadraticSolution"/> instance containing the specified root and no additional solutions.</returns>
    public static QuadraticSolution One(float x)
        => new(x, null);


}


/// <summary>
/// Represents the distances associated with a ray, including the distance along the ray and the distance to the closest
/// point on the ray.
/// </summary>
/// <remarks>This structure encapsulates two distance measurements related to a ray: <list type="bullet"> <item>
/// <description><see cref="DistanceAlongRay"/>: The distance from the ray's origin to a specific point along the
/// ray.</description> </item> <item> <description><see cref="DistanceToClosesPointOnRay"/>: The perpendicular distance
/// from a point to the closest point on the ray.</description> </item> </list> Use this structure to represent and work
/// with ray-related distance calculations in geometric or physics-based computations.</remarks>
/// <param name="DistanceAlongRay">The distance from the ray's origin to a specific point along the ray.</param>
/// <param name="DistanceToClosesPointOnRay">The perpendicular distance from a point to the closest point on the ray.</param>
public readonly record struct RayDistance(float DistanceAlongRay, float DistanceToClosesPointOnRay);


/// <summary>
/// Represents a three-dimensional boolean value, where each dimension (X, Y, Z) is independently true or false.
/// </summary>
/// <remarks>This struct provides logical operations for three boolean values, such as AND, OR, and NOT, as well
/// as properties to evaluate aggregate conditions (e.g., whether all values are true, any value is true, or none are
/// true). It is immutable and implements <see cref="IEquatable{T}"/> for equality comparison.</remarks>
/// <param name="X"></param>
/// <param name="Y"></param>
/// <param name="Z"></param>
public readonly record struct Bool3(bool X, bool Y, bool Z)
{

    /// <summary>
    /// Performs a logical AND operation between this instance and the specified <see cref="Bool3"/> instance.
    /// </summary>
    /// <param name="other">The <see cref="Bool3"/> instance to logically AND with this instance.</param>
    /// <returns>A new <see cref="Bool3"/> instance where each component is the result of a logical AND operation  between the
    /// corresponding components of this instance and the <paramref name="other"/> instance.</returns>
    public Bool3 And(Bool3 other)
        => new Bool3(
            X && other.X,
            Y && other.Y,
            Z && other.Z
            );

    /// <summary>
    /// Performs a logical OR operation between the corresponding components of this instance and the specified <see
    /// cref="Bool3"/>.
    /// </summary>
    /// <param name="other">The <see cref="Bool3"/> instance to combine with this instance.</param>
    /// <returns>A new <see cref="Bool3"/> instance where each component is the result of a logical OR operation between the
    /// corresponding components of this instance and <paramref name="other"/>.</returns>
    public Bool3 Or(Bool3 other)
        => new Bool3(
            X || other.X,
            Y || other.Y,
            Z || other.Z
            );

    /// <summary>
    /// Negates the logical values of the components of the specified <see cref="Bool3"/> instance.
    /// </summary>
    /// <param name="b">The <see cref="Bool3"/> instance to negate.</param>
    /// <returns>A new <see cref="Bool3"/> instance with each component set to the logical negation of the corresponding
    /// component in <paramref name="b"/>.</returns>
    public static Bool3 operator !(Bool3 b)
        => new Bool3(!b.X, !b.Y, !b.Z);

    /// <summary>
    /// Gets a value indicating whether all conditions are satisfied.
    /// </summary>
    public bool All => X && Y && Z;
    /// <summary>
    /// Gets a value indicating whether at least one of the conditions X, Y, or Z is not satisfied.
    /// </summary>
    public bool NotAll => !X || !Y || !Z;
    /// <summary>
    /// Gets a value indicating whether any of the specified conditions are true.
    /// </summary>
    public bool Any => X || Y || Z;
    /// <summary>
    /// Gets a value indicating whether all conditions are false.
    /// </summary>
    public bool None => !X && !Y && !Z;
}
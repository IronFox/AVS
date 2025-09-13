using AVS.Log;
using AVS.Util;
using AVS.Util.Math;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Behavior.Util.Math
{

    /// <summary>
    /// Provides utility methods for working with <see cref="Bounds"/> and related types, such as calculating bounds
    /// relationships, transforming bounds, and retrieving corner points.
    /// </summary>
    /// <remarks>This class includes extension methods for <see cref="Bounds"/>, <see cref="BoxCollider"/>, 
    /// <see cref="CapsuleCollider"/>, and other related types. These methods are designed to simplify  common
    /// operations such as determining containment, translating bounds, and computing corner points.</remarks>
    public static class BoundsUtil
    {
        internal static bool Contains(this Bounds big, Bounds small)
        {
            return small.min.GreaterOrEqual(big.min).All
                && small.max.LessOrEqual(big.max).All;
        }

        internal static Bounds TranslatedBy(this Bounds b, Vector3 delta)
            => new Bounds(b.center + delta, b.size);


        internal static IEnumerable<Vector3> GetCornerPoints(this Bounds bounds)
        {
            yield return M.V3(bounds.min.x, bounds.min.y, bounds.min.z);
            yield return M.V3(bounds.max.x, bounds.min.y, bounds.min.z);
            yield return M.V3(bounds.min.x, bounds.max.y, bounds.min.z);
            yield return M.V3(bounds.max.x, bounds.max.y, bounds.min.z);
            yield return M.V3(bounds.min.x, bounds.min.y, bounds.max.z);
            yield return M.V3(bounds.max.x, bounds.min.y, bounds.max.z);
            yield return M.V3(bounds.min.x, bounds.max.y, bounds.max.z);
            yield return M.V3(bounds.max.x, bounds.max.y, bounds.max.z);
        }

        internal static IEnumerable<Vector3> GetCornerPoints(this BoxCollider c)
        {
            var s = c.size / 2;
            var p = c.center;
            yield return M.V3(p.x + s.x, p.y + s.y, p.z + s.z);
            yield return M.V3(p.x + s.x, p.y + s.y, p.z - s.z);
            yield return M.V3(p.x + s.x, p.y - s.y, p.z + s.z);
            yield return M.V3(p.x + s.x, p.y - s.y, p.z - s.z);
            yield return M.V3(p.x - s.x, p.y + s.y, p.z + s.z);
            yield return M.V3(p.x - s.x, p.y + s.y, p.z - s.z);
            yield return M.V3(p.x - s.x, p.y - s.y, p.z + s.z);
            yield return M.V3(p.x - s.x, p.y - s.y, p.z - s.z);
        }


        internal static IEnumerable<Vector3> GetCornerPoints(this CapsuleCollider c)
        {


            var r = c.radius;
            var p = c.center;
            var h = c.height / 2 + r;
            switch (c.direction)
            {
                case 0:
                    yield return M.V3(p.x + h, p.y + r, p.z + r);
                    yield return M.V3(p.x + h, p.y + r, p.z - r);
                    yield return M.V3(p.x + h, p.y - r, p.z + r);
                    yield return M.V3(p.x + h, p.y - r, p.z - r);
                    yield return M.V3(p.x - h, p.y + r, p.z + r);
                    yield return M.V3(p.x - h, p.y + r, p.z - r);
                    yield return M.V3(p.x - h, p.y - r, p.z + r);
                    yield return M.V3(p.x - h, p.y - r, p.z - r);
                    break;
                case 1:
                    yield return M.V3(p.x + r, p.y + h, p.z + r);
                    yield return M.V3(p.x + r, p.y + h, p.z - r);
                    yield return M.V3(p.x + r, p.y - h, p.z + r);
                    yield return M.V3(p.x + r, p.y - h, p.z - r);
                    yield return M.V3(p.x - r, p.y + h, p.z + r);
                    yield return M.V3(p.x - r, p.y + h, p.z - r);
                    yield return M.V3(p.x - r, p.y - h, p.z + r);
                    yield return M.V3(p.x - r, p.y - h, p.z - r);
                    break;
                case 2:
                    yield return M.V3(p.x + r, p.y + r, p.z + h);
                    yield return M.V3(p.x + r, p.y + r, p.z - h);
                    yield return M.V3(p.x + r, p.y - r, p.z + h);
                    yield return M.V3(p.x + r, p.y - r, p.z - h);
                    yield return M.V3(p.x - r, p.y + r, p.z + h);
                    yield return M.V3(p.x - r, p.y + r, p.z - h);
                    yield return M.V3(p.x - r, p.y - r, p.z + h);
                    yield return M.V3(p.x - r, p.y - r, p.z - h);
                    break;

            }
        }



        //public static IEnumerable<Vector3> GetCornerPoints(this Collider c)
        //{
        //    return GetCornerPoints(c.bounds);
        //}

        internal static Matrix4x4 ToLocalMatrix(this Transform t)
        {
            return Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
        }

        internal static bool IsTooBig(Bounds bounds)
        {
            //return bounds.size.x > 5.5f || bounds.size.y > 3.0f || bounds.size.z > 8.9f; 
            return M.MaxAxis(bounds.extents) > 10f;
        }

        private static void RecurseComputeBounds(
            AVS.RootModController rmc,
            Matrix4x4 matrixToRoot,
            Transform transform,
            ref Bounds bounds,
            bool includeRenderers,
            bool includeColliders,
            bool includeInactiveGameObjects,
            bool includeDisabledColliders,
            Transform? excludeFrom)
        {
            using var log = SmartLog.LazyForAVS(rmc, parameters: Params.Of(transform, includeRenderers, includeColliders, excludeFrom));
            {
                if (transform == excludeFrom)
                    return;
                string t = $"matrix {matrixToRoot}, computed from scale {transform.localScale} on {transform.NiceName()}";
                if (includeRenderers)
                {
                    // var mf = transform.GetComponent<MeshFilter>();
                    // var r = transform.GetComponent<Renderer>();
                    // var l = transform.GetComponent<Light>();
                    //
                    // if (r && mf && !l && (!r.name.Contains("Volum") || !r.name.Contains("Light")) && r.enabled && mf.mesh)
                    // {
                    //     var wasTooBig = IsTooBig(bounds);
                    //     foreach (var corner in mf.mesh.bounds.GetCornerPoints())
                    //         bounds.Encapsulate(matrixToRoot * corner);
                    //     if (!wasTooBig && IsTooBig(bounds))
                    //         Log.Default.LogError(
                    //             $"Computed bounds have gotten too large ({bounds}) after using renderer {r.NiceName()} bounds {mf.mesh.bounds} on {t}");
                    // }

                }

                if (includeColliders)
                {
                    var c = transform.GetComponent<Collider>();
                    if (c && (c.enabled || includeDisabledColliders) && !c.isTrigger)
                    {
                        //                        log.Debug($"Including {c.NiceName()} @{c.transform.localPosition} @{matrixToRoot}");
                        var wasTooBig = IsTooBig(bounds);


                        switch (c)
                        {
                            case MeshCollider mc:
                                foreach (var corner in mc.sharedMesh.bounds.GetCornerPoints())
                                    bounds.Encapsulate(matrixToRoot * M.V4(corner, 1));
                                if (!wasTooBig && IsTooBig(bounds))
                                    log.Error(
                                        $"Computed bounds have gotten too large ({bounds}) after using collider bounds {mc.sharedMesh.bounds} on {t}");
                                break;
                            case BoxCollider box:
                                foreach (var corner in box.GetCornerPoints())
                                    bounds.Encapsulate(matrixToRoot * M.V4(corner, 1));
                                if (!wasTooBig && IsTooBig(bounds))
                                    log.Error(
                                        $"Computed bounds have gotten too large ({bounds}) after using collider box {box.center}-{box.size} on {t}");
                                break;
                            case SphereCollider sphere:
                                {

                                    var center = matrixToRoot * M.V4(sphere.center, 1);
                                    var radius3 = M.Abs((Vector3)(matrixToRoot * M.V4(sphere.radius, 0)));
                                    bounds.Encapsulate(new Bounds(center, radius3 * 2));
                                    if (!wasTooBig && IsTooBig(bounds))
                                        log.Error(
                                            $"Computed bounds have gotten too large ({bounds}) after using collider sphere {sphere.center} r{sphere.radius} on {t}");
                                }
                                break;
                            case CapsuleCollider capsule:
                                foreach (var corner in capsule.GetCornerPoints())
                                    bounds.Encapsulate(matrixToRoot * M.V4(corner, 1));
                                if (!wasTooBig && IsTooBig(bounds))
                                    log.Error(
                                        $"Computed bounds have gotten too large ({bounds}) after using collider capsule {capsule.center} r{capsule.radius} h{capsule.height} on {t}");
                                break;
                            default:
                                log.Debug($"Ignoring unsupported collider type {c.GetType().Name} on {t}");
                                break;

                        }

                    }
                    //else
                    //    log.Debug($"Skipping collider {c.NiceName()} on {transform.NiceName()} - no collider, disabled or trigger ({c.SafeGet(x => x.enabled, false)}, {c.SafeGet(x => x.isTrigger, false)})");
                }

                foreach (var child in transform.SafeGetChildren())
                {
                    if (!child.gameObject.activeSelf && !includeInactiveGameObjects)
                        continue;

                    RecurseComputeBounds(rmc,
                        matrixToRoot * child.ToLocalMatrix(),
                        child,
                        ref bounds,
                        includeRenderers: includeRenderers,
                        includeColliders: includeColliders,
                        excludeFrom: excludeFrom,
                        includeInactiveGameObjects: includeInactiveGameObjects,
                        includeDisabledColliders: includeDisabledColliders);
                    //.Where(x => x.enabled)
                    //matrixToRoot = matrixToRoot* transform.ToLocalMatrix();
                }
            }

        }




        /// <summary>
        /// Computes the scaled local bounds of a transform hierarchy, optionally including renderers and colliders.
        /// </summary>
        /// <param name="rootTransform">The root transform of the hierarchy for which to compute the bounds.</param>
        /// <param name="rmc">The root mod controller used to manage the hierarchy during bounds computation.</param>
        /// <param name="includeRenderers">If <see langword="true"/>, the bounds will include the contributions of renderers in the hierarchy;
        /// otherwise, renderers are ignored.</param>
        /// <param name="includeColliders">If <see langword="true"/>, the bounds will include the contributions of colliders in the hierarchy;
        /// otherwise, colliders are ignored.</param>
        /// <param name="excludeFrom">An optional transform to exclude from the bounds computation, along with its children. If <see
        /// langword="null"/>, no exclusions are applied.</param>
        /// <param name="applyLocalScale">If <see langword="true"/>, the local scale of the root transform is applied to the bounds computation;
        /// otherwise, the scale is ignored.</param>
        /// <param name="includeDisabledColliders">If <see langword="true"/>, disabled colliders are included during the bounds computation; ignored otherwise</param>
        /// <param name="includeInactiveGameObjects">If <see langword="true"/>, inactive child transforms are included during the bounds computation; ignored otherwise</param>
        /// <returns>A <see cref="Bounds3"/> representing the computed local bounds of the transform hierarchy.</returns>
        public static Bounds3 ComputeScaledLocalBounds(
            this Transform rootTransform,
            AVS.RootModController rmc,
            bool includeRenderers,
            bool includeColliders,
            Transform? excludeFrom,
            bool includeDisabledColliders = false,
            bool applyLocalScale = true,
            bool includeInactiveGameObjects = false)
        {
            Bounds bounds = new Bounds(Vector3.zero, M.V3(0));

            RecurseComputeBounds(
                rmc,
                Matrix4x4.TRS(Vector3.zero, Quaternion.identity, applyLocalScale ? rootTransform.localScale : Vector3.one),
                rootTransform,
                ref bounds,
                includeRenderers: includeRenderers,
                includeColliders: includeColliders,
                excludeFrom: excludeFrom,
                includeInactiveGameObjects: includeInactiveGameObjects,
                includeDisabledColliders: includeDisabledColliders);

            return Bounds3.From(bounds);
        }
    }
}
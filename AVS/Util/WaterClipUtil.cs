using AVS.Assets;
using AVS.Log;
using System;
using UnityEngine;

namespace AVS.Util
{
    /// <summary>
    /// Utility class for binding a WaterClipProxy to a target GameObject.
    /// </summary>
    public static class WaterClipUtil
    {
        /// <summary>
        /// Binds a WaterClipProxy to the specified target GameObject using the provided distance map and bounds.
        /// </summary>
        /// <remarks>
        /// The Seamoth has to be loaded before this method is called.
        /// </remarks>
        /// <param name="log">Out logger</param>
        /// <param name="target">Target object that should contain the WaterClipProxy and its renderers. Should not contain any components</param>
        /// <param name="distanceMap">3D distance map to use</param>
        /// <param name="localBounds">Bounding box of the 3D Distance map in a vehicle located in the point of origin with no rotation</param>
        /// <exception cref="InvalidOperationException">The Seamoth helper was not loaded before this method is called</exception>
        public static void BindProxy(LogWriter log, GameObject target, Texture3D distanceMap, Bounds localBounds)
        {
            log = log.Tag("WCP");
            var seamoth = SeamothHelper.Seamoth;
            if (seamoth == null)
            {
                log.Error("Seamoth not found. Cannot bind water clip proxy.");
                throw new InvalidOperationException("Seamoth not found. Cannot bind water clip.");
            }

            WaterClipProxy seamothWCP = seamoth.GetComponentInChildren<WaterClipProxy>();

            log.Write($"Binding WaterClipProxy to {target.name} with distance map {distanceMap.name} and bounds {localBounds}");

            WaterClipProxy waterClip = target.EnsureComponent<WaterClipProxy>();
            waterClip.shape = WaterClipProxy.Shape.DistanceField;
            waterClip.clipMaterial = seamothWCP.clipMaterial;
            waterClip.gameObject.layer = seamothWCP.gameObject.layer;

            var scale = 1.12f;
            log.Write($"Setting up WaterClipProxy with scale {scale}");

            localBounds.size *= scale;
            waterClip.distanceFieldMin = localBounds.min;
            waterClip.distanceFieldMax = localBounds.max;
            waterClip.distanceFieldSize = localBounds.size;
            waterClip.distanceFieldTexture = distanceMap;

            log.Write($"Calculating scaled border size");

            Vector3 borderSizeScaled = default;

            float originalScale = seamothWCP.transform.lossyScale.x;

            float foam = waterClip.waterSurface.SafeGet(x => x.foamDistance, 5f);
            //borderSizeScaled.x = foam / target.transform.lossyScale.x;
            //borderSizeScaled.y = foam / target.transform.lossyScale.y;
            //borderSizeScaled.z = foam / target.transform.lossyScale.z;

            log.Write($"Border size: {borderSizeScaled} from foam {foam}, Seamoth lossy scale was {originalScale}");

            Vector3 extents = waterClip.distanceFieldSize * 0.5f + borderSizeScaled;
            Vector3 vector = (waterClip.distanceFieldMin + waterClip.distanceFieldMax) * 0.5f;
            log.Write($"Creating box mesh for {vector} {extents}");
            waterClip.CreateBoxMesh(vector, extents);

            log.Write($"Setting up mesh renderer");
            MeshRenderer meshRenderer = target.EnsureComponent<MeshRenderer>();
            meshRenderer.material = waterClip.clipMaterial;
            waterClip.clipMaterial = meshRenderer.material;
            waterClip.UpdateMaterial();

            log.Write($"All set. Water clip proxy bound");

        }

        private static void DestroyComponent<T>(GameObject target, LogWriter log) where T : Component
        {
            T? component = target.GetComponent<T>();
            if (component != null)
            {
                log.Write($"Destroying component {typeof(T).Name} on {target.name}");
                UnityEngine.Object.Destroy(component);
            }
            else
            {
                log.Write($"No component of type {typeof(T).Name} found on {target.name}. Nothing to destroy.");
            }
        }


        /// <summary>
        /// Unbinds the WaterClipProxy from the specified target GameObject.
        /// </summary>
        /// <param name="log">Out logger</param>
        /// <param name="target">Target object that may contain the WaterClipProxy and its renderers</param>
        public static void UnbindProxy(LogWriter log, GameObject target)
        {
            log = log.Tag("WCP");
            if (target == null)
            {
                log.Error("Target is null. Cannot unbind water clip proxy.");
                return;
            }
            DestroyComponent<WaterClipProxy>(target, log);
            DestroyComponent<MeshRenderer>(target, log);
            DestroyComponent<MeshFilter>(target, log);

            log.Write($"Unbinding complete. Water clip proxy unbound from {target.name}.");
        }
    }
}

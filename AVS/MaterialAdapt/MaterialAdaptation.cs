
using AVS.Util;
using System;
using UnityEngine;

namespace AVS.MaterialAdapt
{
    /// <summary>
    /// A renderer material target description, identifying a material by its slot index,
    /// not reference.
    /// </summary>
    /// <author>https://github.com/IronFox</author>
    public readonly struct MaterialAddress : IEquatable<MaterialAddress>
    {
        /// <summary>
        /// The targeted renderer. Can become null if the source is destroyed
        /// </summary>
        public Renderer Renderer { get; }
        /// <summary>
        /// The recorded instance id of the renderer. Preserved for performance and also
        /// to prevent null reference exceptions if the renderer is destroyed
        /// </summary>
        public int RendererInstanceId { get; }
        /// <summary>
        /// The 0-based index of this material on the targeted renderer
        /// </summary>
        public int MaterialIndex { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (Renderer == null)
                return $"Dead renderer target ({RendererInstanceId}) material #{MaterialIndex + 1}";
            var m = GetMaterial();
            return $"{Renderer.NiceName()} #{MaterialIndex + 1}/{Renderer.materials.Length}: {m.NiceName()}";
        }

        /// <summary>
        /// Constructs a new material address descriptor
        /// </summary>
        /// <param name="renderer">Targeted renderer</param>
        /// <param name="materialIndex">Index of the material</param>
        public MaterialAddress(Renderer renderer, int materialIndex)
        {
            RendererInstanceId = renderer.GetInstanceID();
            Renderer = renderer;
            MaterialIndex = materialIndex;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is MaterialAddress target &&
                    Equals(target);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = 570612675;
            hashCode = hashCode * -1521134295 + RendererInstanceId.GetHashCode();
            hashCode = hashCode * -1521134295 + MaterialIndex.GetHashCode();
            return hashCode;
        }

        /// <inheritdoc/>
        public bool Equals(MaterialAddress target)
        {
            return RendererInstanceId == target.RendererInstanceId &&
                   MaterialIndex == target.MaterialIndex;
        }

        /// <summary>
        /// Gets the addressed material
        /// </summary>
        /// <returns>Addressed material or null if the address is/has become invalid</returns>
        public Material? GetMaterial()
        {
            if (Renderer == null)
                return null;
            if (MaterialIndex < 0 || MaterialIndex >= Renderer.materials.Length)
                return null;
            return Renderer.materials[MaterialIndex];
        }
    }

    /// <summary>
    /// A full material translation migrated+prototype -> final
    /// </summary>
    /// <author>https://github.com/IronFox</author>
    public class MaterialAdaptation
    {
        /// <summary>
        /// The targeted material
        /// </summary>
        public MaterialAddress Target => UnityMaterial.Source;
        /// <summary>
        /// The (shared) prototype used to modify the final material
        /// </summary>
        public SubnauticaMaterialPrototype Prototype { get; }
        /// <summary>
        /// The data migrated from the original material as present in the mesh
        /// </summary>
        public UnityMaterialData UnityMaterial { get; }
        /// <summary>
        /// The shader that is to be applied to the material
        /// </summary>
        public Shader Shader { get; }


        /// <summary>
        /// Constructs a new material adaptation descriptor
        /// </summary>
        /// <param name="prototype">The Subnautica material prototype descriptor</param>
        /// <param name="unityMaterial">The material data extracted from the Unity material</param>
        /// <param name="shader">The shader to use (Marmoset UBER)</param>
        public MaterialAdaptation(
            SubnauticaMaterialPrototype prototype,
            UnityMaterialData unityMaterial,
            Shader shader
            )
        {
            Prototype = prototype;
            UnityMaterial = unityMaterial;
            Shader = shader;
        }

        
        /// <summary>
        /// Resets only variables known to be corrupted during moonpool undock
        /// </summary>
        /// <param name="logConfig">Log Configuration</param>
        public void PostDockFixOnTarget(Logging logConfig)
        {
            try
            {
                var m = Target.GetMaterial();
                if (m == null)
                {
                    logConfig.Warn($"Target material is gone ({Target}). Cannot apply");
                    return;
                }
                if (m.shader != Shader)
                {
                    logConfig.LogExtraStep($"Applying {Shader.NiceName()} to {Target}");

                    m.shader = Shader;
                }

                Prototype.ApplyTo(m, logConfig, x =>
                       x == "_SpecInt"
                    || x == "_GlowStrength"
                    || x == "_GlowStrengthNight",
                    Target.ToString());

            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                logConfig.Error($"Failed to apply MaterialAdaptation to material {Target}");
            }
        }

        /// <summary>
        /// Reapplies all material properties to the target
        /// </summary>
        /// <param name="logConfig">Log Configuration</param>
        /// <param name="uniformShininess">The uniform shininess to apply everywhere. If not null,
        /// the unity material's smoothness value is disregarded</param>
        public void ApplyToTarget(Logging logConfig = default, float? uniformShininess = null)
        {
            try
            {
                var m = Target.GetMaterial();
                var mName = Target.ToString();
                if (m == null)
                {
                    logConfig.Warn($"Target material is gone ({mName}). Cannot apply");
                    return;
                }
                if (m.shader != Shader)
                {

                    m.shader = Shader;
                    logConfig.LogExtraStep($"Applied {m.shader.NiceName()} to {mName}");
                }

                Prototype.ApplyTo(m, logConfig, materialName: mName);

                UnityMaterial.ApplyTo(m, uniformShininess, logConfig, mName);
            }
            catch (Exception ex)
            {
                logConfig.Error($"Failed to apply MaterialAdaptation to {Target}");
                Debug.LogException(ex);
            }
        }
    }
}
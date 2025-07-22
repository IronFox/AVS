using AVS.Composition;
using AVS.Log;
using System.Linq;
using UnityEngine;

namespace AVS.MaterialAdapt
{
    /// <summary>
    /// The default material adaptation configuration.
    /// Can be instantiated or inherited by the client mod.
    /// </summary>
    public class DefaultMaterialAdaptConfig : IMaterialAdaptConfig
    {
        /// <summary>
        /// Default tag used to mark materials that should not be adapted.
        /// </summary>
        public static string KeepTag { get; } = "[keep]";
        /// <summary>
        /// Default tag used to mark materials that are consider glass and should not be adapted in the default process.
        /// </summary>
        public static string GlassTag { get; } = "[glass]";

        /// <inheritdoc/>
        public virtual bool IgnoreShaderNames => false;

        /// <inheritdoc/>
        public MaterialLog LogConfig { get; }

        /// <summary>
        /// Constructs a new default material adaptation configuration.
        /// </summary>
        /// <param name="logConfig">Optional logging configuration to use.
        /// If null, <see cref="MaterialLog.Default" /> is used</param>
        public DefaultMaterialAdaptConfig(MaterialLog? logConfig = null)
        {
            LogConfig = logConfig ?? MaterialLog.Default;
        }

        /// <inheritdoc/>
        public virtual bool IsExcludedFromMaterialFixing(GameObject go, VehicleComposition comp)
            => go.GetComponent<Skybox>()
            //|| go.name.ToLower().Contains("light")
            || comp.CanopyWindows.Contains(go);

        /// <inheritdoc/>
        /// <remarks>Calls <see cref="IsExcludedFromMaterialFixingByName(string)"/></remarks>
        public virtual bool IsExcludedFromMaterialFixing(Renderer renderer, int materialIndex, Material material)
            => IsExcludedFromMaterialFixingByName(material.name.ToLower());

        /// <summary>
        /// If this method returns true, the specific material with the given lower-case name will be excluded
        /// from material fixing.
        /// If you exclusion logic is based on material names only, you only need to override this method.
        /// </summary>
        /// <remarks>This default implementation excluded all materials 
        /// that have <see cref="KeepTag"/> or <see cref="GlassTag" /> in their name</remarks>
        /// <param name="lowerCaseMaterialName">Lower-case name of the material</param>
        /// <returns>True if this material should not be fixed</returns>
        public virtual bool IsExcludedFromMaterialFixingByName(string lowerCaseMaterialName)
            => lowerCaseMaterialName.Contains(KeepTag) || lowerCaseMaterialName.Contains(GlassTag);

        /// <inheritdoc/>
        public virtual UnityMaterialData ConvertUnityMaterial(UnityMaterialData materialData)
            => materialData;
    }
}

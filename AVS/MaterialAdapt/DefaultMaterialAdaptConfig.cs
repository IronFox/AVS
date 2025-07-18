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

        /// <summary>
        /// If this method returns true,
        /// all materials of the given game object will be excluded
        /// from material fixing.
        /// </summary>
        /// <remarks>Child objects will still be processed</remarks>
        /// <param name="go">Game object to test</param>
        /// <param name="comp">Vehicle composition of the target vehicle</param>
        /// <returns>True if this object should not be fixed</returns>
        public virtual bool IsExcludedFromMaterialFixing(GameObject go, VehicleComposition comp)
            => go.GetComponent<Skybox>()
            //|| go.name.ToLower().Contains("light")
            || comp.CanopyWindows.Contains(go);

        /// <summary>
        /// If this method returns true,
        /// the specific material of the given renderer will be excluded
        /// from material fixing.
        /// Override if your exclusion logic is based on material states beyond just their names.
        /// Otherwise, override <see cref="IsExcludedFromMaterialFixingByName(string)"/> instead.
        /// </summary>
        /// <remarks>Calls <see cref="IsExcludedFromMaterialFixingByName(string)"/></remarks>
        /// <param name="renderer">Owning renderer</param>
        /// <param name="materialIndex">Index of the material being processed with 0 being the first material</param>
        /// <param name="material">Material being processed</param>
        /// <returns>True if this material should not be fixed</returns>
        public virtual bool IsExcludedFromMaterialFixing(Renderer renderer, int materialIndex, Material material)
            => IsExcludedFromMaterialFixingByName(material.name.ToLower());

        /// <summary>
        /// If this method returns true, the specific material with the given lower-case name will be excluded
        /// from material fixing.
        /// If you exclusion logic is based on material names only, you only need to override this method.
        /// </summary>
        /// <param name="lowerCaseMaterialName">Lower-case name of <paramref name="material"/></param>
        /// <returns>True if this material should not be fixed</returns>
        public virtual bool IsExcludedFromMaterialFixingByName(string lowerCaseMaterialName)
            => lowerCaseMaterialName.Contains(KeepTag) || lowerCaseMaterialName.Contains(GlassTag);
    }
}

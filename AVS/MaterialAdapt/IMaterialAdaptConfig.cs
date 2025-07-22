using AVS.Composition;
using AVS.Log;
using UnityEngine;

namespace AVS.MaterialAdapt
{
    /// <summary>
    /// Controls how materials are adapted to Subnautica's material system.
    /// </summary>
    public interface IMaterialAdaptConfig
    {
        /// <summary>
        /// Logging configuration for material adaptation.
        /// </summary>
        public MaterialLog LogConfig { get; }


        /// <summary>
        /// If true, the vehicle's shader name will not be checked when filtering materials.
        /// </summary>
        public bool IgnoreShaderNames { get; }

        /// <summary>
        /// If this method returns true,
        /// all materials of the given game object will be excluded
        /// from material fixing.
        /// </summary>
        /// <remarks>Child objects will still be processed</remarks>
        /// <param name="go">Game object to test</param>
        /// <param name="comp">Vehicle composition of the target vehicle</param>
        /// <returns>True if this object should not be fixed</returns>
        public bool IsExcludedFromMaterialFixing(GameObject go, VehicleComposition comp);

        /// <summary>
        /// If this method returns true,
        /// the specific material of the given renderer will be excluded
        /// from material fixing.
        /// </summary>
        /// <param name="renderer">Owning renderer</param>
        /// <param name="materialIndex">Index of the material being processed with 0 being the first material</param>
        /// <param name="material">Material being processed</param>
        /// <returns>True if this material should not be fixed</returns>
        public bool IsExcludedFromMaterialFixing(Renderer renderer, int materialIndex, Material material);

        /// <summary>
        /// Passes the loaded material data to the config for custom conversion.
        /// </summary>
        /// <param name="materialData">Loaded material data</param>
        /// <returns>Converted material</returns>
        public UnityMaterialData ConvertUnityMaterial(UnityMaterialData materialData);
    }
}

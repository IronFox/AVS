using AVS.Configuration;
using AVS.MaterialAdapt;
using AVS.Util;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.BaseVehicle
{
    public abstract partial class AvsVehicle
    {

        /// <summary>
        /// The material fixer instance used for this vehicle.
        /// Ineffective if <see cref="VehicleConfiguration.AutoFixMaterials"/> is false.
        /// </summary>
        public MaterialFixer MaterialFixer { get; }

        private IEnumerable<UnityMaterialData> ResolveMaterial(IMaterialAdaptConfig config)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (config.IsExcludedFromMaterialFixing(renderer.gameObject, Com))
                {
                    config.LogConfig.LogExtraStep($"Skipping renderer {renderer.NiceName()} because it is excluded from material fixing");
                    continue;
                }

                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (config.IsExcludedFromMaterialFixing(renderer, i, renderer.materials[i]))
                    {
                        config.LogConfig.LogExtraStep($"Skipping material {i} of {renderer.NiceName()} ({renderer.materials[i].NiceName()}) because it is excluded from material fixing");
                        continue;
                    }

                    var material = UnityMaterialData.From(renderer, i, config.LogConfig, config.IgnoreShaderNames);
                    if (material != null)
                        yield return material;
                }
            }
        }



    }
}

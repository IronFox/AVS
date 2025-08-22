using AVS.BaseVehicle;
using AVS.Util;
using System.Collections.Generic;
using UnityEngine;


namespace AVS.MaterialAdapt;

internal class AvsMaterialResolver : IMaterialResolver
{
    public AvsMaterialResolver(IMaterialAdaptConfig config, AvsVehicle target)
    {
        Config = config;
        Target = target;
    }

    public IMaterialAdaptConfig Config { get; }
    public AvsVehicle Target { get; }

    public IEnumerable<UnityMaterialData> ResolveMaterials()
    {
        var renderers = Target.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (Config.IsExcludedFromMaterialFixing(renderer.gameObject, Target.Com))
            {
                Config.LogConfig.LogExtraStep(
                    $"Skipping renderer {renderer.NiceName()} because it is excluded from material fixing");
                continue;
            }

            for (var i = 0; i < renderer.materials.Length; i++)
            {
                var m = renderer.materials[i];
                var classification = Config.ClassifyMaterial(renderer, i, m);
                if (!classification.Include)
                {
                    Config.LogConfig.LogExtraStep(
                        $"Skipping material {i} of {renderer.NiceName()} ({m.NiceName()}) because it is excluded from material fixing");
                    continue;
                }

                var material = UnityMaterialData.From(
                    renderer,
                    i,
                    classification.Type,
                    Config.LogConfig,
                    classification.IgnoreShaderNames);
                if (material.IsNotNull())
                {
                    material = Config.ConvertUnityMaterial(material);
                    yield return material;
                }
            }
        }
    }
}
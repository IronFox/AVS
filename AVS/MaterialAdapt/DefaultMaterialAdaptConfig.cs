using AVS.Composition;
using AVS.Log;
using System.Linq;
using UnityEngine;

namespace AVS.MaterialAdapt;

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

    /// <summary>
    /// True if shader names should be ignored during material adaptation.
    /// </summary>
    public virtual bool IgnoreShaderNames => false;

    /// <summary>
    /// True if glass shader names should be ignored during material adaptation.
    /// </summary>
    public virtual bool IgnoreGlassShaderNames => true;

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
    {
        return go.GetComponent<Skybox>()
               //|| go.name.ToLower().Contains("light")
               || comp.CanopyWindows.Contains(go);
    }


    /// <summary>
    /// If this method returns true, the specific material with the given lower-case name will be excluded
    /// from material fixing.
    /// If you exclusion logic is based on material names only, you only need to override this method.
    /// </summary>
    /// <remarks>This default implementation excludes all materials 
    /// that have <see cref="KeepTag"/> in their name</remarks>
    /// <param name="lowerCaseMaterialName">Lower-case name of the material</param>
    /// <returns>True if this material should not be fixed</returns>
    public virtual bool IsExcludedFromMaterialFixingByName(string lowerCaseMaterialName)
    {
        return lowerCaseMaterialName.Contains(KeepTag);
    }

    /// <summary>
    /// Determines whether the provided renderer is excluded from material fixing.
    /// </summary>
    /// <param name="renderer">The renderer to be evaluated.</param>
    /// <returns>True if the renderer is excluded from material fixing; otherwise, false.</returns>
    public virtual bool IsExcludedFromMaterialFixing(Renderer renderer)
    {
        return renderer is SpriteRenderer;
    }

    /// <inheritdoc/>
    public virtual UnityMaterialData ConvertUnityMaterial(UnityMaterialData materialData)
    {
        return materialData;
    }

    /// <inheritdoc/>
    public MaterialClassification ClassifyMaterial(Renderer renderer, int materialIndex, Material material)
    {
        var name = material.name.ToLower();
        if (IsExcludedFromMaterialFixingByName(name))
            return MaterialClassification.Excluded;
        if (IsExcludedFromMaterialFixing(renderer))
            return MaterialClassification.Excluded;
        // uGUI_Icon
        if (name.Contains(GlassTag))
            return new MaterialClassification(MaterialType.Glass, IgnoreGlassShaderNames);
        return new MaterialClassification(MaterialType.Opaque, IgnoreShaderNames);
    }
}
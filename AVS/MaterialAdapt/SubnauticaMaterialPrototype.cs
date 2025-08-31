using AVS.Assets;
using AVS.Interfaces;
using AVS.Log;
using AVS.MaterialAdapt.Variables;
using AVS.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace AVS.MaterialAdapt;


/// <summary>
/// Read-only material definition as retrieved from some existing material
/// </summary>
/// <author>https://github.com/IronFox</author>
public class SubnauticaMaterialPrototype : INullTestableType
{
    /// <summary>
    /// True if this instance was created without a source material.
    /// All local values are empty/default if true
    /// </summary>
    public bool IsEmpty { get; private set; }

    private HashSet<string> ShaderKeywords { get; } = new();

    /// <summary>
    /// Global illumination flags retrieved from the source material
    /// </summary>
    public MaterialGlobalIlluminationFlags MaterialGlobalIlluminationFlags { get; }

    private ColorVariable[] ColorVariables { get; }
    private VectorVariable[] VectorVariables { get; }
    private TextureVariable[] TextureVariables { get; }
    private FloatVariable[] FloatVariables { get; }

    /// <summary>
    /// Updates all recorded shader variables in the specified material
    /// </summary>
    /// <param name="m">Target material</param>
    /// <param name="logConfig">Log Configuration</param>
    /// <param name="materialName">Optional custom material name to use instead of the nice name of the material itself</param>
    /// <param name="rmc">Root mod controller for logging purposes</param>
    /// <param name="variableNamePredicate">
    /// Optional predicate to only check/update certain shader variables by name.
    /// If non-null updates only variables for which this function returns true</param>
    public void ApplyTo(RootModController rmc, Material m, MaterialLog logConfig, Func<string, bool>? variableNamePredicate = null,
        string? materialName = null)
    {
        using var log = SmartLog.LazyForAVS(rmc);
        variableNamePredicate = variableNamePredicate ?? (_ => true);

        foreach (var v in ColorVariables)
            if (variableNamePredicate(v.Name))
                v.SetTo(rmc, m, logConfig, materialName);
        foreach (var v in VectorVariables)
            if (variableNamePredicate(v.Name))
                v.SetTo(rmc, m, logConfig, materialName);
        foreach (var v in FloatVariables)
            if (variableNamePredicate(v.Name))
                v.SetTo(rmc, m, logConfig, materialName);
        foreach (var v in TextureVariables)
            if (variableNamePredicate(v.Name))
                v.SetTo(rmc, m, logConfig, materialName);

        if (m.globalIlluminationFlags != MaterialGlobalIlluminationFlags)
        {
            logConfig.LogMaterialChange(log,
                $"Applying global illumination flags ({m.globalIlluminationFlags} -> {MaterialGlobalIlluminationFlags})");

            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags;
        }

        foreach (var existing in m.shaderKeywords.ToList())
            if (!ShaderKeywords.Contains(existing))
            {
                logConfig.LogMaterialChange(log, $"Removing shader keyword {existing}");
                m.DisableKeyword(existing);
            }

        foreach (var kw in ShaderKeywords)
            if (!m.IsKeywordEnabled(kw))
            {
                logConfig.LogMaterialChange(log, $"Enabling shader keyword {kw}");
                m.EnableKeyword(kw);
            }
    }

    /// <summary>
    /// Constructs the prototype from a given material
    /// </summary>
    /// <param name="source">Material to read. Can be null, causing <see cref="IsEmpty"/> to be set true</param>
    /// <param name="loadTextures">If true also load texture property values</param>
    public SubnauticaMaterialPrototype(Material? source, bool loadTextures = false)
    {
        if (source.IsNull())
        {
            IsEmpty = true;
            ColorVariables = Array.Empty<ColorVariable>();
            FloatVariables = Array.Empty<FloatVariable>();
            VectorVariables = Array.Empty<VectorVariable>();
            TextureVariables = Array.Empty<TextureVariable>();

            return;
        }

        MaterialGlobalIlluminationFlags = source.globalIlluminationFlags;
        ShaderKeywords.AddRange(source.shaderKeywords);

        var colorVariables = new List<ColorVariable>();
        var floatVariables = new List<FloatVariable>();
        var vectorVariables = new List<VectorVariable>();
        var textureVariables = new List<TextureVariable>();

        for (var v = 0; v < source.shader.GetPropertyCount(); v++)
        {
            var n = source.shader.GetPropertyName(v);
            switch (source.shader.GetPropertyType(v))
            {
                case ShaderPropertyType.Color:
                    if (!n.StartsWith("_Color") //don't copy colors (_Color, _Color2, _Color3)
                        &&
                        !n.StartsWith("_SpecColor") //not sure if these have an impact but can be left out
                       )
                        colorVariables.Add(new ColorVariable(source, n));
                    break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    floatVariables.Add(new FloatVariable(source, n));
                    break;
                case ShaderPropertyType.Vector:
                    vectorVariables.Add(new VectorVariable(source, n));
                    break;

                case ShaderPropertyType.Texture:
                    if (loadTextures)
                        //if (n != "_MainTex" && n != "_BumpMap" && n != "_SpecTex" && n != "_Illum")
                        textureVariables.Add(new TextureVariable(source, n));
                    break;
            }
        }

        ColorVariables = colorVariables.ToArray();
        FloatVariables = floatVariables.ToArray();
        VectorVariables = vectorVariables.ToArray();
        TextureVariables = textureVariables.ToArray();
    }


    /// <summary>
    /// Creates a material prototype for the glass material of the Seamoth.
    /// </summary>
    /// <param name="logConfig">Logging configuration</param>
    /// <param name="result">The retrieved aquarium glass material, if any.</param>
    /// <param name="rmc">Root mod controller for logging purposes</param>
    /// <returns>False if the seamoth is not (yet) available. Keep trying if false.
    /// Ttrue if the seamoth is loaded, but <paramref name="result"/> can still be null
    /// if the respective material is not found</returns>
    public static bool GlassMaterialFromSeamoth(RootModController rmc, out Material? result, MaterialLog logConfig = default)
    {
        var sm = SeamothHelper.Seamoth;
        if (sm.IsNull())
        {
            result = null;
            return false;
        }
        using var log = SmartLog.LazyForAVS(rmc);

        logConfig.LogExtraStep(log, $"Found Seamoth");
        var glassMaterial = sm.transform
            .Find("Model/Submersible_SeaMoth/Submersible_seaMoth_geo/Submersible_SeaMoth_glass_interior_geo")
            .GetComponent<SkinnedMeshRenderer>().material;
        result = glassMaterial;
        return true;
    }

    /// <summary>
    /// Creates a material prototype for the glass material of the Seamoth.
    /// </summary>
    /// <param name="logConfig">Logging configuration</param>
    /// <param name="rmc">Root mod controller for logging purposes</param>
    /// <returns>Null if the seamoth is not (yet) available. Keep trying if null.
    /// Non-null if the seamoth is loaded, but can then be empty (IsEmpty is true)
    /// if the respective material is not found</returns>
    public static SubnauticaMaterialPrototype? GlassFromSeamoth(RootModController rmc, MaterialLog logConfig = default)
    {
        if (!GlassMaterialFromSeamoth(rmc, out var glassMaterial, logConfig))
            return null;
        return new SubnauticaMaterialPrototype(glassMaterial, true);
    }

    /// <summary>
    /// Retrieves the entire glass material of the aquarium.
    /// </summary>
    /// <param name="rmc">Root mod controller for logging purposes</param>
    /// <param name="logConfig">Logging configuration</param>
    /// <param name="result">The material prototype to fill with the aquarium glass material.</param>
    /// <returns>False if the aquarium is not (yet) available. Keep trying if false.
    /// Ttrue if the aquarium is loaded, but <paramref name="result"/> can still be null
    /// if the respective material is not found</returns>
    public static bool GlassMaterialFromAquarium(RootModController rmc, out Material? result, MaterialLog logConfig = default)
    {
        var sm = PrefabLoader.Request(TechType.Aquarium, true);
        if (sm.Prefab.IsNull())
        {
            result = null;
            return false;
        }

        using var log = SmartLog.LazyForAVS(rmc);

        logConfig.LogExtraStep(log, $"Found Aquarium");

        Material? glassMaterial = null;
        var renderers = sm.Prefab.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.materials)
                if (material.shader.name == Shaders.MainShader
                    && material.name.StartsWith("Aquarium_glass"))
                {
                    logConfig.LogExtraStep(log, $"Found material prototype: {material.NiceName()}");
                    glassMaterial = material;
                    break;
                }
                else
                {
                    logConfig.LogExtraStep(log,
                        $"(Expected) shader mismatch on {material.NiceName()} which uses {material.shader}");
                }

            if (glassMaterial.IsNotNull())
                break;
        }

        result = glassMaterial;
        return true;
    }

    /// <summary>
    /// Creates a material prototype for the glass material of the Seamoth.
    /// </summary>
    /// <param name="rmc">Root mod controller for logging purposes</param>
    /// <param name="logConfig">Logging configuration</param>
    /// <returns>Null if the seamoth is not (yet) available. Keep trying if null.
    /// Non-null if the seamoth is loaded, but can then be empty (IsEmpty is true)
    /// if the respective material is not found</returns>
    public static SubnauticaMaterialPrototype? GlassFromAquarium(RootModController rmc, MaterialLog logConfig = default)
    {
        if (!GlassMaterialFromAquarium(rmc, out var glassMaterial, logConfig))
            return null;
        return new SubnauticaMaterialPrototype(glassMaterial);
    }

    /// <summary>
    /// Creates a material prototype for the main material of the Seamoth body.
    /// While the Seamoth is not yet available, the method returns null.
    /// If the Seamoth is loaded but the material could not be found, the return
    /// value is an empty material prototype (IsEmpty=true)
    /// </summary>
    /// <param name="rmc">Root mod controller for logging purposes</param>
    /// <param name="logConfig">Logging configuration</param>
    /// <returns>Null if the seamoth is not (yet) available. Keep trying if null.
    /// Non-null if the seamoth is loaded, but can then be empty (IsEmpty is true)
    /// if the respective material is not found</returns>
    public static SubnauticaMaterialPrototype? FromSeamoth(RootModController rmc, MaterialLog logConfig = default)
    {
        var sm = SeamothHelper.Seamoth;
        if (sm.IsNull())
            return null;
        using var log = SmartLog.LazyForAVS(rmc);
        logConfig.LogExtraStep(log, $"Found Seamoth");

        Material? seamothMaterial = null;
        var renderers = sm.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.materials)
                if (material.shader.name == Shaders.MainShader
                    && material.name.StartsWith("Submersible_SeaMoth"))
                {
                    logConfig.LogExtraStep(log, $"Found material prototype: {material.NiceName()}");
                    seamothMaterial = material;
                    break;
                }
                else
                {
                    logConfig.LogExtraStep(log,
                        $"(Expected) shader mismatch on {material.NiceName()} which uses {material.shader}");
                }

            if (seamothMaterial.IsNotNull())
                break;
        }

        return new SubnauticaMaterialPrototype(seamothMaterial);
    }
}
using AVS.Assets;
using AVS.Log;
using AVS.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AVS.Interfaces;
using UnityEngine;
using UnityEngine.Rendering;

namespace AVS.MaterialAdapt;

internal interface IShaderVariable
{
    ShaderPropertyType Type { get; }

    /// <summary>
    /// Updates a material according to the preserved values present in the local variable
    /// </summary>
    /// <param name="m">Material to update</param>
    /// <param name="logConfig">Log Configuration</param>
    /// <param name="materialName">Optional custom material name to use instead of the nice name of the material itself</param>
    void SetTo(Material m, MaterialLog logConfig, string? materialName);
}

internal readonly struct ColorVariable : IShaderVariable
{
    public ShaderPropertyType Type => ShaderPropertyType.Color;
    public Color Value { get; }
    public string Name { get; }

    public ColorVariable(Material m, string n)
    {
        Value = m.GetColor(n);
        Name = n;
    }

    /// <summary>
    /// Sets the color property of a material with the given value and logs the change.
    /// </summary>
    /// <param name="m">The material on which the color property will be set.</param>
    /// <param name="name">The name of the color property to set.</param>
    /// <param name="value">The new color value to assign to the property.</param>
    /// <param name="logConfig">The log configuration used to log the operation.</param>
    /// <param name="materialName">Optional custom material name for logging purposes.</param>
    public static void Set(Material m, string name, Color value, MaterialLog logConfig, string? materialName)
    {
        try
        {
            var old = m.GetColor(name);
            if (old == value)
                return;
            logConfig.LogMaterialVariableSet(ShaderPropertyType.Color, name, old, value, m, materialName);
            m.SetColor(name, value);
        }
        catch (Exception ex)
        {
            logConfig.Writer.Error($"Failed to set color {name} ({value}) on {materialName ?? m.NiceName()}", ex);
        }
    }

    public void SetTo(Material m, MaterialLog logConfig, string? materialName)
    {
        Set(m, Name, Value, logConfig, materialName);
    }
}

internal readonly struct VectorVariable : IShaderVariable
{
    public ShaderPropertyType Type => ShaderPropertyType.Vector;


    public Vector4 Value { get; }
    public string Name { get; }

    public VectorVariable(Material m, string n)
    {
        Value = m.GetVector(n);
        Name = n;
    }

    public void SetTo(Material m, MaterialLog logConfig, string? materialName)
    {
        try
        {
            var old = m.GetVector(Name);
            if (old == Value)
                return;
            logConfig.LogMaterialVariableSet(Type, Name, old, Value, m, materialName);
            m.SetVector(Name, Value);
        }
        catch (Exception ex)
        {
            logConfig.Writer.Error($"Failed to set {Type} {Name} ({Value}) on {materialName ?? m.NiceName()}", ex);
        }
    }
}

internal readonly struct TextureVariable : IShaderVariable
{
    public ShaderPropertyType Type => ShaderPropertyType.Vector;


    public Texture Texture { get; }
    public Vector2 Offset { get; }
    public Vector2 Scale { get; }
    public string Name { get; }

    public TextureVariable(Material m, string n)
    {
        Texture = m.GetTexture(n);
        Offset = m.GetTextureOffset(n);
        Scale = m.GetTextureScale(n);
        Name = n;
    }

    public void SetTo(Material m, MaterialLog logConfig, string? materialName)
    {
        try
        {
            var oldT = m.GetTexture(Name);
            var oldO = m.GetTextureOffset(Name);
            var oldS = m.GetTextureScale(Name);
            if (oldT != Texture)
            {
                logConfig.LogMaterialVariableSet(Type, Name, oldT, Texture, m, materialName);
                m.SetTexture(Name, Texture);
            }

            if (oldO != Offset)
            {
                logConfig.LogMaterialVariableSet(Type, Name, oldO, Offset, m, materialName);
                m.SetTextureOffset(Name, Offset);
            }

            if (oldS != Scale)
            {
                logConfig.LogMaterialVariableSet(Type, Name, oldS, Scale, m, materialName);
                m.SetTextureScale(Name, Scale);
            }
        }
        catch (Exception ex)
        {
            logConfig.Writer.Error(
                $"Failed to set {Type} {Name} ({Texture.NiceName()}, {Offset}, {Scale}) on {materialName ?? m.NiceName()}",
                ex);
        }
    }
}

internal readonly struct FloatVariable : IShaderVariable
{
    public ShaderPropertyType Type => ShaderPropertyType.Float;


    public float Value { get; }
    public string Name { get; }

    public FloatVariable(Material m, string n)
    {
        Value = m.GetFloat(n);
        Name = n;
    }

    public void SetTo(Material m, MaterialLog logConfig, string? materialName)
    {
        try
        {
            var old = m.GetFloat(Name);
            if (old == Value)
                return;
            logConfig.LogMaterialVariableSet(Type, Name, old, Value, m, materialName);
            m.SetFloat(Name, Value);
        }
        catch (Exception ex)
        {
            logConfig.Writer.Error(
                $"Failed to set {Type} {Name} ({Value.ToString(CultureInfo.InvariantCulture)}) on {materialName ?? m.NiceName()}",
                ex);
        }
    }
}

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
    /// <param name="variableNamePredicate">
    /// Optional predicate to only check/update certain shader variables by name.
    /// If non-null updates only variables for which this function returns true</param>
    public void ApplyTo(Material m, MaterialLog logConfig, Func<string, bool>? variableNamePredicate = null,
        string? materialName = null)
    {
        variableNamePredicate = variableNamePredicate ?? (_ => true);

        foreach (var v in ColorVariables)
            if (variableNamePredicate(v.Name))
                v.SetTo(m, logConfig, materialName);
        foreach (var v in VectorVariables)
            if (variableNamePredicate(v.Name))
                v.SetTo(m, logConfig, materialName);
        foreach (var v in FloatVariables)
            if (variableNamePredicate(v.Name))
                v.SetTo(m, logConfig, materialName);
        foreach (var v in TextureVariables)
            if (variableNamePredicate(v.Name))
                v.SetTo(m, logConfig, materialName);

        if (m.globalIlluminationFlags != MaterialGlobalIlluminationFlags)
        {
            logConfig.LogMaterialChange(
                $"Applying global illumination flags ({m.globalIlluminationFlags} -> {MaterialGlobalIlluminationFlags})");

            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags;
        }

        foreach (var existing in m.shaderKeywords.ToList())
            if (!ShaderKeywords.Contains(existing))
            {
                logConfig.LogMaterialChange($"Removing shader keyword {existing}");
                m.DisableKeyword(existing);
            }

        foreach (var kw in ShaderKeywords)
            if (!m.IsKeywordEnabled(kw))
            {
                logConfig.LogMaterialChange($"Enabling shader keyword {kw}");
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
    /// <returns>False if the seamoth is not (yet) available. Keep trying if false.
    /// Ttrue if the seamoth is loaded, but <paramref name="result"/> can still be null
    /// if the respective material is not found</returns>
    public static bool GlassMaterialFromSeamoth(out Material? result, MaterialLog logConfig = default)
    {
        var sm = SeamothHelper.Seamoth;
        if (sm.IsNull())
        {
            result = null;
            return false;
        }

        logConfig.LogExtraStep($"Found Seamoth");
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
    /// <returns>Null if the seamoth is not (yet) available. Keep trying if null.
    /// Non-null if the seamoth is loaded, but can then be empty (IsEmpty is true)
    /// if the respective material is not found</returns>
    public static SubnauticaMaterialPrototype? GlassFromSeamoth(MaterialLog logConfig = default)
    {
        if (!GlassMaterialFromSeamoth(out var glassMaterial, logConfig))
            return null;
        return new SubnauticaMaterialPrototype(glassMaterial, true);
    }

    /// <summary>
    /// Retrieves the entire glass material of the aquarium.
    /// </summary>
    /// <param name="logConfig">Logging configuration</param>
    /// <param name="result">The material prototype to fill with the aquarium glass material.</param>
    /// <returns>False if the aquarium is not (yet) available. Keep trying if false.
    /// Ttrue if the aquarium is loaded, but <paramref name="result"/> can still be null
    /// if the respective material is not found</returns>
    public static bool GlassMaterialFromAquarium(out Material? result, MaterialLog logConfig = default)
    {
        var sm = PrefabLoader.Request(TechType.Aquarium, logConfig.Writer, true);
        if (sm.Prefab.IsNull())
        {
            result = null;
            return false;
        }

        logConfig.LogExtraStep($"Found Aquarium");

        Material? glassMaterial = null;
        var renderers = sm.Prefab.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.materials)
                if (material.shader.name == Shaders.MainShader
                    && material.name.StartsWith("Aquarium_glass"))
                {
                    logConfig.LogExtraStep($"Found material prototype: {material.NiceName()}");
                    glassMaterial = material;
                    break;
                }
                else
                {
                    logConfig.LogExtraStep(
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
    /// <param name="logConfig">Logging configuration</param>
    /// <returns>Null if the seamoth is not (yet) available. Keep trying if null.
    /// Non-null if the seamoth is loaded, but can then be empty (IsEmpty is true)
    /// if the respective material is not found</returns>
    public static SubnauticaMaterialPrototype? GlassFromAquarium(MaterialLog logConfig = default)
    {
        if (!GlassMaterialFromAquarium(out var glassMaterial, logConfig))
            return null;
        return new SubnauticaMaterialPrototype(glassMaterial);
    }

    /// <summary>
    /// Creates a material prototype for the main material of the Seamoth body.
    /// While the Seamoth is not yet available, the method returns null.
    /// If the Seamoth is loaded but the material could not be found, the return
    /// value is an empty material prototype (IsEmpty=true)
    /// </summary>
    /// <param name="logConfig">Logging configuration</param>
    /// <returns>Null if the seamoth is not (yet) available. Keep trying if null.
    /// Non-null if the seamoth is loaded, but can then be empty (IsEmpty is true)
    /// if the respective material is not found</returns>
    public static SubnauticaMaterialPrototype? FromSeamoth(MaterialLog logConfig = default)
    {
        var sm = SeamothHelper.Seamoth;
        if (sm.IsNull())
            return null;

        logConfig.LogExtraStep($"Found Seamoth");

        Material? seamothMaterial = null;
        var renderers = sm.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.materials)
                if (material.shader.name == Shaders.MainShader
                    && material.name.StartsWith("Submersible_SeaMoth"))
                {
                    logConfig.LogExtraStep($"Found material prototype: {material.NiceName()}");
                    seamothMaterial = material;
                    break;
                }
                else
                {
                    logConfig.LogExtraStep(
                        $"(Expected) shader mismatch on {material.NiceName()} which uses {material.shader}");
                }

            if (seamothMaterial.IsNotNull())
                break;
        }

        return new SubnauticaMaterialPrototype(seamothMaterial);
    }
}
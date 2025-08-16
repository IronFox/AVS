using AVS.Log;
using AVS.Util;
using System;
using UnityEngine;

namespace AVS.MaterialAdapt
{
    /// <summary>
    /// Material classification.
    /// </summary>
    public enum MaterialType
    {
        /// <summary>
        /// Standard opaque material
        /// </summary>
        Opaque,
        /// <summary>
        /// Glass material
        /// </summary>
        Glass
    }


    /// <summary>
    /// Surface shader data extracted from a material imported from Unity.
    /// Only values relevant to the translation process are read.
    /// Read-only
    /// </summary>
    /// <author>https://github.com/IronFox</author>
    public class UnityMaterialData
    {
        private const string SpecTexName = "_SpecTex";
        private const string IllumTexName = "_Illum";
        private static readonly int SpecTex = Shader.PropertyToID(SpecTexName);
        private static readonly int Illum = Shader.PropertyToID(IllumTexName);

        /// <summary>
        /// The name of the source material
        /// </summary>
        public string MaterialName { get; }

        /// <summary>
        /// The material classification
        /// </summary>
        public MaterialType Type { get; }

        /// <summary>
        /// Main color of the material. Black if none
        /// </summary>
        public Color Color { get; }

        /// <summary>
        /// The specular color of the material. Tints specular reflection.
        /// Default is white.
        /// </summary>
        public Color SpecularColor { get; }

        /// <summary>
        /// Emission texture of this material. Black if not emissive
        /// </summary>
        public Color EmissionColor { get; }
        /// <summary>
        /// Main texture of the material. Null if none.
        /// In order to be applicable as
        /// specular reflectivity map, its alpha value must be filled such.
        /// </summary>
        public Texture? MainTex { get; }

        /// <summary>
        /// Smoothness value (typically 0-1)
        /// </summary>
        public float Smoothness { get; }
        /// <summary>
        /// Metallic texture. In order to be applicable as
        /// specular reflectivity map, its alpha value must be filled such.
        /// </summary>
        public Texture? MetallicTexture { get; }
        /// <summary>
        /// Normal map. Null if none
        /// </summary>
        public Texture? BumpMap { get; }
        /// <summary>
        /// Emission texture. Null if none
        /// </summary>
        public Texture? EmissionTexture { get; }
        /// <summary>
        /// Texture channel to derive the smoothness (specular) appearance from
        /// 0 = Metallic
        /// 1 = MainTex
        /// </summary>
        public int SmoothnessTextureChannel { get; }

        /// <summary>
        /// The specular reflectivity texture to use for this material.
        /// Only the alpha channel is used.
        /// </summary>
        public Texture? SpecularTexture
        {
            get
            {
                switch (SmoothnessTextureChannel)
                {
                    case 0:
                        return MetallicTexture;
                    case 1:
                        return MainTex;
                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// The source material
        /// </summary>
        public MaterialAddress Source { get; }

        /// <summary>
        /// Constructs a new instance of <see cref="UnityMaterialData"/>
        /// </summary>
        private UnityMaterialData(
            MaterialType type,
            string materialName,
            Color color,
            Color specularColor,
            Color emissionColor,
            Texture? mainTex,
            float smoothness,
            int smoothnessTextureChannel,
            Texture? metallicTexture,
            Texture? bumpMap,
            Texture? emissionTexture,
            MaterialAddress source)
        {
            Type = type;
            MaterialName = materialName;
            Source = source;
            Color = color;
            SpecularColor = specularColor;
            EmissionColor = emissionColor;
            MainTex = mainTex;
            Smoothness = smoothness;
            MetallicTexture = metallicTexture;
            SmoothnessTextureChannel = smoothnessTextureChannel;
            BumpMap = bumpMap;
            EmissionTexture = emissionTexture;
        }

        private static Color GetColor(Material m, string name, MaterialLog logConfig)
        {
            if (!m.HasProperty(name))
            {
                logConfig.Warn($"Material {m} does not have expected property {name}");
                return Color.black;
            }
            try
            {
                return m.GetColor(name);
            }
            catch (Exception e)
            {
                logConfig.Error($"Material {m} does not have expected color property {name}");
                Debug.LogException(e);
                return Color.black;
            }
        }

        private static Texture? GetTexture(Material m, string name, MaterialLog logConfig)
        {
            if (!m.HasProperty(name))
            {
                logConfig.Warn($"Material {m} does not have expected property {name}");
                return null;
            }
            try
            {
                return m.GetTexture(name);
            }
            catch (Exception e)
            {
                logConfig.Error($"Material {m} does not have expected texture property {name}");
                Debug.LogException(e);
                return null;
            }
        }

        private static float GetFloat(Material m, string name, MaterialLog logConfig)
        {
            if (!m.HasProperty(name))
            {
                logConfig.Warn($"Material {m} does not have expected property {name}");
                return 0;
            }
            try
            {
                return m.GetFloat(name);
            }
            catch (Exception e)
            {
                logConfig.Error($"Material {m} does not have expected float property {name}");
                Debug.LogException(e);
                return 0;
            }
        }

        private static int GetInt(Material m, string name, MaterialLog logConfig)
        {
            if (!m.HasProperty(name))
            {
                logConfig.Warn($"Material {m} does not have expected property {name}");
                return 0;
            }
            try
            {
                return m.GetInt(name);
            }
            catch (Exception e)
            {
                logConfig.Error($"Material {m} does not have expected int property {name}");
                Debug.LogException(e);
                return 0;
            }
        }



        private static UnityMaterialData? From(MaterialAddress target, Material m, MaterialLog logConfig, MaterialType type, bool ignoreShaderName = false)
        {

            if (m.shader.name != "Standard" && !ignoreShaderName)
            {
                logConfig.LogExtraStep($"Ignoring {m.NiceName()} which uses {m.shader.NiceName()}");
                return null;
            }
            var mName = target.ToString();
            logConfig.LogExtraStep($"Reading {mName} which uses {m.shader.NiceName()}");
            var data = new UnityMaterialData(
                type: type,
                materialName: m.name,
                color: GetColor(m, "_Color", logConfig),
                specularColor: Color.white,
                emissionColor: GetColor(m, "_EmissionColor", logConfig),
                mainTex: GetTexture(m, "_MainTex", logConfig),
                smoothness: GetFloat(m, "_Glossiness", logConfig),
                metallicTexture: GetTexture(m, "_MetallicGlossMap", logConfig),
                bumpMap: GetTexture(m, "_BumpMap", logConfig),
                emissionTexture: GetTexture(m, "_EmissionMap", logConfig),
                smoothnessTextureChannel: GetInt(m, "_SmoothnessTextureChannel", logConfig),
                source: target
                );

            logConfig.LogMaterialVariableData(
                nameof(data.Color),
                data.Color,
                m, mName);
            logConfig.LogMaterialVariableData(
                nameof(data.EmissionColor),
                data.EmissionColor,
                m, mName);
            logConfig.LogMaterialVariableData(
                nameof(data.MainTex),
                data.MainTex,
                m, mName);
            logConfig.LogMaterialVariableData(
                nameof(data.Smoothness),
                data.Smoothness,
                m, mName);
            logConfig.LogMaterialVariableData(
                nameof(data.BumpMap),
                data.BumpMap,
                m, mName);
            logConfig.LogMaterialVariableData(
                nameof(data.EmissionTexture),
                data.EmissionTexture,
                m, mName);
            logConfig.LogMaterialVariableData(
                nameof(data.MetallicTexture),
                data.MetallicTexture,
                m, mName);
            logConfig.LogMaterialVariableData(
                nameof(data.SmoothnessTextureChannel),
                data.SmoothnessTextureChannel,
                m, mName);
            logConfig.LogMaterialVariableData(
                nameof(data.SpecularTexture),
                data.SpecularTexture,
                m, mName);

            return data;
        }

        /// <summary>
        /// Reads all local values from the given material address (if available).
        /// Unless <paramref name="ignoreShaderName"/> is set,
        /// the method returns null if the material's shader's name does not
        /// currently match "Standard"
        /// </summary>
        /// <param name="source">The source material address to read from</param>
        /// <param name="logConfig">Configuration for logging material operations</param>
        /// <param name="type">The type classification for this material</param>
        /// <param name="ignoreShaderName">
        /// If true, will always read the material, regardless of shader name.
        /// If false, will only read the material if its shader name equals "Standard",
        /// return null otherwise</param>
        /// <returns>Read surface shader data or null if the shader name did not match
        /// or the target is (no longer) valid</returns>
        public static UnityMaterialData? From(MaterialAddress source, MaterialLog logConfig, MaterialType type, bool ignoreShaderName = false)
        {
            var material = source.GetMaterial();
            if (material == null)
            {
                Debug.LogError($"Material {source} could not be resolved to an instance");
                return null;
            }
            return From(source, material, logConfig, type, ignoreShaderName);
        }



        /// <summary>
        /// Reads all local values from the given renderer material (if available).
        /// Unless <paramref name="ignoreShaderName"/> is set,
        /// the method returns null if the material's shader's name does not
        /// currently match "Standard"
        /// </summary>
        /// <param name="type">The type classification for this material</param>
        /// <param name="renderer">The source renderer</param>
        /// <param name="materialIndex">The source material index on that renderer</param>
        /// <param name="logConfig">Log Configuration</param>
        /// <param name="ignoreShaderName">
        /// If true, will always read the material, regardless of shader name.
        /// If false, will only read the material if its shader name equals "Standard",
        /// return null otherwise</param>
        /// <returns>Read surface shader data or null if the shader name did not match
        /// or the target is (no longer) valid</returns>
        public static UnityMaterialData? From(Renderer renderer, int materialIndex, MaterialType type, MaterialLog logConfig = default, bool ignoreShaderName = false)
        {
            var a = new MaterialAddress(renderer, materialIndex);
            var m = a.GetMaterial();
            if (m == null)
                return null;
            return From(a, m, logConfig, type, ignoreShaderName: ignoreShaderName);
        }


        /// <summary>
        /// Applies the loaded configuration to the given material
        /// </summary>
        /// <param name="m">Target material</param>
        /// <param name="uniformShininess">If non-null, applies this level of shininess to all materials</param>
        /// <param name="logConfig">Log Configuration</param>
        /// <param name="materialName">Optional custom material name to use instead of the nice name of the material itself</param>
        public void ApplyTo(Material m, float? uniformShininess, MaterialLog logConfig, string? materialName)
        {
            ColorVariable.Set(m, "_Color2", Color, logConfig, materialName);
            ColorVariable.Set(m, "_Color3", Color, logConfig, materialName);
            ColorVariable.Set(m, "_SpecColor", SpecularColor, logConfig, materialName);


            //if (!MainTex && !m.mainTexture)
            //{
            //    logConfig.LogExtraStep($"Main texture not set. Loading white texture");
            //    m.mainTexture = Texture2D.whiteTexture;
            //}

            var existingSpecTex = m.GetTexture(SpecTex);

            var spec = SpecularTexture;

            if (spec && uniformShininess is null)
            {
                if (existingSpecTex != spec)
                {
                    logConfig.LogMaterialVariableSet(UnityEngine.Rendering.ShaderPropertyType.Texture,
                        SpecTexName, existingSpecTex, spec, m, materialName);
                    //                    logConfig.LogExtraStep($"Translating smoothness alpha map {spec.NiceName()} to spec");

                    m.SetTexture(SpecTex, spec);
                }
            }
            else
            {
                if (spec)
                    logConfig.LogExtraStep($"Specular source texture is set but uniform shininess is defined. Ignoring");
                var tex = OnePixelTexture.Get(existingSpecTex);
                if (tex == null)
                {
                    logConfig.LogExtraStep($"Source has no smoothness alpha texture. Setting to {Smoothness} on {materialName ?? m.NiceName()}");
                    var gray = uniformShininess ?? Smoothness;
                    tex = OnePixelTexture.Create(new Color(gray, gray, gray, gray));
                    m.SetTexture(SpecTex, tex.Texture);
                }
                else
                {
                    var gray = uniformShininess ?? Smoothness;
                    var col = new Color(gray, gray, gray, gray);
                    tex.Update(col, (old, nw) => logConfig.LogExtraStep($"Updating smoothness alpha texture. Setting {old} -> {nw} in {materialName ?? m.NiceName()}"));
                }
            }
            var existingIllumTex = m.GetTexture(Illum);

            if (EmissionTexture != null)
            {
                if (EmissionTexture != existingIllumTex)
                {
                    logConfig.LogExtraStep($"Translating emission map {EmissionTexture} to {IllumTexName} on {materialName ?? m.NiceName()}");

                    m.SetTexture(Illum, EmissionTexture);
                }

            }
            else
            {
                if (EmissionColor != Color.black)
                {
                    var tex = OnePixelTexture.Get(existingIllumTex);
                    if (tex != null)
                        tex.Update(EmissionColor, (old, nw) => logConfig.LogExtraStep($"Updating emission color texture. Setting {old} -> {nw} on {materialName ?? m.NiceName()}"));
                    else
                    {
                        logConfig.LogExtraStep($"Translating emission color {EmissionColor} to {IllumTexName} on {materialName ?? m.NiceName()}");
                        tex = OnePixelTexture.Create(EmissionColor);
                        m.SetTexture(Illum, tex.Texture);
                    }
                }
                else if (existingIllumTex != Texture2D.blackTexture)
                {
                    logConfig.LogExtraStep($"Source has no illumination texture and illumination color is black. Loading black into {IllumTexName} on {materialName ?? m.NiceName()}");
                    m.SetTexture(Illum, Texture2D.blackTexture);
                }
            }
        }

        /// <summary>
        /// Creates a clone with a new source material address
        /// </summary>
        /// <param name="source">New source address</param>
        /// <returns>Clone with updated source</returns>
        public UnityMaterialData RedefineSource(MaterialAddress source)
            => new UnityMaterialData(
                type: Type,
                materialName: MaterialName,
                color: Color,
                specularColor: SpecularColor,
                mainTex: MainTex,
                emissionColor: EmissionColor,
                smoothness: Smoothness,
                metallicTexture: MetallicTexture,
                bumpMap: BumpMap,
                emissionTexture: EmissionTexture,
                smoothnessTextureChannel: SmoothnessTextureChannel,
                source: source);

        /// <inheritdoc />
        public override string ToString()
            => Source + $" ({Type})";

    }
}
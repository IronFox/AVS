using AVS.Log;
using AVS.Util;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AVS.MaterialAdapt.Variables
{

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

        public void SetTo(RootModController rmc, Material m, MaterialLog logConfig, string? materialName)
        {
            using var log = SmartLog.LazyForAVS(rmc);
            try
            {
                var oldT = m.GetTexture(Name);
                var oldO = m.GetTextureOffset(Name);
                var oldS = m.GetTextureScale(Name);
                if (oldT != Texture)
                {
                    logConfig.LogMaterialVariableSet(log, Type, Name, oldT, Texture, m, materialName);
                    m.SetTexture(Name, Texture);
                }

                if (oldO != Offset)
                {
                    logConfig.LogMaterialVariableSet(log, Type, Name, oldO, Offset, m, materialName);
                    m.SetTextureOffset(Name, Offset);
                }

                if (oldS != Scale)
                {
                    logConfig.LogMaterialVariableSet(log, Type, Name, oldS, Scale, m, materialName);
                    m.SetTextureScale(Name, Scale);
                }
            }
            catch (Exception ex)
            {
                log.Error(
                    $"Failed to set {Type} {Name} ({Texture.NiceName()}, {Offset}, {Scale}) on {materialName ?? m.NiceName()}",
                    ex);
            }
        }
    }

}

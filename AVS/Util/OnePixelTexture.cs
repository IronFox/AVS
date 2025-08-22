using System;
using System.Data.SqlTypes;
using UnityEngine;

namespace AVS.Util;

internal class OnePixelTexture : INullTestableType
{
    public const string TexName = "SurfaceShaderData.OnePixelTexture";

    private OnePixelTexture(Texture2D tex)
    {
        Texture = tex;
    }

    public Texture2D Texture { get; }

    public static OnePixelTexture? Get(Texture? texture)
    {
        if (texture is Texture2D tex && tex.name == TexName)
            return new OnePixelTexture(tex);
        return null;
    }

    public static OnePixelTexture Create(Color color)
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.name = TexName;
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return new OnePixelTexture(tex);
    }

    public void Update(Color col, Action<Color, Color>? onUpdate)
    {
        var old = Texture.GetPixel(0, 0);
        if (!old.ApproxEquals(col, 0.02f))
        {
            onUpdate?.Invoke(old, col);
            Texture.SetPixel(0, 0, col);
            Texture.Apply();
        }
    }
}
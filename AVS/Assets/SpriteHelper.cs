using AVS.Log;
using AVS.Util;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace AVS.Assets;

/// <summary>
/// Provides helper methods for loading and managing sprites from disk and registering ping sprites.
/// </summary>
public static class SpriteHelper
{
    private static readonly Dictionary<string, Sprite?> _spriteCache = new();

    /// <summary>
    /// Loads a raw <see cref="Sprite"/> from a relative path based on the calling assembly's location.
    /// </summary>
    /// <param name="relativePath">The relative path to the sprite file.</param>
    /// <param name="rmc">The <see cref="RootModController"/> instance for logging purposes.</param>
    /// <returns>The loaded <see cref="Sprite"/>, or null if not found.</returns>
    public static Sprite? GetSpriteRaw(RootModController rmc, string relativePath)
    {
        var modPath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
        var fullPath = Path.Combine(modPath, relativePath);
        return GetSpriteGenericRaw(rmc, fullPath);
    }

    /// <summary>
    /// Loads a required <see cref="Image"/> from a relative path based on the calling assembly's location.
    /// </summary>
    /// <param name="relativePath">Path relative to the executing assembly's path</param>
    /// <param name="rmc">The <see cref="RootModController"/> instance for logging purposes.</param>
    /// <returns>Loaded image</returns>
    /// <exception cref="FileNotFoundException">The file does not exist</exception>
    /// <exception cref="IOException">Sprite loading has failed</exception>
    public static Image RequireImage(RootModController rmc, string relativePath)
    {
        var modPath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
        var fullPath = Path.Combine(modPath, relativePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException(
                $"Image file not found at {fullPath}. Ensure the file exists in the Sprites directory.");
        //LogWriter.Default.Debug($"Loading image from {fullPath}");

        var sprite = GetSpriteGenericRaw(rmc, fullPath);
        if (sprite.IsNull())
            throw new IOException($"Sprite {fullPath} could not be loaded.");
        sprite.name = Path.GetFileNameWithoutExtension(fullPath);
        if (sprite.texture.IsNull())
            throw new IOException($"Sprite {fullPath} has no texture.");
        sprite.texture.name = sprite.name;
        //LogWriter.Default.Debug($"Done loading image {sprite.texture.name}");
        return new Image(sprite);
    }

    /// <summary>
    /// Loads a <see cref="Sprite"/> from a full file path.
    /// </summary>
    /// <param name="fullPath">The full path to the sprite file.</param>
    /// <param name="rmc">The <see cref="RootModController"/> instance for logging purposes.</param>
    /// <returns>The loaded <see cref="Sprite"/>, or null if not found.</returns>
    private static Sprite? GetSpriteGenericRaw(RootModController rmc, string fullPath)
    {
        if (_spriteCache.TryGetValue(fullPath, out var cachedSprite))
            return cachedSprite;
        using var log = SmartLog.ForAVS(rmc);
        try
        {
            var spriteBytes = File.ReadAllBytes(fullPath);
            var SpriteTexture = new Texture2D(128, 128);
            SpriteTexture.LoadImage(spriteBytes);
            var rs = Sprite.Create(SpriteTexture, new Rect(0.0f, 0.0f, SpriteTexture.width, SpriteTexture.height),
                new Vector2(0.5f, 0.5f), 100.0f);
            _spriteCache[fullPath] = rs;
            return rs;
        }
        catch
        {
            log.Warn($"Could not find file {fullPath}. Returning null Sprite.");
            _spriteCache[fullPath] = null;
            return null;
        }
    }


    /// <summary>
    /// List of registered ping sprites, each with a name, ping type, and sprite.
    /// </summary>
    internal static List<(string Name, PingType Type, Sprite Sprite)> PingSprites { get; } = new();

    /// <summary>
    /// Registers a ping sprite with a name and ping type.
    /// </summary>
    /// <param name="name">The name of the ping sprite.</param>
    /// <param name="pt">The ping type.</param>
    /// <param name="pingSprite">The <see cref="Sprite"/> to register.</param>
    public static void RegisterPingSprite(string name, PingType pt, Sprite pingSprite)
    {
        PingSprites.Add((name, pt, pingSprite));
    }
}
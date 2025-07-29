using AVS.Log;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace AVS.Assets
{
    /// <summary>
    /// Provides helper methods for loading and managing sprites from disk and registering ping sprites.
    /// </summary>
    public static class SpriteHelper
    {
        /// <summary>
        /// Loads an <see cref="Atlas.Sprite"/> from the "Sprites" directory relative to the executing assembly.
        /// </summary>
        /// <param name="name">The sprite file name.</param>
        /// <returns>The loaded <see cref="Atlas.Sprite"/>, or null if not found.</returns>
        internal static Atlas.Sprite? GetSpriteInternal(string name)
        {
            string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string fullPath = Path.Combine(modPath, "Sprites", name);
            return GetSpriteGeneric(fullPath);
        }

        private static readonly Dictionary<string, Sprite?> _spriteCache = new Dictionary<string, Sprite?>();

        /// <summary>
        /// Loads an <see cref="Atlas.Sprite"/> from a relative path based on the calling assembly's location.
        /// </summary>
        /// <param name="relativePath">The relative path to the sprite file.</param>
        /// <returns>The loaded <see cref="Atlas.Sprite"/>, or null if not found.</returns>
        public static Atlas.Sprite? GetSprite(string relativePath)
        {
            string modPath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            string fullPath = Path.Combine(modPath, relativePath);
            return GetSpriteGeneric(fullPath);
        }

        /// <summary>
        /// Loads a raw <see cref="Sprite"/> from a relative path based on the calling assembly's location.
        /// </summary>
        /// <param name="relativePath">The relative path to the sprite file.</param>
        /// <returns>The loaded <see cref="Sprite"/>, or null if not found.</returns>
        public static Sprite? GetSpriteRaw(string relativePath)
        {
            string modPath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            string fullPath = Path.Combine(modPath, relativePath);
            return GetSpriteGenericRaw(fullPath);
        }

        /// <summary>
        /// Loads a required <see cref="Image"/> from a relative path based on the calling assembly's location.
        /// </summary>
        /// <param name="relativePath">Path relative to the executing assembly's path</param>
        /// <returns>Loaded image</returns>
        /// <exception cref="FileNotFoundException">The file does not exist</exception>
        /// <exception cref="IOException">Sprite loading has failed</exception>
        public static Image RequireImage(string relativePath)
        {
            string modPath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            string fullPath = Path.Combine(modPath, relativePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Image file not found at {fullPath}. Ensure the file exists in the Sprites directory.");
            LogWriter.Default.Debug($"Loading image from {fullPath}");
            var sprite = GetSpriteGenericRaw(fullPath);
            if (sprite == null)
                throw new IOException($"Sprite {fullPath} could not be loaded.");
            sprite.name = Path.GetFileNameWithoutExtension(fullPath);
            if (sprite.texture == null)
                throw new IOException($"Sprite {fullPath} has no texture.");
            sprite.texture.name = sprite.name;
            LogWriter.Default.Debug($"Done loading image {sprite.texture.name}");
            return new Image(sprite);
        }

        /// <summary>
        /// Loads an <see cref="Atlas.Sprite"/> from a full file path.
        /// </summary>
        /// <param name="fullPath">The full path to the sprite file.</param>
        /// <returns>The loaded <see cref="Atlas.Sprite"/>, or null if not found.</returns>
        private static Atlas.Sprite? GetSpriteGeneric(string fullPath)
        {
            var innerSprite = GetSpriteGenericRaw(fullPath);
            if (innerSprite != null)
            {
                var sprite = new Atlas.Sprite(innerSprite);
                return sprite;
            }
            _spriteCache[fullPath] = null;
            LogWriter.Default.Warn($"Could not find file {fullPath}. Returning null Atlas.Sprite.");
            return null;
        }

        /// <summary>
        /// Loads a <see cref="Sprite"/> from a full file path.
        /// </summary>
        /// <param name="fullPath">The full path to the sprite file.</param>
        /// <returns>The loaded <see cref="Sprite"/>, or null if not found.</returns>
        private static Sprite? GetSpriteGenericRaw(string fullPath)
        {
            if (_spriteCache.TryGetValue(fullPath, out var cachedSprite))
                return cachedSprite;

            try
            {
                byte[] spriteBytes = System.IO.File.ReadAllBytes(fullPath);
                Texture2D SpriteTexture = new Texture2D(128, 128);
                SpriteTexture.LoadImage(spriteBytes);
                var rs = Sprite.Create(SpriteTexture, new Rect(0.0f, 0.0f, SpriteTexture.width, SpriteTexture.height), new Vector2(0.5f, 0.5f), 100.0f);
                _spriteCache[fullPath] = rs;
                return rs;
            }
            catch
            {
                LogWriter.Default.Warn($"Could not find file {fullPath}. Returning null Sprite.");
                _spriteCache[fullPath] = null;
                return null;
            }
        }

        /// <summary>
        /// Creates a <see cref="Sprite"/> from an <see cref="Atlas.Sprite"/>.
        /// </summary>
        /// <param name="sprite">The <see cref="Atlas.Sprite"/> to convert.</param>
        /// <returns>The created <see cref="Sprite"/>.</returns>
        public static Sprite? CreateSpriteFromAtlasSprite(Atlas.Sprite? sprite)
        {
            if (sprite == null)
            {
                LogWriter.Default.Warn("Sprite is null, cannot create Sprite from Atlas.Sprite.");
                return null;
            }
            Texture2D texture = sprite.texture;
            return Sprite.Create(texture, new Rect(0f, 0f, (float)texture.width, (float)texture.height), Vector2.one * 0.5f);
        }

        /// <summary>
        /// List of registered ping sprites, each with a name, ping type, and sprite.
        /// </summary>
        internal static List<(string Name, PingType Type, Atlas.Sprite Sprite)> PingSprites { get; } = new List<(string, PingType, Atlas.Sprite)>();

        /// <summary>
        /// Registers a ping sprite with a name and ping type.
        /// </summary>
        /// <param name="name">The name of the ping sprite.</param>
        /// <param name="pt">The ping type.</param>
        /// <param name="pingSprite">The <see cref="Atlas.Sprite"/> to register.</param>
        public static void RegisterPingSprite(string name, PingType pt, Atlas.Sprite pingSprite)
        {
            PingSprites.Add((name, pt, pingSprite));
        }
    }
}

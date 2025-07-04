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
        internal static Atlas.Sprite GetSpriteInternal(string name)
        {
            string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string fullPath = Path.Combine(modPath, "Sprites", name);
            return GetSpriteGeneric(fullPath);
        }

        /// <summary>
        /// Loads an <see cref="Atlas.Sprite"/> from a relative path based on the calling assembly's location.
        /// </summary>
        /// <param name="relativePath">The relative path to the sprite file.</param>
        /// <returns>The loaded <see cref="Atlas.Sprite"/>, or null if not found.</returns>
        public static Atlas.Sprite GetSprite(string relativePath)
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
        public static Sprite GetSpriteRaw(string relativePath)
        {
            string modPath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            string fullPath = Path.Combine(modPath, relativePath);
            return GetSpriteGenericRaw(fullPath);
        }

        /// <summary>
        /// Loads an <see cref="Atlas.Sprite"/> from a full file path.
        /// </summary>
        /// <param name="fullPath">The full path to the sprite file.</param>
        /// <returns>The loaded <see cref="Atlas.Sprite"/>, or null if not found.</returns>
        private static Atlas.Sprite GetSpriteGeneric(string fullPath)
        {
            Sprite innerSprite = GetSpriteGenericRaw(fullPath);
            if (innerSprite != null)
            {
                return new Atlas.Sprite(innerSprite);
            }
            else return null;
        }

        /// <summary>
        /// Loads a <see cref="Sprite"/> from a full file path.
        /// </summary>
        /// <param name="fullPath">The full path to the sprite file.</param>
        /// <returns>The loaded <see cref="Sprite"/>, or null if not found.</returns>
        private static Sprite GetSpriteGenericRaw(string fullPath)
        {
            try
            {
                byte[] spriteBytes = System.IO.File.ReadAllBytes(fullPath);
                Texture2D SpriteTexture = new Texture2D(128, 128);
                SpriteTexture.LoadImage(spriteBytes);
                return Sprite.Create(SpriteTexture, new Rect(0.0f, 0.0f, SpriteTexture.width, SpriteTexture.height), new Vector2(0.5f, 0.5f), 100.0f);
            }
            catch
            {
                Logger.Warn($"Could not find file {fullPath}. Returning null Sprite.");
                return null;
            }
        }

        /// <summary>
        /// Creates a <see cref="Sprite"/> from an <see cref="Atlas.Sprite"/>.
        /// </summary>
        /// <param name="sprite">The <see cref="Atlas.Sprite"/> to convert.</param>
        /// <returns>The created <see cref="Sprite"/>.</returns>
        public static Sprite CreateSpriteFromAtlasSprite(Atlas.Sprite sprite)
        {
            Texture2D texture = sprite.texture;
            return Sprite.Create(texture, new Rect(0f, 0f, (float)texture.width, (float)texture.height), Vector2.one * 0.5f);
        }

        /// <summary>
        /// List of registered ping sprites, each with a name, ping type, and sprite.
        /// </summary>
        internal static readonly List<(string, PingType, Atlas.Sprite)> PingSprites = new List<(string, PingType, Atlas.Sprite)>();

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

using UnityEngine;

namespace AVS.Assets
{
    /// <summary>
    /// Helper structure for sprites
    /// </summary>
    public readonly struct Image
    {
        /// <summary>
        /// The image loaded as a sprite
        /// </summary>
        public Sprite Sprite { get; }
        /// <summary>
        /// The image loaded as an Atlas.Sprite
        /// </summary>
        public Atlas.Sprite AtlasSprite { get; }
        /// <summary>
        /// Constructs a new <see cref="Image"/> from a <see cref="Sprite"/>.
        /// </summary>
        /// <param name="sprite"></param>
        public Image(Sprite sprite)
        {
            Sprite = sprite;
            AtlasSprite = new Atlas.Sprite(sprite);
        }
    }
}

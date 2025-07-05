using AVS.Engines;
using UnityEngine;

namespace AVS.Assets
{
    /// <summary>
    /// Provides static access to commonly used assets such as sprites, recipes, and default engine instances.
    /// </summary>
    public static class StaticAssets
    {
        /// <summary>
        /// Gets the default sprite used as the icon for mod vehicle that do not customize it.
        /// </summary>
        public static Atlas.Sprite ModVehicleIcon { get; private set; }

        /// <summary>
        /// Gets the default sprite used as the icon for upgrades if the mod vehicle does not customize it.
        /// </summary>
        public static Atlas.Sprite UpgradeIcon { get; private set; }

        ///// <summary>
        ///// Gets the default sprite used as the icon for depth modules if the mod vehicle does not customize it.
        ///// </summary>
        //public static Atlas.Sprite DepthIcon { get; private set; }

        /// <summary>
        /// Gets the default ping sprite for the mod vehicle used in the PDA and other interfaces
        /// if the mod vehicle does not customize it.
        /// </summary>
        public static Atlas.Sprite DefaultPingSprite { get; private set; }

        /// <summary>
        /// Gets the default sprite used for save files of the mod vehicle if the mod vehicle does not customize it.
        /// </summary>
        public static Sprite DefaultSaveFileSprite { get; private set; }

        /// <summary>
        /// Gets the default engine instance for the mod vehicle.
        /// </summary>
        public static AbstractEngine DefaultEngine { get; private set; }

        /// <summary>
        /// Loads and assigns static sprite assets from internal and asset bundle sources.
        /// </summary>
        internal static void GetSprites()
        {
            ModVehicleIcon = Assets.SpriteHelper.GetSpriteInternal("ModVehicleIcon.png");
            UpgradeIcon = Assets.SpriteHelper.GetSpriteInternal("UpgradeIcon.png");
            //DepthIcon = Assets.SpriteHelper.GetSpriteInternal("DepthIcon.png");

            Assets.VehicleAssets DSAssets = Assets.AssetBundleInterface.GetVehicleAssetsFromBundle("modvehiclepingsprite");
            DefaultPingSprite = Assets.AssetBundleInterface.LoadAdditionalSprite(DSAssets.AssetBundleInterface, "ModVehicleSpriteAtlas", "ModVehiclePingSprite");
            DefaultSaveFileSprite = Assets.AssetBundleInterface.LoadAdditionalRawSprite(DSAssets.AssetBundleInterface, "ModVehicleSpriteAtlas", "ModVehiclePingSprite");
            DSAssets.Close();
        }

        /// <summary>
        /// Sets up the default engine for the mod vehicle.
        /// </summary>
        internal static void SetupDefaultAssets()
        {
            DefaultEngine = new Engines.AtramaEngine();
        }
    }
}

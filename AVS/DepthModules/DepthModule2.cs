using AVS.Configuration;
using AVS.UpgradeTypes;
//using AVS.Localization;

namespace AVS.DepthModules
{
    /// <summary>
    /// Represents the second-tier depth module upgrade for vehicles, enhancing their maximum depth capacity.
    /// </summary>
    /// <remarks>This module is part of the vehicle upgrade system and provides an increased depth limit for
    /// supported vehicles. It can be crafted using specific materials and installed in the vehicle's upgrade
    /// console.</remarks>
    public class DepthModule2 : ModVehicleUpgrade
    {
        /// <inheritdoc/>
        public override string ClassId => "AvsDepthModule2";
        /// <inheritdoc/>
        public override string DisplayName => Language.main.Get("VFDepth2FriendlyString");
        /// <inheritdoc/>
        public override string Description => Language.main.Get("VFDepth2Description");
        /// <inheritdoc/>
        public override Recipe Recipe { get; } = NewRecipe
                .StartWith(TechType.TitaniumIngot, 3)
                .Include(TechType.Lithium, 3)
                .Include(TechType.EnameledGlass, 3)
                .Include(TechType.AluminumOxide, 5)
                .Done();

        /// <inheritdoc/>
        public override Atlas.Sprite Icon => Assets.SpriteHelper.GetSprite("Sprites/DepthIcon.png");
        /// <inheritdoc/>
        public override Atlas.Sprite TabIcon => Assets.SpriteHelper.GetSprite("Sprites/DepthIcon.png");
        /// <inheritdoc/>
        public override string TabName => "MVDM";
        /// <inheritdoc/>
        public override string TabDisplayName => Language.main.Get("VFMVDepthModules");
        /// <inheritdoc/>
        public override void OnAdded(AddActionParams param)
        {
            Admin.Utils.EvaluateDepthModules(param);
        }
        /// <inheritdoc/>
        public override void OnRemoved(AddActionParams param)
        {
            Admin.Utils.EvaluateDepthModules(param);
        }
    }
}

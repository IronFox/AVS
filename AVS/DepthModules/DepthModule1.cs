using AVS.Configuration;
using AVS.UpgradeTypes;
//using AVS.Localization;

namespace AVS.DepthModules
{
    /// <summary>
    /// Represents the first depth module upgrade for vehicles, enhancing their maximum depth capability.
    /// </summary>
    /// <remarks>This module can be crafted using specific ingredients and installed on compatible vehicles to
    /// increase their maximum operational depth.  It is part of the modular vehicle upgrade system and is categorized
    /// under the "Depth Modules" tab in the crafting interface.</remarks>
    public class DepthModule1 : AvsVehicleUpgrade
    {
        /// <inheritdoc/>
        public override string ClassId => "AvsDepthModule1";
        /// <inheritdoc/>
        public override string DisplayName => Language.main.Get("AvsDepth1FriendlyString");
        /// <inheritdoc/>
        public override string Description => Language.main.Get("AvsDepth1Description");
        /// <inheritdoc/>
        public override Recipe Recipe { get; } = NewRecipe
                .StartWith(TechType.TitaniumIngot, 1)
                .Include(TechType.Magnetite, 3)
                .Include(TechType.Glass, 3)
                .Include(TechType.AluminumOxide, 3)
                .Done();
        /// <inheritdoc/>
        public override Atlas.Sprite? Icon { get; } = Assets.SpriteHelper.GetSprite("Sprites/DepthIcon.png");
        /// <inheritdoc/>
        public override Atlas.Sprite? TabIcon { get; } = Assets.SpriteHelper.GetSprite("Sprites/DepthIcon.png");
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

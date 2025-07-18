using AVS.Configuration;
using AVS.UpgradeTypes;
//using AVS.Localization;

namespace AVS.DepthModules
{
    /// <summary>
    /// Level 3 depth module for vehicles, allowing deeper dives.
    /// </summary>
    public class DepthModule3 : AvsVehicleUpgrade
    {
        /// <inheritdoc/>
        public override string ClassId => "AvsDepthModule3";
        /// <inheritdoc/>
        public override string DisplayName => Language.main.Get("AvsDepth3FriendlyString");
        /// <inheritdoc/>
        public override string Description => Language.main.Get("AvsDepth3Description");
        /// <inheritdoc/>
        public override Recipe Recipe { get; } = NewRecipe
                .StartWith(TechType.PlasteelIngot, 3)
                .Include(TechType.Nickel, 3)
                .Include(TechType.EnameledGlass, 3)
                .Include(TechType.Kyanite, 3)
                .Done();
        /// <inheritdoc/>

        public override Atlas.Sprite? Icon { get; } = Assets.SpriteHelper.GetSprite("Sprites/DepthIcon.png");
        /// <inheritdoc/>
        public override Atlas.Sprite? TabIcon => Assets.SpriteHelper.GetSprite("Sprites/DepthIcon.png");
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

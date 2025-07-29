using AVS.Configuration;
using AVS.Localization;
//using AVS.Localization;

namespace AVS.UpgradeModules
{
    /// <summary>
    /// Represents the second-tier depth module upgrade for vehicles, enhancing their maximum depth capacity.
    /// </summary>
    /// <remarks>This module is part of the vehicle upgrade system and provides an increased depth limit for
    /// supported vehicles. It can be crafted using specific materials and installed in the vehicle's upgrade
    /// console.</remarks>
    public class DepthModule2 : AvsVehicleUpgrade
    {
        /// <inheritdoc/>
        public override string ClassId => "AvsDepthModule2";
        /// <inheritdoc/>
        public override string DisplayName => Translator.Get(TranslationKey.Module_Depth2_DisplayName);
        /// <inheritdoc/>
        public override string Description => Translator.Get(TranslationKey.Module_Depth2_Description);
        /// <inheritdoc/>
        public override Recipe Recipe { get; } = NewRecipe
                .StartWith(TechType.TitaniumIngot, 3)
                .Include(TechType.Lithium, 3)
                .Include(TechType.EnameledGlass, 3)
                .Include(TechType.AluminumOxide, 5)
                .Done();

        /// <inheritdoc/>
        public override Atlas.Sprite Icon => MainPatcher.Instance.DepthModule2Icon;
        /// <inheritdoc/>
        public override void OnAdded(AddActionParams param)
        {
            AvsUtils.EvaluateDepthModules(param);
        }
        /// <inheritdoc/>
        public override void OnRemoved(AddActionParams param)
        {
            AvsUtils.EvaluateDepthModules(param);
        }
    }
}

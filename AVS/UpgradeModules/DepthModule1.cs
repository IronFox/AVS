using AVS.Configuration;
using AVS.Localization;
//using AVS.Localization;

namespace AVS.UpgradeModules
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
        public override string DisplayName => Translator.Get(TranslationKey.Module_Depth1_DisplayName);
        /// <inheritdoc/>
        public override string Description => Translator.Get(TranslationKey.Module_Depth1_Description);
        /// <inheritdoc/>
        public override Recipe Recipe { get; } = NewRecipe
                .StartWith(TechType.TitaniumIngot, 1)
                .Include(TechType.Magnetite, 3)
                .Include(TechType.Glass, 3)
                .Include(TechType.AluminumOxide, 3)
                .Done();
        /// <inheritdoc/>
        public override Atlas.Sprite Icon => MainPatcher.Instance.DepthModule1Icon;

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

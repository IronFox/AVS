using AVS.Configuration;
using AVS.Localization;
//using AVS.Localization;

namespace AVS.UpgradeModules.Common
{
    /// <summary>
    /// Level 3 depth module for vehicles, allowing deeper dives.
    /// </summary>
    internal class DepthModule3 : DepthModuleBase<DepthModule3>
    {
        /// <inheritdoc/>
        public override string ClassId => "AvsDepthModule3";
        /// <inheritdoc/>
        public override string DisplayName => Translator.Get(TranslationKey.Module_Depth3_DisplayName);
        /// <inheritdoc/>
        public override string Description => Translator.Get(TranslationKey.Module_Depth3_Description);
        /// <inheritdoc/>
        public override Recipe Recipe { get; } = NewRecipe
                .Add(TechType.PlasteelIngot, 3)
                .Add(TechType.Nickel, 3)
                .Add(TechType.EnameledGlass, 3)
                .Add(TechType.Kyanite, 3)
                .Done();
        /// <inheritdoc/>

        public override Atlas.Sprite Icon => MainPatcher.Instance.DepthModule3Icon;
        /// <inheritdoc/>
        public override void OnAdded(AddActionParams param)
        {
            base.OnAdded(param);
            AvsUtils.EvaluateDepthModules(param);
        }
        /// <inheritdoc/>
        public override void OnRemoved(AddActionParams param)
        {
            base.OnRemoved(param);
            AvsUtils.EvaluateDepthModules(param);
        }
    }
}

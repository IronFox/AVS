using AVS.Configuration;
using AVS.Localization;
using UnityEngine;
//using AVS.Localization;

namespace AVS.UpgradeModules.Common
{
    /// <summary>
    /// Represents the first depth module upgrade for vehicles, enhancing their maximum depth capability.
    /// </summary>
    /// <remarks>This module can be crafted using specific ingredients and installed on compatible vehicles to
    /// increase their maximum operational depth.  It is part of the modular vehicle upgrade system and is categorized
    /// under the "Depth Modules" tab in the crafting interface.</remarks>
    internal class DepthModule1 : DepthModuleBase<DepthModule1>
    {
        public DepthModule1(RootModController rmc) : base(rmc)
        {
        }

        /// <inheritdoc/>
        public override string ClassId => Owner.ModName + "DepthModule1";
        /// <inheritdoc/>
        public override string DisplayName => Translator.Get(TranslationKey.Module_Depth1_DisplayName);
        /// <inheritdoc/>
        public override string Description => Translator.Get(TranslationKey.Module_Depth1_Description);
        /// <inheritdoc/>
        public override Recipe Recipe { get; } = NewRecipe
                .Add(TechType.TitaniumIngot, 1)
                .Add(TechType.Magnetite, 3)
                .Add(TechType.Glass, 3)
                .Add(TechType.AluminumOxide, 3)
                .Done();
        /// <inheritdoc/>
        public override Sprite Icon => Owner.DepthModule1Icon;

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

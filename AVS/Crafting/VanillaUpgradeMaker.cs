using AVS.UpgradeModules;
using AVS.UpgradeModules.Variations;
using Nautilus.Assets.Gadgets;
using System.Collections.Generic;

namespace AVS.Crafting
{
    internal static class VanillaUpgradeMaker
    {
        public static List<TechType> CyclopsUpgradeTechTypes = new List<TechType>();
        internal static Nautilus.Assets.CustomPrefab CreateModuleVanilla(AvsVehicleModule upgrade, bool isPdaSetup, Nautilus.Assets.PrefabInfo info)
        {
            Nautilus.Assets.CustomPrefab prefab = new Nautilus.Assets.CustomPrefab(info);
            var clone = new Nautilus.Assets.PrefabTemplates.CloneTemplate(info, TechType.SeamothElectricalDefense);
            prefab.SetGameObject(clone);
            if (!isPdaSetup)
            {
                prefab.SetPdaGroupCategory(TechGroup.VehicleUpgrades, TechCategory.VehicleUpgrades);
            }
            if (!upgrade.UnlockAtStart)
            {
                ScanningGadget scanGadge = prefab.SetUnlock(upgrade.UnlockTechType);
                if (upgrade.UnlockedSprite != null)
                {
                    scanGadge.WithAnalysisTech(upgrade.UnlockedSprite, unlockMessage: upgrade.UnlockedMessage);
                }
            }
            return prefab;
        }

        private static Nautilus.Assets.CustomPrefab AddRecipe(this Nautilus.Assets.CustomPrefab customPrefab, AvsVehicleModule upgrade, VehicleType vType)
        {
            Nautilus.Crafting.RecipeData moduleRecipe = upgrade.GetRecipe(vType).ToRecipeData();

            customPrefab
                .SetRecipe(moduleRecipe)
                .WithFabricatorType(Assets.AvsFabricator.TreeType)
                .WithStepsToFabricatorTab(upgrade.Node.GetPath().Segments)
                .WithCraftingTime(upgrade.CraftingTime);
            return customPrefab;
        }

        #region CreationMethods
        internal static void CreatePassiveModule(AvsVehicleModule upgrade, UpgradeCompat compat, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            if (upgrade.IsVehicleSpecific)
            {
                return;
            }
            bool isPdaRegistered = isPdaSetup;
            if (!compat.SkipSeamoth)
            {
                CreatePassiveModuleSeamoth(upgrade, ref utt, isPdaRegistered);
                isPdaRegistered = true;
            }
            if (!compat.SkipExosuit)
            {
                CreatePassiveModuleExosuit(upgrade, ref utt, isPdaRegistered);
                isPdaRegistered = true;
            }
            if (!compat.SkipCyclops)
            {
                CreatePassiveModuleCyclops(upgrade, ref utt, isPdaRegistered);
            }
        }
        internal static void CreateSelectModule(SelectableModule select, UpgradeCompat compat, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            if (select.IsVehicleSpecific)
            {
                return;
            }
            bool isPdaRegistered = isPdaSetup;
            if (!compat.SkipSeamoth)
            {
                CreateSelectModuleSeamoth(select, ref utt, isPdaRegistered);
                isPdaRegistered = true;
            }
            if (!compat.SkipExosuit)
            {
                CreateSelectModuleExosuit(select, ref utt, isPdaRegistered);
                isPdaRegistered = true;
            }
            if (!compat.SkipCyclops)
            {
                //CreateSelectModuleCyclops(select, ref utt);
            }
        }
        internal static void CreateChargeModule(SelectableChargeableModule selectcharge, UpgradeCompat compat, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            if (selectcharge.IsVehicleSpecific)
            {
                return;
            }
            bool isPdaRegistered = isPdaSetup;
            if (!compat.SkipSeamoth)
            {
                CreateChargeModuleSeamoth(selectcharge, ref utt, isPdaRegistered);
                isPdaRegistered = true;
            }
            if (!compat.SkipExosuit)
            {
                CreateChargeModuleExosuit(selectcharge, ref utt, isPdaRegistered);
                isPdaRegistered = true;
            }
            if (!compat.SkipCyclops)
            {
                //CreateSelectModuleCyclops(select, ref utt);
            }
        }
        internal static void CreateChargeModule(ChargeableModule charge, UpgradeCompat compat, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            if (charge.IsVehicleSpecific)
            {
                return;
            }
            bool isPdaRegistered = isPdaSetup;
            if (!compat.SkipSeamoth)
            {
                CreateChargeModuleSeamoth(charge, ref utt, isPdaRegistered);
                isPdaRegistered = true;
            }
            if (!compat.SkipExosuit)
            {
                CreateChargeModuleExosuit(charge, ref utt, isPdaRegistered);
                isPdaRegistered = true;
            }
            if (!compat.SkipCyclops)
            {
                //CreateSelectModuleCyclops(select, ref utt);
            }
        }
        internal static void CreateToggleModule(ToggleableModule toggle, UpgradeCompat compat, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            if (toggle.IsVehicleSpecific)
            {
                return;
            }
            bool isPdaRegistered = isPdaSetup;
            if (!compat.SkipSeamoth)
            {
                CreateToggleModuleSeamoth(toggle, ref utt, isPdaRegistered);
                isPdaRegistered = true;
            }
            if (!compat.SkipExosuit)
            {
                CreateToggleModuleExosuit(toggle, ref utt, isPdaRegistered);
                isPdaRegistered = true;
            }
            if (!compat.SkipCyclops)
            {
                //CreateSelectModuleCyclops(select, ref utt);
            }
        }

        #endregion

        #region AddActions
        internal static void AddPassiveActions(UpgradeModuleGadget gadget, AvsVehicleModule upgrade, Nautilus.Assets.PrefabInfo info)
        {
            gadget
                 .WithOnModuleAdded((vehicleInstance, slotId) =>
                 {
                     var addedParams = AddActionParams.CreateForVehicle
                     (
                         vehicle: vehicleInstance,
                         slotID: slotId,
                         techType: info.TechType,
                         added: true
                     );
                     upgrade.OnAdded(addedParams);
                 })
                 .WithOnModuleRemoved((vehicleInstance, slotId) =>
                 {
                     var addedParams = AddActionParams.CreateForVehicle
                     (
                         vehicle: vehicleInstance,
                         slotID: slotId,
                         techType: info.TechType,
                         added: false
                     );
                     upgrade.OnAdded(addedParams);
                 });
        }
        internal static void AddSelectActions(UpgradeModuleGadget gadget, SelectableModule upgrade, Nautilus.Assets.PrefabInfo info)
        {
            gadget
                .WithCooldown(upgrade.Cooldown)
                .WithEnergyCost(upgrade.EnergyCost)
                .WithOnModuleUsed((vehicleInstance, slotId, charge, chargeFraction) =>
                {
                    var selectParams = new SelectableModule.Params
                    (
                        vehicle: vehicleInstance,
                        slotID: slotId,
                        techType: info.TechType
                    );
                    upgrade.OnSelected(selectParams);
                });
        }
        internal static void AddToggleActions(UpgradeModuleGadget gadget, ToggleableModule upgrade, Nautilus.Assets.PrefabInfo info)
        {
            gadget
                .WithOnModuleToggled((vehicleInstance, slotId, energyCost, isActive) =>
                {
                    var param = new ToggleableModule.Params
                    (
                        isActive: isActive,
                        vehicle: vehicleInstance,
                        slotID: slotId,
                        techType: info.TechType
                    );
                    UpgradeRegistrar.OnToggleActions.ForEach(x => x(param));
                });
        }
        internal static void AddChargeActions(UpgradeModuleGadget gadget, SelectableChargeableModule upgrade, Nautilus.Assets.PrefabInfo info)
        {
            gadget
                .WithMaxCharge(upgrade.ChargeLimit) // this creates a harmless Nautilus warning
                .WithEnergyCost(upgrade.EnergyCostPerSecond) // this creates a harmless Nautilus warning
                .WithOnModuleUsed((vehicleInstance, slotId, charge, chargeFraction) =>
                {
                    var chargeParams = new SelectableChargeableModule.Params
                    (
                        vehicle: vehicleInstance,
                        slotID: slotId,
                        techType: info.TechType,
                        charge: charge,
                        chargeFraction: chargeFraction
                    );
                    upgrade.OnActivate(chargeParams);
                });
        }
        internal static void AddChargeActions(UpgradeModuleGadget gadget, ChargeableModule upgrade, Nautilus.Assets.PrefabInfo info)
        {
            gadget
                .WithMaxCharge(upgrade.ChargeLimit) // this creates a harmless Nautilus warning
                .WithEnergyCost(upgrade.EnergyCostPerSecond) // this creates a harmless Nautilus warning
                .WithOnModuleUsed((vehicleInstance, slotId, charge, chargeFraction) =>
                {
                    var chargeParams = new ChargeableModule.Params
                    (
                        vehicle: vehicleInstance,
                        slotID: slotId,
                        techType: info.TechType,
                        charge: charge,
                        chargeFraction: chargeFraction
                    );
                    upgrade.OnActivate(chargeParams);
                });
        }
        #endregion

        #region PassiveModules
        internal static TechType CreatePassiveModuleVanilla(AvsVehicleModule upgrade, bool isPdaSetup, Nautilus.Assets.PrefabInfo info, EquipmentType equipType, VehicleType vType)
        {
            Nautilus.Assets.CustomPrefab prefab = CreateModuleVanilla(upgrade, isPdaSetup, info)
                .AddRecipe(upgrade, vType);
            UpgradeModuleGadget gadget = prefab.SetVehicleUpgradeModule(equipType, QuickSlotType.Passive);
            AddPassiveActions(gadget, upgrade, info);
            prefab.Register();
            upgrade.TechTypes = upgrade.TechTypes.ReplaceVehicleType(vType, info.TechType, false);
            upgrade.LastRegisteredTechType = info.TechType;
            return info.TechType;
        }
        internal static void CreatePassiveModuleSeamoth(AvsVehicleModule upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Seamoth", "Seamoth " + upgrade.DisplayName, "An upgrade for the Seamoth. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceSeamoth(prefabInfo.TechType);
            CreatePassiveModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.SeamothModule, VehicleType.Seamoth);
        }
        internal static void CreatePassiveModuleExosuit(AvsVehicleModule upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Exosuit", "Exosuit " + upgrade.DisplayName, "An upgrade for the Exosuit. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceExosuit(prefabInfo.TechType);
            CreatePassiveModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.ExosuitModule, VehicleType.Prawn);
        }
        internal static void CreatePassiveModuleCyclops(AvsVehicleModule upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Cyclops", "Cyclops " + upgrade.DisplayName, "An upgrade for the Cyclops. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceCyclops(prefabInfo.TechType);
            Nautilus.Assets.CustomPrefab prefab = new Nautilus.Assets.CustomPrefab(prefabInfo);
            var clone = new Nautilus.Assets.PrefabTemplates.CloneTemplate(prefabInfo, TechType.SeamothElectricalDefense);
            prefab.SetGameObject(clone);
            if (!isPdaSetup)
            {
                prefab.SetPdaGroupCategory(TechGroup.Cyclops, TechCategory.CyclopsUpgrades);
            }
            if (!upgrade.UnlockAtStart)
            {
                ScanningGadget scanGadge = prefab.SetUnlock(upgrade.UnlockTechType);
                if (upgrade.UnlockedSprite != null)
                {
                    scanGadge.WithAnalysisTech(upgrade.UnlockedSprite, unlockMessage: upgrade.UnlockedMessage);
                }
            }
            prefab.AddRecipe(upgrade, VehicleType.Cyclops);
            prefab.SetEquipment(EquipmentType.CyclopsModule);
            prefab.Register();
            upgrade.RegisterTechTypeFor(VehicleType.Cyclops, prefabInfo.TechType);
            CyclopsUpgradeTechTypes.Add(upgrade.UnlockTechType);
        }
        #endregion

        #region SelectModules
        internal static TechType CreateSelectModuleVanilla(SelectableModule upgrade, bool isPdaSetup, Nautilus.Assets.PrefabInfo info, EquipmentType equipType, VehicleType vType)
        {
            Nautilus.Assets.CustomPrefab prefab = CreateModuleVanilla(upgrade, isPdaSetup, info)
                .AddRecipe(upgrade, vType);
            UpgradeModuleGadget gadget = prefab.SetVehicleUpgradeModule(equipType, QuickSlotType.Selectable);
            AddPassiveActions(gadget, upgrade, info);
            AddSelectActions(gadget, upgrade, info);
            prefab.Register();
            upgrade.RegisterTechTypeFor(vType, info.TechType);
            return info.TechType;
        }
        internal static void CreateSelectModuleSeamoth(SelectableModule upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Seamoth", "Seamoth " + upgrade.DisplayName, "An upgrade for the Seamoth. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceSeamoth(prefabInfo.TechType);
            CreateSelectModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.SeamothModule, VehicleType.Seamoth);
        }
        internal static void CreateSelectModuleExosuit(SelectableModule upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Exosuit", "Exosuit " + upgrade.DisplayName, "An upgrade for the Exosuit. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceExosuit(prefabInfo.TechType);
            CreateSelectModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.ExosuitModule, VehicleType.Prawn);
        }
        internal static void CreateSelectModuleCyclops(SelectableModule upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Cyclops", "Cyclops " + upgrade.DisplayName, "An upgrade for the Exosuit. " + upgrade.Description)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceCyclops(prefabInfo.TechType);
            CreateSelectModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.CyclopsModule, VehicleType.Cyclops);
        }
        #endregion

        #region ChargeModules
        internal static TechType CreateChargeModuleVanilla(SelectableChargeableModule upgrade, bool isPdaSetup, Nautilus.Assets.PrefabInfo info, EquipmentType equipType, VehicleType vType)
        {
            Nautilus.Assets.CustomPrefab prefab = CreateModuleVanilla(upgrade, isPdaSetup, info)
                .AddRecipe(upgrade, vType);
            UpgradeModuleGadget gadget = prefab.SetVehicleUpgradeModule(equipType, QuickSlotType.SelectableChargeable);
            AddPassiveActions(gadget, upgrade, info);
            AddChargeActions(gadget, upgrade, info);
            prefab.Register();
            upgrade.RegisterTechTypeFor(vType, info.TechType);
            return info.TechType;
        }
        internal static void CreateChargeModuleSeamoth(SelectableChargeableModule upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Seamoth", "Seamoth " + upgrade.DisplayName, "An upgrade for the Seamoth. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceSeamoth(prefabInfo.TechType);
            CreateChargeModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.SeamothModule, VehicleType.Seamoth);
        }
        internal static void CreateChargeModuleExosuit(SelectableChargeableModule upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Exosuit", "Exosuit " + upgrade.DisplayName, "An upgrade for the Exosuit. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceExosuit(prefabInfo.TechType);
            CreateChargeModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.ExosuitModule, VehicleType.Prawn);
        }
        internal static void CreateChargeModuleCyclops(SelectableChargeableModule upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Cyclops", "Cyclops " + upgrade.DisplayName, "An upgrade for the Exosuit. " + upgrade.Description)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceCyclops(prefabInfo.TechType);
            CreateChargeModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.CyclopsModule, VehicleType.Cyclops);
        }

        internal static TechType CreateChargeModuleVanilla(ChargeableModule upgrade, bool isPdaSetup, Nautilus.Assets.PrefabInfo info, EquipmentType equipType, VehicleType vType)
        {
            Nautilus.Assets.CustomPrefab prefab = CreateModuleVanilla(upgrade, isPdaSetup, info)
                .AddRecipe(upgrade, vType);
            UpgradeModuleGadget gadget = prefab.SetVehicleUpgradeModule(equipType, QuickSlotType.SelectableChargeable);
            AddPassiveActions(gadget, upgrade, info);
            AddChargeActions(gadget, upgrade, info);
            prefab.Register();
            upgrade.RegisterTechTypeFor(vType, info.TechType);
            return info.TechType;
        }
        internal static void CreateChargeModuleSeamoth(ChargeableModule upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Seamoth", "Seamoth " + upgrade.DisplayName, "An upgrade for the Seamoth. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceSeamoth(prefabInfo.TechType);
            CreateChargeModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.SeamothModule, VehicleType.Seamoth);
        }
        internal static void CreateChargeModuleExosuit(ChargeableModule upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Exosuit", "Exosuit " + upgrade.DisplayName, "An upgrade for the Exosuit. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceExosuit(prefabInfo.TechType);
            CreateChargeModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.ExosuitModule, VehicleType.Prawn);
        }
        internal static void CreateChargeModuleCyclops(ChargeableModule upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Cyclops", "Cyclops " + upgrade.DisplayName, "An upgrade for the Exosuit. " + upgrade.Description)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceCyclops(prefabInfo.TechType);
            CreateChargeModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.CyclopsModule, VehicleType.Cyclops);
        }
        #endregion

        #region ToggleModules
        internal static TechType CreateToggleModuleVanilla(ToggleableModule upgrade, bool isPdaSetup, Nautilus.Assets.PrefabInfo info, EquipmentType equipType, VehicleType vType)
        {
            Nautilus.Assets.CustomPrefab prefab = CreateModuleVanilla(upgrade, isPdaSetup, info)
                .AddRecipe(upgrade, vType);
            UpgradeModuleGadget gadget = prefab.SetVehicleUpgradeModule(equipType, QuickSlotType.Toggleable);
            AddPassiveActions(gadget, upgrade, info);
            AddToggleActions(gadget, upgrade, info);
            prefab.Register();
            upgrade.RegisterTechTypeFor(vType, info.TechType);
            return info.TechType;
        }
        internal static void CreateToggleModuleSeamoth(ToggleableModule upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Seamoth", "Seamoth " + upgrade.DisplayName, "An upgrade for the Seamoth. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceSeamoth(prefabInfo.TechType);
            CreateToggleModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.SeamothModule, VehicleType.Seamoth);
        }
        internal static void CreateToggleModuleExosuit(ToggleableModule upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Exosuit", "Exosuit " + upgrade.DisplayName, "An upgrade for the Exosuit. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceExosuit(prefabInfo.TechType);
            CreateToggleModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.ExosuitModule, VehicleType.Prawn);
        }
        internal static void CreateToggleModuleCyclops(ToggleableModule upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Cyclops", "Cyclops " + upgrade.DisplayName, "An upgrade for the Exosuit. " + upgrade.Description)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceCyclops(prefabInfo.TechType);
            CreateToggleModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.CyclopsModule, VehicleType.Cyclops);
        }
        #endregion

    }
}

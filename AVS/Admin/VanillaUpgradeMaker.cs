﻿using AVS.UpgradeTypes;
using AVS.Util;
using Nautilus.Assets.Gadgets;
using System.Collections.Generic;

namespace AVS.Admin
{
    internal static class VanillaUpgradeMaker
    {
        public static List<TechType> CyclopsUpgradeTechTypes = new List<TechType>();
        internal static Nautilus.Assets.CustomPrefab CreateModuleVanilla(AvsVehicleUpgrade upgrade, bool isPdaSetup, Nautilus.Assets.PrefabInfo info)
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
                ScanningGadget scanGadge = prefab.SetUnlock(upgrade.UnlockTechType == TechType.Fragment ? upgrade.UnlockWith : upgrade.UnlockTechType);
                if (upgrade.UnlockedSprite != null)
                {
                    scanGadge.WithAnalysisTech(upgrade.UnlockedSprite, unlockMessage: upgrade.UnlockedMessage);
                }
            }
            return prefab;
        }

        private static Nautilus.Assets.CustomPrefab AddRecipe(this Nautilus.Assets.CustomPrefab customPrefab, AvsVehicleUpgrade upgrade, VehicleType vType)
        {
            Nautilus.Crafting.RecipeData moduleRecipe = upgrade.GetRecipe(vType).ToRecipeData();

            var steps = upgrade.ResolvePath(vType);
            customPrefab
                .SetRecipe(moduleRecipe)
                .WithFabricatorType(Assets.AVSFabricator.TreeType)
                .WithStepsToFabricatorTab(steps.ToArray())
                .WithCraftingTime(upgrade.CraftingTime);
            return customPrefab;
        }

        #region CreationMethods
        internal static void CreatePassiveModule(AvsVehicleUpgrade upgrade, UpgradeCompat compat, ref UpgradeTechTypes utt, bool isPdaSetup)
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
        internal static void CreateSelectModule(SelectableUpgrade select, UpgradeCompat compat, ref UpgradeTechTypes utt, bool isPdaSetup)
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
        internal static void CreateChargeModule(SelectableChargeableUpgrade selectcharge, UpgradeCompat compat, ref UpgradeTechTypes utt, bool isPdaSetup)
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
        internal static void CreateToggleModule(ToggleableUpgrade toggle, UpgradeCompat compat, ref UpgradeTechTypes utt, bool isPdaSetup)
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
        internal static void AddPassiveActions(UpgradeModuleGadget gadget, AvsVehicleUpgrade upgrade, Nautilus.Assets.PrefabInfo info)
        {
            gadget
                 .WithOnModuleAdded((Vehicle vehicleInstance, int slotId) =>
                 {
                     UpgradeTypes.AddActionParams addedParams = new UpgradeTypes.AddActionParams
                     {
                         vehicle = vehicleInstance,
                         slotID = slotId,
                         techType = info.TechType,
                         isAdded = true
                     };
                     upgrade.OnAdded(addedParams);
                 })
                 .WithOnModuleRemoved((Vehicle vehicleInstance, int slotId) =>
                 {
                     UpgradeTypes.AddActionParams addedParams = new UpgradeTypes.AddActionParams
                     {
                         vehicle = vehicleInstance,
                         slotID = slotId,
                         techType = info.TechType,
                         isAdded = false
                     };
                     upgrade.OnAdded(addedParams);
                 });
        }
        internal static void AddSelectActions(UpgradeModuleGadget gadget, SelectableUpgrade upgrade, Nautilus.Assets.PrefabInfo info)
        {
            gadget
                .WithCooldown(upgrade.Cooldown)
                .WithEnergyCost(upgrade.EnergyCost)
                .WithOnModuleUsed((Vehicle vehicleInstance, int slotId, float charge, float chargeFraction) =>
                {
                    UpgradeTypes.SelectableActionParams selectParams = new UpgradeTypes.SelectableActionParams
                    (
                        vehicle: vehicleInstance,
                        slotID: slotId,
                        techType: info.TechType
                    );
                    upgrade.OnSelected(selectParams);
                });
        }
        internal static void AddToggleActions(UpgradeModuleGadget gadget, ToggleableUpgrade upgrade, Nautilus.Assets.PrefabInfo info)
        {
            gadget
                .WithOnModuleToggled((Vehicle vehicleInstance, int slotId, float energyCost, bool isActive) =>
                {
                    UpgradeTypes.ToggleActionParams param = new UpgradeTypes.ToggleActionParams
                    {
                        active = isActive,
                        vehicle = vehicleInstance,
                        slotID = slotId,
                        techType = info.TechType
                    };
                    Admin.UpgradeRegistrar.OnToggleActions.ForEach(x => x(param));
                });
        }
        internal static void AddChargeActions(UpgradeModuleGadget gadget, SelectableChargeableUpgrade upgrade, Nautilus.Assets.PrefabInfo info)
        {
            gadget
                .WithMaxCharge(upgrade.MaxCharge) // this creates a harmless Nautilus warning
                .WithEnergyCost(upgrade.EnergyCost) // this creates a harmless Nautilus warning
                .WithOnModuleUsed((Vehicle vehicleInstance, int slotId, float charge, float chargeFraction) =>
                {
                    UpgradeTypes.SelectableChargeableActionParams chargeParams = new UpgradeTypes.SelectableChargeableActionParams
                    (
                        vehicle: vehicleInstance,
                        slotID: slotId,
                        techType: info.TechType,
                        charge: charge,
                        slotCharge: chargeFraction
                    );
                    upgrade.OnSelected(chargeParams);
                });
        }
        #endregion

        #region PassiveModules
        internal static TechType CreatePassiveModuleVanilla(AvsVehicleUpgrade upgrade, bool isPdaSetup, Nautilus.Assets.PrefabInfo info, EquipmentType equipType, VehicleType vType)
        {
            Nautilus.Assets.CustomPrefab prefab = CreateModuleVanilla(upgrade, isPdaSetup, info)
                .AddRecipe(upgrade, vType);
            UpgradeModuleGadget gadget = prefab.SetVehicleUpgradeModule(equipType, QuickSlotType.Passive);
            AddPassiveActions(gadget, upgrade, info);
            prefab.Register();
            upgrade.UnlockTechType = info.TechType;
            return info.TechType;
        }
        internal static void CreatePassiveModuleSeamoth(AvsVehicleUpgrade upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Seamoth", "Seamoth " + upgrade.DisplayName, "An upgrade for the Seamoth. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceSeamoth(prefabInfo.TechType);
            CreatePassiveModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.SeamothModule, VehicleType.Seamoth);
        }
        internal static void CreatePassiveModuleExosuit(AvsVehicleUpgrade upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Exosuit", "Exosuit " + upgrade.DisplayName, "An upgrade for the Exosuit. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceExosuit(prefabInfo.TechType);
            CreatePassiveModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.ExosuitModule, VehicleType.Prawn);
        }
        internal static void CreatePassiveModuleCyclops(AvsVehicleUpgrade upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
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
                ScanningGadget scanGadge = prefab.SetUnlock(upgrade.UnlockTechType == TechType.Fragment ? upgrade.UnlockWith : upgrade.UnlockTechType);
                if (upgrade.UnlockedSprite != null)
                {
                    scanGadge.WithAnalysisTech(upgrade.UnlockedSprite, unlockMessage: upgrade.UnlockedMessage);
                }
            }
            prefab.AddRecipe(upgrade, VehicleType.Cyclops);
            prefab.SetEquipment(EquipmentType.CyclopsModule);
            prefab.Register();
            upgrade.UnlockTechType = prefabInfo.TechType;
            CyclopsUpgradeTechTypes.Add(upgrade.UnlockTechType);
        }
        #endregion

        #region SelectModules
        internal static TechType CreateSelectModuleVanilla(SelectableUpgrade upgrade, bool isPdaSetup, Nautilus.Assets.PrefabInfo info, EquipmentType equipType, VehicleType vType)
        {
            Nautilus.Assets.CustomPrefab prefab = CreateModuleVanilla(upgrade, isPdaSetup, info)
                .AddRecipe(upgrade, vType);
            UpgradeModuleGadget gadget = prefab.SetVehicleUpgradeModule(equipType, QuickSlotType.Selectable);
            AddPassiveActions(gadget, upgrade, info);
            AddSelectActions(gadget, upgrade, info);
            prefab.Register();
            upgrade.UnlockTechType = info.TechType;
            return info.TechType;
        }
        internal static void CreateSelectModuleSeamoth(SelectableUpgrade upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Seamoth", "Seamoth " + upgrade.DisplayName, "An upgrade for the Seamoth. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceSeamoth(prefabInfo.TechType);
            CreateSelectModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.SeamothModule, VehicleType.Seamoth);
        }
        internal static void CreateSelectModuleExosuit(SelectableUpgrade upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Exosuit", "Exosuit " + upgrade.DisplayName, "An upgrade for the Exosuit. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceExosuit(prefabInfo.TechType);
            CreateSelectModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.ExosuitModule, VehicleType.Prawn);
        }
        internal static void CreateSelectModuleCyclops(SelectableUpgrade upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Cyclops", "Cyclops " + upgrade.DisplayName, "An upgrade for the Exosuit. " + upgrade.Description)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceCyclops(prefabInfo.TechType);
            CreateSelectModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.CyclopsModule, VehicleType.Cyclops);
        }
        #endregion

        #region ChargeModules
        internal static TechType CreateChargeModuleVanilla(SelectableChargeableUpgrade upgrade, bool isPdaSetup, Nautilus.Assets.PrefabInfo info, EquipmentType equipType, VehicleType vType)
        {
            Nautilus.Assets.CustomPrefab prefab = CreateModuleVanilla(upgrade, isPdaSetup, info)
                .AddRecipe(upgrade, vType);
            UpgradeModuleGadget gadget = prefab.SetVehicleUpgradeModule(equipType, QuickSlotType.SelectableChargeable);
            AddPassiveActions(gadget, upgrade, info);
            AddChargeActions(gadget, upgrade, info);
            prefab.Register();
            upgrade.UnlockTechType = info.TechType;
            return info.TechType;
        }
        internal static void CreateChargeModuleSeamoth(SelectableChargeableUpgrade upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Seamoth", "Seamoth " + upgrade.DisplayName, "An upgrade for the Seamoth. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceSeamoth(prefabInfo.TechType);
            CreateChargeModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.SeamothModule, VehicleType.Seamoth);
        }
        internal static void CreateChargeModuleExosuit(SelectableChargeableUpgrade upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Exosuit", "Exosuit " + upgrade.DisplayName, "An upgrade for the Exosuit. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceExosuit(prefabInfo.TechType);
            CreateChargeModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.ExosuitModule, VehicleType.Prawn);
        }
        internal static void CreateChargeModuleCyclops(SelectableChargeableUpgrade upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Cyclops", "Cyclops " + upgrade.DisplayName, "An upgrade for the Exosuit. " + upgrade.Description)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceCyclops(prefabInfo.TechType);
            CreateChargeModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.CyclopsModule, VehicleType.Cyclops);
        }
        #endregion

        #region ToggleModules
        internal static TechType CreateToggleModuleVanilla(ToggleableUpgrade upgrade, bool isPdaSetup, Nautilus.Assets.PrefabInfo info, EquipmentType equipType, VehicleType vType)
        {
            Nautilus.Assets.CustomPrefab prefab = CreateModuleVanilla(upgrade, isPdaSetup, info)
                .AddRecipe(upgrade, vType);
            UpgradeModuleGadget gadget = prefab.SetVehicleUpgradeModule(equipType, QuickSlotType.Toggleable);
            AddPassiveActions(gadget, upgrade, info);
            AddToggleActions(gadget, upgrade, info);
            prefab.Register();
            upgrade.UnlockTechType = info.TechType;
            return info.TechType;
        }
        internal static void CreateToggleModuleSeamoth(ToggleableUpgrade upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Seamoth", "Seamoth " + upgrade.DisplayName, "An upgrade for the Seamoth. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceSeamoth(prefabInfo.TechType);
            CreateToggleModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.SeamothModule, VehicleType.Seamoth);
        }
        internal static void CreateToggleModuleExosuit(ToggleableUpgrade upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Exosuit", "Exosuit " + upgrade.DisplayName, "An upgrade for the Exosuit. " + upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceExosuit(prefabInfo.TechType);
            CreateToggleModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.ExosuitModule, VehicleType.Prawn);
        }
        internal static void CreateToggleModuleCyclops(ToggleableUpgrade upgrade, ref UpgradeTechTypes utt, bool isPdaSetup)
        {
            var prefabInfo = Nautilus.Assets.PrefabInfo.WithTechType(upgrade.ClassId + "Cyclops", "Cyclops " + upgrade.DisplayName, "An upgrade for the Exosuit. " + upgrade.Description)
                .WithIcon(upgrade.Icon);
            utt = utt.ReplaceCyclops(prefabInfo.TechType);
            CreateToggleModuleVanilla(upgrade, isPdaSetup, prefabInfo, EquipmentType.CyclopsModule, VehicleType.Cyclops);
        }
        #endregion

    }
}

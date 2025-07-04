using AVS.Assets;
using AVS.UpgradeTypes;
using Nautilus.Assets.Gadgets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.Admin
{
    /// <summary>
    /// Specifies compatibility flags for registering upgrades with different vehicle types.
    /// </summary>
    public struct UpgradeCompat
    {
        /// <summary>
        /// If true, skip registering for ModVehicle.
        /// </summary>
        public bool skipModVehicle;
        /// <summary>
        /// If true, skip registering for Seamoth.
        /// </summary>
        public bool skipSeamoth;
        /// <summary>
        /// If true, skip registering for Exosuit.
        /// </summary>
        public bool skipExosuit;
        /// <summary>
        /// If true, skip registering for Cyclops.
        /// </summary>
        public bool skipCyclops;
    }
    /// <summary>
    /// Holds TechTypes for an upgrade for each supported vehicle type.
    /// </summary>
    public readonly struct UpgradeTechTypes
    {
        /// <summary>
        /// Registration result to use for mod vehicles.
        /// </summary>
        public TechType ForModVehicle { get; }
        /// <summary>
        /// Registration result to use for Seamoth vehicles.
        /// </summary>
        public TechType ForSeamoth { get; }
        /// <summary>
        /// Registration result to use for PRAWN exosuits.
        /// </summary>
        public TechType ForExosuit { get; }
        /// <summary>
        /// Registration result to use for Cyclops submarines.
        /// </summary>
        public TechType ForCyclops { get; }

        /// <summary>
        /// Constructor for UpgradeTechTypes.
        /// </summary>
        internal UpgradeTechTypes(TechType forModVehicle = TechType.None, TechType forSeamoth = TechType.None, TechType forExosuit = TechType.None, TechType forCyclops = TechType.None)
        {
            ForModVehicle = forModVehicle;
            ForSeamoth = forSeamoth;
            ForExosuit = forExosuit;
            ForCyclops = forCyclops;
        }
        /// <summary>
        /// Determines whether the specified <see cref="TechType"/> is associated with any of the supported vehicles.
        /// </summary>
        /// <param name="techType">The <see cref="TechType"/> to check for association with a vehicle.</param>
        /// <returns><see langword="true"/> if the specified <see cref="TechType"/> is associated with  <see
        /// langword="ForModVehicle"/>, <see langword="ForSeamoth"/>, <see langword="ForExosuit"/>,  or <see
        /// langword="ForCyclops"/>; otherwise, <see langword="false"/>.</returns>
        public bool HasTechType(TechType techType)
        {
            return ForModVehicle == techType
                || ForSeamoth == techType
                || ForExosuit == techType
                || ForCyclops == techType;
        }

        internal UpgradeTechTypes ReplaceSeamoth(TechType techType)
        {
            return new UpgradeTechTypes(
                forModVehicle: ForModVehicle,
                forSeamoth: techType,
                forExosuit: ForExosuit,
                forCyclops: ForCyclops
            );
        }

        internal UpgradeTechTypes ReplaceExosuit(TechType techType)
        {
            return new UpgradeTechTypes(
                forModVehicle: ForModVehicle,
                forSeamoth: ForSeamoth,
                forExosuit: techType,
                forCyclops: ForCyclops
            );
        }

        internal UpgradeTechTypes ReplaceCyclops(TechType techType)
        {
            return new UpgradeTechTypes(
                forModVehicle: ForModVehicle,
                forSeamoth: ForSeamoth,
                forExosuit: ForExosuit,
                forCyclops: techType
            );
        }
    }
    /// <summary>
    /// Enum representing supported vehicle types.
    /// </summary>
    public enum VehicleType
    {
        /// <summary>
        /// Any mod vehicle type
        /// </summary>
        ModVehicle,
        /// <summary>
        /// The Seamoth vehicle type.
        /// </summary>
        Seamoth,
        /// <summary>
        /// The Prawn exosuit vehicle type.
        /// </summary>
        Prawn,
        /// <summary>
        /// The Cyclops vehicle type.
        /// </summary>
        Cyclops,
        /// <summary>
        /// Vehicle specific type, used for upgrades that are specific to one vehicle type.
        /// </summary>
        Custom
    }
    /// <summary>
    /// Handles registration and management of vehicle upgrades, including their icons, actions, and compatibility.
    /// </summary>
    public static class UpgradeRegistrar
    {
        /// <summary>
        /// Dictionary of upgrade icons, indexed by upgrade ClassId.
        /// </summary>
        public static Dictionary<string, Sprite> UpgradeIcons = new Dictionary<string, Sprite>();

        /// <summary>
        /// List of actions to invoke when an upgrade is added or removed.
        /// </summary>
        internal static List<Action<AddActionParams>> OnAddActions = new List<Action<AddActionParams>>();
        /// <summary>
        /// List of actions to invoke when a toggleable upgrade is toggled.
        /// </summary>
        internal static List<Action<ToggleActionParams>> OnToggleActions = new List<Action<ToggleActionParams>>();
        /// <summary>
        /// List of actions to invoke when a selectable chargeable upgrade is selected.
        /// </summary>
        internal static List<Action<SelectableChargeableActionParams>> OnSelectChargeActions = new List<Action<SelectableChargeableActionParams>>();
        /// <summary>
        /// List of actions to invoke when a selectable upgrade is selected.
        /// </summary>
        internal static List<Action<SelectableActionParams>> OnSelectActions = new List<Action<SelectableActionParams>>();
        /// <summary>
        /// List of actions to invoke when an arm action is performed (down).
        /// </summary>
        internal static List<Action<ArmActionParams>> OnArmDownActions = new List<Action<ArmActionParams>>();
        /// <summary>
        /// List of actions to invoke when an arm action is held.
        /// </summary>
        internal static List<Action<ArmActionParams>> OnArmHeldActions = new List<Action<ArmActionParams>>();
        /// <summary>
        /// List of actions to invoke when an arm action is released (up).
        /// </summary>
        internal static List<Action<ArmActionParams>> OnArmUpActions = new List<Action<ArmActionParams>>();
        /// <summary>
        /// List of actions to invoke when an alternate arm action is performed.
        /// </summary>
        internal static List<Action<ArmActionParams>> OnArmAltActions = new List<Action<ArmActionParams>>();
        /// <summary>
        /// Tracks currently toggled actions for vehicles, by vehicle, slot, and coroutine.
        /// </summary>
        internal static List<Tuple<Vehicle, int, Coroutine>> toggledActions = new List<Tuple<Vehicle, int, Coroutine>>();

        /// <summary>
        /// Registers a ModVehicleUpgrade and sets up its icons, recipes, and actions for compatible vehicle types.
        /// </summary>
        /// <param name="upgrade">The upgrade to register.</param>
        /// <param name="compat">Compatibility flags for vehicle types.</param>
        /// <param name="verbose">If true, enables verbose logging.</param>
        /// <returns>UpgradeTechTypes containing TechTypes for each vehicle type.</returns>
        public static UpgradeTechTypes RegisterUpgrade(ModVehicleUpgrade upgrade, UpgradeCompat compat = default(UpgradeCompat), bool verbose = false)
        {
            Logger.Log("Registering ModVehicleUpgrade " + upgrade.ClassId + " : " + upgrade.DisplayName);
            bool result = ValidateModVehicleUpgrade(upgrade, compat);
            if (result)
            {
                CraftTreeHandler.EnsureCraftingTabsAvailable(upgrade, compat);
                UpgradeIcons.Add(upgrade.ClassId, SpriteHelper.CreateSpriteFromAtlasSprite(upgrade.Icon));
                UpgradeTechTypes utt = new UpgradeTechTypes();
                bool isPdaRegistered = false;
                if (!compat.skipModVehicle || upgrade.IsVehicleSpecific)
                {
                    utt = new UpgradeTechTypes(
                        forModVehicle: RegisterModVehicleUpgrade(upgrade)
                    );
                    isPdaRegistered = true;
                }
                RegisterUpgradeMethods(upgrade, compat, ref utt, isPdaRegistered);
                upgrade.TechTypes = utt;
                return utt;
            }
            else
            {
                Logger.Error("Failed to register upgrade: " + upgrade.ClassId);
                return default;
            }
        }

        /// <summary>
        /// Validates the provided ModVehicleUpgrade and its compatibility settings.
        /// </summary>
        /// <param name="upgrade">The upgrade to validate.</param>
        /// <param name="compat">Compatibility flags.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private static bool ValidateModVehicleUpgrade(ModVehicleUpgrade upgrade, UpgradeCompat compat)
        {
            if (compat.skipModVehicle && compat.skipSeamoth && compat.skipExosuit && compat.skipCyclops)
            {
                Logger.Error($"UpgradeRegistrar Error: ModVehicleUpgrade {upgrade.ClassId}: compat cannot skip all vehicle types!");
                return false;
            }
            if (upgrade.ClassId.Equals(string.Empty))
            {
                Logger.Error($"UpgradeRegistrar Error: ModVehicleUpgrade {upgrade.ClassId} cannot have empty class ID!");
                return false;
            }
            if (upgrade.GetRecipe(VehicleType.ModVehicle).IsEmpty)
            {
                Logger.Error($"UpgradeRegistrar Error: ModVehicleUpgrade {upgrade.ClassId} cannot have empty recipe!");
                return false;
            }
            if (!upgrade.UnlockAtStart)
            {
                if (!upgrade.UnlockedSprite && !upgrade.UnlockedMessage.Equals(ModVehicleUpgrade.DefaultUnlockMessage))
                {
                    Logger.Warn($"UpgradeRegistrar Warning: the upgrade {upgrade.ClassId} has UnlockAtStart false and UnlockedSprite null. When unlocked, its custom UnlockedMessage will not be displayed. Add an UnlockedSprite to resolve this.");
                }
            }
            return true;
        }

        /// <summary>
        /// Registers the ModVehicleUpgrade for ModVehicle, sets up its prefab, recipe, and unlock conditions.
        /// </summary>
        /// <param name="upgrade">The upgrade to register.</param>
        /// <returns>The TechType assigned to the upgrade.</returns>
        private static TechType RegisterModVehicleUpgrade(ModVehicleUpgrade upgrade)
        {
            Nautilus.Crafting.RecipeData moduleRecipe = upgrade.GetRecipe(VehicleType.ModVehicle).ToRecipeData();
            Nautilus.Assets.PrefabInfo module_info = Nautilus.Assets.PrefabInfo
                .WithTechType(upgrade.ClassId, upgrade.DisplayName, upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            Nautilus.Assets.CustomPrefab module_CustomPrefab = new Nautilus.Assets.CustomPrefab(module_info);
            Nautilus.Assets.PrefabTemplates.PrefabTemplate moduleTemplate = new Nautilus.Assets.PrefabTemplates.CloneTemplate(module_info, TechType.SeamothElectricalDefense)
            {
                ModifyPrefab = prefab => prefab.GetComponentsInChildren<Renderer>().ForEach(r => r.materials.ForEach(m => m.color = upgrade.Color))
            };
            module_CustomPrefab.SetGameObject(moduleTemplate);

            string[] steps;
            if (upgrade.IsVehicleSpecific)
            {
                steps = upgrade.ResolvePath(VehicleType.Custom);
            }
            else
            {
                steps = upgrade.ResolvePath(VehicleType.ModVehicle);
            }
            if (!CraftTreeHandler.IsValidCraftPath(steps))
            {
                throw new Exception($"UpgradeRegistrar: Invalid Crafting Path: there were tab nodes in that tab: {steps.Last()}. Cannot mix tab nodes and crafting nodes.");
            }
            CraftTreeHandler.CraftNodeTabNodes.Add(steps.Last());
            module_CustomPrefab
                .SetRecipe(moduleRecipe)
                .WithCraftingTime(upgrade.CraftingTime)
                .WithFabricatorType(Assets.AVSFabricator.TreeType)
                .WithStepsToFabricatorTab(steps);
            module_CustomPrefab.SetPdaGroupCategory(TechGroup.VehicleUpgrades, TechCategory.VehicleUpgrades);
            module_CustomPrefab
                .SetEquipment(VehicleBuilder.ModuleType)
                .WithQuickSlotType(upgrade.QuickSlotType);
            if (!upgrade.UnlockAtStart)
            {
                var scanningGadget = module_CustomPrefab.SetUnlock(upgrade.UnlockTechType == TechType.Fragment ? upgrade.UnlockWith : upgrade.UnlockTechType);
                if (upgrade.UnlockedSprite != null)
                {
                    scanningGadget.WithAnalysisTech(upgrade.UnlockedSprite, unlockMessage: upgrade.UnlockedMessage);
                }
            }
            module_CustomPrefab.Register(); // this line causes PDA voice lag by 1.5 seconds ???????
            upgrade.UnlockTechType = module_info.TechType;
            return module_info.TechType;
        }

        /// <summary>
        /// Registers the appropriate upgrade methods (passive, selectable, chargeable, toggleable) for the upgrade.
        /// </summary>
        /// <param name="upgrade">The upgrade to register methods for.</param>
        /// <param name="compat">Compatibility flags.</param>
        /// <param name="utt">Reference to UpgradeTechTypes to update.</param>
        /// <param name="isPDASetup">Indicates if PDA registration has occurred.</param>
        private static void RegisterUpgradeMethods(ModVehicleUpgrade upgrade, UpgradeCompat compat, ref UpgradeTechTypes utt, bool isPDASetup)
        {
            bool isPDASetupLocal = isPDASetup;
            RegisterPassiveUpgradeActions(upgrade, compat, ref utt, ref isPDASetupLocal);
            RegisterSelectableUpgradeActions(upgrade, compat, ref utt, ref isPDASetupLocal);
            RegisterSelectableChargeableUpgradeActions(upgrade, compat, ref utt, ref isPDASetupLocal);
            RegisterToggleableUpgradeActions(upgrade, compat, ref utt, ref isPDASetupLocal);
        }

        /// <summary>
        /// Registers passive upgrade actions for the upgrade.
        /// </summary>
        /// <param name="upgrade">The upgrade to register.</param>
        /// <param name="compat">Compatibility flags.</param>
        /// <param name="utt">Reference to UpgradeTechTypes to update.</param>
        /// <param name="isPDASetup">Indicates if PDA registration has occurred.</param>
        private static void RegisterPassiveUpgradeActions(ModVehicleUpgrade upgrade, UpgradeCompat compat, ref UpgradeTechTypes utt, ref bool isPDASetup)
        {
            if (upgrade is SelectableUpgrade
                || upgrade is ToggleableUpgrade
                || upgrade is SelectableChargeableUpgrade
                )
            {

            }
            else
            {
                VanillaUpgradeMaker.CreatePassiveModule(upgrade, compat, ref utt, isPDASetup);
                isPDASetup = true;
            }
            TechType mvTT = utt.ForModVehicle;
            TechType sTT = utt.ForSeamoth;
            TechType eTT = utt.ForExosuit;
            TechType cTT = utt.ForCyclops;
            void WrappedOnAdded(AddActionParams param)
            {
                if (param.techType != TechType.None && (param.techType == mvTT || param.techType == sTT || param.techType == eTT || param.techType == cTT))
                {
                    if (param.vehicle != null)
                    {
                        if (param.isAdded)
                        {
                            upgrade.OnAdded(param);
                        }
                        else
                        {
                            upgrade.OnRemoved(param);
                        }

                    }
                    else if (param.cyclops != null)
                    {
                        upgrade.OnCyclops(param);
                    }
                }
            }
            OnAddActions.Add(WrappedOnAdded);
        }

        /// <summary>
        /// Registers selectable upgrade actions for the upgrade.
        /// </summary>
        /// <param name="upgrade">The upgrade to register.</param>
        /// <param name="compat">Compatibility flags.</param>
        /// <param name="utt">Reference to UpgradeTechTypes to update.</param>
        /// <param name="isPDASetup">Indicates if PDA registration has occurred.</param>
        private static void RegisterSelectableUpgradeActions(ModVehicleUpgrade upgrade, UpgradeCompat compat, ref UpgradeTechTypes utt, ref bool isPDASetup)
        {
            if (upgrade is SelectableUpgrade select)
            {
                VanillaUpgradeMaker.CreateSelectModule(select, compat, ref utt, isPDASetup);
                isPDASetup = true;
                TechType mvTT = utt.ForModVehicle;
                TechType sTT = utt.ForSeamoth;
                TechType eTT = utt.ForExosuit;
                TechType cTT = utt.ForCyclops;
                void WrappedOnSelected(SelectableActionParams param)
                {
                    if (param.techType != TechType.None && (param.techType == mvTT || param.techType == sTT || param.techType == eTT || param.techType == cTT))
                    {
                        select.OnSelected(param);
                        param.vehicle.quickSlotTimeUsed[param.slotID] = Time.time;
                        param.vehicle.quickSlotCooldown[param.slotID] = select.Cooldown;
                        param.vehicle.energyInterface.ConsumeEnergy(select.EnergyCost);
                    }
                }
                OnSelectActions.Add(WrappedOnSelected);
            }
        }

        /// <summary>
        /// Registers selectable chargeable upgrade actions for the upgrade.
        /// </summary>
        /// <param name="upgrade">The upgrade to register.</param>
        /// <param name="compat">Compatibility flags.</param>
        /// <param name="utt">Reference to UpgradeTechTypes to update.</param>
        /// <param name="isPDASetup">Indicates if PDA registration has occurred.</param>
        private static void RegisterSelectableChargeableUpgradeActions(ModVehicleUpgrade upgrade, UpgradeCompat compat, ref UpgradeTechTypes utt, ref bool isPDASetup)
        {
            if (upgrade is SelectableChargeableUpgrade selectcharge)
            {
                VanillaUpgradeMaker.CreateChargeModule(selectcharge, compat, ref utt, isPDASetup);
                foreach (System.Reflection.FieldInfo field in typeof(UpgradeTechTypes).GetFields())
                {
                    // Set MaxCharge and EnergyCost for all possible TechTypes emerging from this upgrade.
                    TechType value = (TechType)field.GetValue(utt);
                    Logger.Log(value.AsString());
                    Nautilus.Handlers.CraftDataHandler.SetMaxCharge(value, selectcharge.MaxCharge);
                    Nautilus.Handlers.CraftDataHandler.SetEnergyCost(value, selectcharge.EnergyCost);
                }
                isPDASetup = true;
                TechType mvTT = utt.ForModVehicle;
                TechType sTT = utt.ForSeamoth;
                TechType eTT = utt.ForExosuit;
                TechType cTT = utt.ForCyclops;
                void WrappedOnSelectedCharged(SelectableChargeableActionParams param)
                {
                    if (param.techType != TechType.None && (param.techType == mvTT || param.techType == sTT || param.techType == eTT || param.techType == cTT))
                    {
                        selectcharge.OnSelected(param);
                        param.vehicle.energyInterface.ConsumeEnergy(selectcharge.EnergyCost);
                    }
                }
                OnSelectChargeActions.Add(WrappedOnSelectedCharged);
            }
        }

        /// <summary>
        /// Registers toggleable upgrade actions for the upgrade.
        /// </summary>
        /// <param name="upgrade">The upgrade to register.</param>
        /// <param name="compat">Compatibility flags.</param>
        /// <param name="utt">Reference to UpgradeTechTypes to update.</param>
        /// <param name="isPDASetup">Indicates if PDA registration has occurred.</param>
        private static void RegisterToggleableUpgradeActions(ModVehicleUpgrade upgrade, UpgradeCompat compat, ref UpgradeTechTypes utt, ref bool isPDASetup)
        {
            if (upgrade is ToggleableUpgrade toggle)
            {
                IEnumerator DoToggleAction(ToggleActionParams param, float timeToFirstActivation, float repeatRate, float energyCostPerActivation)
                {
                    bool isModVehicle = param.vehicle.GetComponent<ModVehicle>() != null;
                    yield return new WaitForSeconds(timeToFirstActivation);
                    while (true)
                    {
                        bool shouldStopWorking = isModVehicle ? !param.vehicle.GetComponent<ModVehicle>().IsUnderCommand : !param.vehicle.GetPilotingMode();
                        if (shouldStopWorking)
                        {
                            param.vehicle.ToggleSlot(param.slotID, false);
                            yield break;
                        }
                        toggle.OnRepeat(param);
                        float availablePower = param.vehicle.energyInterface.TotalCanProvide(out _);
                        if (availablePower < energyCostPerActivation)
                        {
                            param.vehicle.ToggleSlot(param.slotID, false);
                            yield break;
                        }
                        param.vehicle.energyInterface.ConsumeEnergy(energyCostPerActivation);
                        yield return new WaitForSeconds(repeatRate);
                    }
                }
                VanillaUpgradeMaker.CreateToggleModule(toggle, compat, ref utt, isPDASetup);
                isPDASetup = true;
                TechType mvTT = utt.ForModVehicle;
                TechType sTT = utt.ForSeamoth;
                TechType eTT = utt.ForExosuit;
                TechType cTT = utt.ForCyclops;
                void WrappedOnToggle(ToggleActionParams param)
                {
                    toggledActions.RemoveAll(x => x.Item3 == null);
                    if (param.techType != TechType.None && (param.techType == mvTT || param.techType == sTT || param.techType == eTT || param.techType == cTT))
                    {
                        var relevantActions = toggledActions.Where(x => x.Item1 == param.vehicle).Where(x => x.Item2 == param.slotID);
                        if (param.active)
                        {
                            if (relevantActions.Any())
                            {
                                // Something triggers my Nautilus WithOnModuleToggled action doubly for the Seamoth.
                                // So if the toggle action already exists, don't add another one.
                                return;
                            }
                            var thisToggleCoroutine = param.vehicle.StartCoroutine(DoToggleAction(param, toggle.TimeToFirstActivation, toggle.RepeatRate, toggle.EnergyCostPerActivation));
                            toggledActions.Add(new Tuple<Vehicle, int, Coroutine>(param.vehicle, param.slotID, thisToggleCoroutine));
                        }
                        else
                        {
                            foreach (var innerAction in relevantActions)
                            {
                                toggledActions.RemoveAll(x => x == innerAction);
                                param.vehicle.StopCoroutine(innerAction.Item3);
                            }
                        }
                    }
                }
                OnToggleActions.Add(WrappedOnToggle);
            }
        }
    }
}

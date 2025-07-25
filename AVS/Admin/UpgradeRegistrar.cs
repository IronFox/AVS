﻿using AVS.Assets;
using AVS.BaseVehicle;
using AVS.Log;
using AVS.UpgradeTypes;
using AVS.Util;
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
    public readonly struct UpgradeCompat
    {
        /// <summary>
        /// If true, skip registering for <see cref="AvsVehicle" />.
        /// </summary>
        public bool SkipAvsVehicle { get; }
        /// <summary>
        /// If true, skip registering for Seamoth.
        /// </summary>
        public bool SkipSeamoth { get; }
        /// <summary>
        /// If true, skip registering for Exosuit.
        /// </summary>
        public bool SkipExosuit { get; }
        /// <summary>
        /// If true, skip registering for Cyclops.
        /// </summary>
        public bool SkipCyclops { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpgradeCompat"/> class with options to skip certain vehicle
        /// upgrades.
        /// </summary>
        /// <param name="skipAvsVehicle">If <see langword="true"/>, the AVS vehicle upgrade will be skipped; otherwise, it will be included. Defaults
        /// to <see langword="false"/>.</param>
        /// <param name="skipSeamoth">If <see langword="true"/>, the Seamoth upgrade will be skipped; otherwise, it will be included. Defaults to
        /// <see langword="false"/>.</param>
        /// <param name="skipExosuit">If <see langword="true"/>, the Exosuit upgrade will be skipped; otherwise, it will be included. Defaults to
        /// <see langword="false"/>.</param>
        /// <param name="skipCyclops">If <see langword="true"/>, the Cyclops upgrade will be skipped; otherwise, it will be included. Defaults to
        /// <see langword="false"/>.</param>
        public UpgradeCompat(
            bool skipAvsVehicle = true,
            bool skipSeamoth = true,
            bool skipExosuit = true,
            bool skipCyclops = true)
        {
            SkipAvsVehicle = skipAvsVehicle;
            SkipSeamoth = skipSeamoth;
            SkipExosuit = skipExosuit;
            SkipCyclops = skipCyclops;
        }

        /// <summary>
        /// Default compatibility settings for registering upgrades to be applicable only to
        /// AVS vehicles.
        /// </summary>
        public static UpgradeCompat AvsVehiclesOnly { get; } = new UpgradeCompat(
            skipAvsVehicle: false
        );
    }
    /// <summary>
    /// Holds TechTypes for an upgrade for each supported vehicle type.
    /// </summary>
    public readonly struct UpgradeTechTypes
    {
        /// <summary>
        /// Tech type applicable for any AVS vehicle.
        /// </summary>
        public TechType ForAvsVehicle { get; }
        /// <summary>
        /// Tech type applicable for Seamoth vehicles.
        /// </summary>
        public TechType ForSeamoth { get; }
        /// <summary>
        /// Tech type applicable for PRAWN exosuits.
        /// </summary>
        public TechType ForExosuit { get; }
        /// <summary>
        /// Tech type applicable for Cyclops vehicles.
        /// </summary>
        public TechType ForCyclops { get; }

        /// <summary>
        /// Constructor for UpgradeTechTypes.
        /// </summary>
        internal UpgradeTechTypes(TechType forAvsVehicle = TechType.None, TechType forSeamoth = TechType.None, TechType forExosuit = TechType.None, TechType forCyclops = TechType.None)
        {
            ForAvsVehicle = forAvsVehicle;
            ForSeamoth = forSeamoth;
            ForExosuit = forExosuit;
            ForCyclops = forCyclops;
        }
        /// <summary>
        /// Determines whether the specified <see cref="TechType"/> is not <see cref="TechType.None" />
        /// and equal to any of the contained types.
        /// </summary>
        public bool HasTechType(TechType techType)
        {
            if (techType == TechType.None)
                return false;
            return ForAvsVehicle == techType
                || ForSeamoth == techType
                || ForExosuit == techType
                || ForCyclops == techType;
        }

        internal UpgradeTechTypes ReplaceSeamoth(TechType techType)
        {
            return new UpgradeTechTypes(
                forAvsVehicle: ForAvsVehicle,
                forSeamoth: techType,
                forExosuit: ForExosuit,
                forCyclops: ForCyclops
            );
        }

        internal UpgradeTechTypes ReplaceExosuit(TechType techType)
        {
            return new UpgradeTechTypes(
                forAvsVehicle: ForAvsVehicle,
                forSeamoth: ForSeamoth,
                forExosuit: techType,
                forCyclops: ForCyclops
            );
        }

        internal UpgradeTechTypes ReplaceCyclops(TechType techType)
        {
            return new UpgradeTechTypes(
                forAvsVehicle: ForAvsVehicle,
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
        AvsVehicle,
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
        public static Dictionary<string, Sprite> UpgradeIcons { get; } = new Dictionary<string, Sprite>();

        /// <summary>
        /// List of actions to invoke when an upgrade is added or removed.
        /// </summary>
        internal static List<Action<AddActionParams>> OnAddActions { get; } = new List<Action<AddActionParams>>();
        /// <summary>
        /// List of actions to invoke when a toggleable upgrade is toggled.
        /// </summary>
        internal static List<Action<ToggleActionParams>> OnToggleActions { get; } = new List<Action<ToggleActionParams>>();
        /// <summary>
        /// List of actions to invoke when a selectable chargeable upgrade is selected.
        /// </summary>
        internal static List<Action<SelectableChargeableActionParams>> OnSelectChargeActions { get; } = new List<Action<SelectableChargeableActionParams>>();
        /// <summary>
        /// List of actions to invoke when a selectable upgrade is selected.
        /// </summary>
        internal static List<Action<SelectableActionParams>> OnSelectActions { get; } = new List<Action<SelectableActionParams>>();
        /// <summary>
        /// List of actions to invoke when an arm action is performed (down).
        /// </summary>
        internal static List<Action<ArmActionParams>> OnArmDownActions { get; } = new List<Action<ArmActionParams>>();
        /// <summary>
        /// List of actions to invoke when an arm action is held.
        /// </summary>
        internal static List<Action<ArmActionParams>> OnArmHeldActions { get; } = new List<Action<ArmActionParams>>();
        /// <summary>
        /// List of actions to invoke when an arm action is released (up).
        /// </summary>
        internal static List<Action<ArmActionParams>> OnArmUpActions { get; } = new List<Action<ArmActionParams>>();
        /// <summary>
        /// List of actions to invoke when an alternate arm action is performed.
        /// </summary>
        internal static List<Action<ArmActionParams>> OnArmAltActions { get; } = new List<Action<ArmActionParams>>();
        /// <summary>
        /// Tracks currently toggled actions for vehicles, by vehicle, slot, and coroutine.
        /// </summary>
        internal static List<Tuple<Vehicle, int, Coroutine>> ToggledActions { get; } = new List<Tuple<Vehicle, int, Coroutine>>();

        /// <summary>
        /// Registers a AvsVehicleUpgrade and sets up its icons, recipes, and actions for compatible vehicle types.
        /// </summary>
        /// <param name="upgrade">The upgrade to register.</param>
        /// <param name="compat">Compatibility flags for vehicle types.</param>
        /// <param name="verbose">If true, enables verbose logging.</param>
        /// <returns>UpgradeTechTypes containing TechTypes for each vehicle type.</returns>
        public static UpgradeTechTypes RegisterUpgrade(AvsVehicleUpgrade upgrade, UpgradeCompat compat = default(UpgradeCompat), bool verbose = false)
        {
            LogWriter.Default.Write($"Registering {nameof(AvsVehicleUpgrade)} " + upgrade.ClassId + " : " + upgrade.DisplayName);
            bool result = ValidateAvsVehicleUpgrade(upgrade, compat);
            if (result)
            {
                CraftTreeHandler.EnsureCraftingTabsAvailable(upgrade, compat);
                var icon = SpriteHelper.CreateSpriteFromAtlasSprite(upgrade.Icon);
                if (icon != null)
                    UpgradeIcons.Add(upgrade.ClassId, icon);
                else
                    LogWriter.Default.Error($"UpgradeRegistrar Error: {nameof(AvsVehicleUpgrade)} {upgrade.ClassId} has a null icon! Please provide a valid icon sprite.");
                UpgradeTechTypes utt = new UpgradeTechTypes();
                bool isPdaRegistered = false;
                if (!compat.SkipAvsVehicle || upgrade.IsVehicleSpecific)
                {
                    utt = new UpgradeTechTypes(
                        forAvsVehicle: RegisterAvsVehicleUpgrade(upgrade)
                    );
                    isPdaRegistered = true;
                }
                RegisterUpgradeMethods(upgrade, compat, ref utt, isPdaRegistered);
                upgrade.TechTypes = utt;
                return utt;
            }
            else
            {
                LogWriter.Default.Error("Failed to register upgrade: " + upgrade.ClassId);
                return default;
            }
        }

        /// <summary>
        /// Validates the provided <see cref="AvsVehicleUpgrade"/> and its compatibility settings.
        /// </summary>
        /// <param name="upgrade">The upgrade to validate.</param>
        /// <param name="compat">Compatibility flags.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private static bool ValidateAvsVehicleUpgrade(AvsVehicleUpgrade upgrade, UpgradeCompat compat)
        {
            if (compat.SkipAvsVehicle && compat.SkipSeamoth && compat.SkipExosuit && compat.SkipCyclops)
            {
                Logger.Error($"UpgradeRegistrar Error: {nameof(AvsVehicleUpgrade)} {upgrade.ClassId}: compat cannot skip all vehicle types!");
                return false;
            }
            if (upgrade.ClassId.Equals(string.Empty))
            {
                Logger.Error($"UpgradeRegistrar Error: {nameof(AvsVehicleUpgrade)} {upgrade.ClassId} cannot have empty class ID!");
                return false;
            }
            if (upgrade.GetRecipe(VehicleType.AvsVehicle).IsEmpty)
            {
                Logger.Error($"UpgradeRegistrar Error: {nameof(AvsVehicleUpgrade)} {upgrade.ClassId} cannot have empty recipe!");
                return false;
            }
            if (!upgrade.UnlockAtStart)
            {
                if (!upgrade.UnlockedSprite && !upgrade.UnlockedMessage.Equals(AvsVehicleUpgrade.DefaultUnlockMessage))
                {
                    Logger.Warn($"UpgradeRegistrar Warning: the upgrade {upgrade.ClassId} has UnlockAtStart false and UnlockedSprite null. When unlocked, its custom UnlockedMessage will not be displayed. Add an UnlockedSprite to resolve this.");
                }
            }
            return true;
        }

        /// <summary>
        /// Registers the <see cref="AvsVehicleUpgrade"/> for <see cref="AvsVehicle"/>, sets up its prefab, recipe, and unlock conditions.
        /// </summary>
        /// <param name="upgrade">The upgrade to register.</param>
        /// <returns>The TechType assigned to the upgrade.</returns>
        private static TechType RegisterAvsVehicleUpgrade(AvsVehicleUpgrade upgrade)
        {
            Nautilus.Crafting.RecipeData moduleRecipe = upgrade.GetRecipe(VehicleType.AvsVehicle).ToRecipeData();
            Nautilus.Assets.PrefabInfo module_info = Nautilus.Assets.PrefabInfo
                .WithTechType(upgrade.ClassId, upgrade.DisplayName, upgrade.Description, unlockAtStart: upgrade.UnlockAtStart)
                .WithIcon(upgrade.Icon);
            Nautilus.Assets.CustomPrefab module_CustomPrefab = new Nautilus.Assets.CustomPrefab(module_info);
            Nautilus.Assets.PrefabTemplates.PrefabTemplate moduleTemplate = new Nautilus.Assets.PrefabTemplates.CloneTemplate(module_info, TechType.SeamothElectricalDefense)
            {
                ModifyPrefab = prefab => prefab.GetComponentsInChildren<Renderer>().ForEach(r => r.materials.ForEach(m => m.color = upgrade.Color))
            };
            module_CustomPrefab.SetGameObject(moduleTemplate);

            IReadOnlyList<string> steps;
            if (upgrade.IsVehicleSpecific)
            {
                steps = upgrade.ResolvePath(VehicleType.Custom);
            }
            else
            {
                steps = upgrade.ResolvePath(VehicleType.AvsVehicle);
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
                .WithStepsToFabricatorTab(steps.ToArray());
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
        private static void RegisterUpgradeMethods(AvsVehicleUpgrade upgrade, UpgradeCompat compat, ref UpgradeTechTypes utt, bool isPDASetup)
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
        private static void RegisterPassiveUpgradeActions(AvsVehicleUpgrade upgrade, UpgradeCompat compat, ref UpgradeTechTypes utt, ref bool isPDASetup)
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
            TechType mvTT = utt.ForAvsVehicle;
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
        private static void RegisterSelectableUpgradeActions(AvsVehicleUpgrade upgrade, UpgradeCompat compat, ref UpgradeTechTypes utt, ref bool isPDASetup)
        {
            if (upgrade is SelectableUpgrade select)
            {
                VanillaUpgradeMaker.CreateSelectModule(select, compat, ref utt, isPDASetup);
                isPDASetup = true;
                TechType mvTT = utt.ForAvsVehicle;
                TechType sTT = utt.ForSeamoth;
                TechType eTT = utt.ForExosuit;
                TechType cTT = utt.ForCyclops;
                void WrappedOnSelected(SelectableActionParams param)
                {
                    if (param.TechType != TechType.None && (param.TechType == mvTT || param.TechType == sTT || param.TechType == eTT || param.TechType == cTT))
                    {
                        select.OnSelected(param);
                        param.Vehicle.quickSlotTimeUsed[param.SlotID] = Time.time;
                        param.Vehicle.quickSlotCooldown[param.SlotID] = select.Cooldown;
                        param.Vehicle.energyInterface.ConsumeEnergy(select.EnergyCost);
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
        private static void RegisterSelectableChargeableUpgradeActions(AvsVehicleUpgrade upgrade, UpgradeCompat compat, ref UpgradeTechTypes utt, ref bool isPDASetup)
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
                var myType = utt;
                void WrappedOnSelectedCharged(SelectableChargeableActionParams param)
                {
                    if (myType.HasTechType(param.TechType))
                    {
                        selectcharge.OnSelected(param);
                        param.Vehicle.energyInterface.ConsumeEnergy(selectcharge.EnergyCost);
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
        private static void RegisterToggleableUpgradeActions(AvsVehicleUpgrade upgrade, UpgradeCompat compat, ref UpgradeTechTypes utt, ref bool isPDASetup)
        {
            if (upgrade is ToggleableUpgrade toggle)
            {
                IEnumerator DoToggleAction(ToggleActionParams param, float timeToFirstActivation, float repeatRate, float energyCostPerActivation)
                {
                    var isAvsVehicle = param.vehicle.GetComponent<AvsVehicle>();
                    yield return new WaitForSeconds(timeToFirstActivation);
                    while (true)
                    {
                        bool shouldStopWorking = isAvsVehicle ? !isAvsVehicle.IsBoarded : !param.vehicle.GetPilotingMode();
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
                TechType mvTT = utt.ForAvsVehicle;
                TechType sTT = utt.ForSeamoth;
                TechType eTT = utt.ForExosuit;
                TechType cTT = utt.ForCyclops;
                void WrappedOnToggle(ToggleActionParams param)
                {
                    ToggledActions.RemoveAll(x => x.Item3 == null);
                    if (param.techType != TechType.None && (param.techType == mvTT || param.techType == sTT || param.techType == eTT || param.techType == cTT))
                    {
                        var relevantActions = ToggledActions.Where(x => x.Item1 == param.vehicle).Where(x => x.Item2 == param.slotID);
                        if (param.active)
                        {
                            if (relevantActions.Any())
                            {
                                // Something triggers my Nautilus WithOnModuleToggled action doubly for the Seamoth.
                                // So if the toggle action already exists, don't add another one.
                                return;
                            }
                            var thisToggleCoroutine = param.vehicle.StartCoroutine(DoToggleAction(param, toggle.TimeToFirstActivation, toggle.RepeatRate, toggle.EnergyCostPerActivation));
                            ToggledActions.Add(new Tuple<Vehicle, int, Coroutine>(param.vehicle, param.slotID, thisToggleCoroutine));
                        }
                        else
                        {
                            foreach (var innerAction in relevantActions)
                            {
                                ToggledActions.RemoveAll(x => x == innerAction);
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

using AVS.Assets;
using AVS.BaseVehicle;
using AVS.Log;
using AVS.UpgradeModules;
using Nautilus.Assets.Gadgets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.Crafting
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
    public readonly struct UpgradeTechTypes : IEquatable<UpgradeTechTypes>
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
        /// Gets an enumerable collection of <see cref="TechType"/> values that are not <see cref="TechType.None"/>.
        /// </summary>
        public IEnumerable<TechType> AllNotNone
        {
            get
            {
                if (ForAvsVehicle != TechType.None)
                    yield return ForAvsVehicle;
                if (ForSeamoth != TechType.None)
                    yield return ForSeamoth;
                if (ForExosuit != TechType.None)
                    yield return ForExosuit;
                if (ForCyclops != TechType.None)
                    yield return ForCyclops;
            }
        }

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

        internal bool GetAnyNotNone(out TechType tt)
        {
            if (ForAvsVehicle != TechType.None)
            {
                tt = ForAvsVehicle;
                return true;
            }
            if (ForSeamoth != TechType.None)
            {
                tt = ForSeamoth;
                return true;
            }
            if (ForExosuit != TechType.None)
            {
                tt = ForExosuit;
                return true;
            }
            if (ForCyclops != TechType.None)
            {
                tt = ForCyclops;
                return true;
            }
            tt = TechType.None;
            return false;
        }

        internal UpgradeTechTypes ReplaceVehicleType(VehicleType vType, TechType techType, bool failIfCustom = true)
        {
            switch (vType)
            {
                case VehicleType.AvsVehicle:
                    return new UpgradeTechTypes(
                        forAvsVehicle: techType,
                        forSeamoth: ForSeamoth,
                        forExosuit: ForExosuit,
                        forCyclops: ForCyclops
                    );
                case VehicleType.Seamoth:
                    return ReplaceSeamoth(techType);
                case VehicleType.Prawn:
                    return ReplaceExosuit(techType);
                case VehicleType.Cyclops:
                    return ReplaceCyclops(techType);
                default:
                    if (failIfCustom)
                        throw new ArgumentOutOfRangeException(nameof(vType), vType, "Unsupported VehicleType for UpgradeTechTypes replacement.");
                    else
                        return this;
            }
        }

        /// <inheritdoc/>
        public bool Equals(UpgradeTechTypes other)
            => ForAvsVehicle == other.ForAvsVehicle
            && ForSeamoth == other.ForSeamoth
            && ForExosuit == other.ForExosuit
            && ForCyclops == other.ForCyclops;

        /// <summary>
        /// Counts the total number of upgrades locally registered
        /// in the provided <see cref="Equipment"/> instance.
        /// </summary>
        /// <param name="modules">Modules to count instances in</param>
        /// <returns>The total number of modules installed of the local type</returns>
        public int CountSumIn(Equipment modules)
        {
            int sum = 0;
            foreach (var techType in AllNotNone)
            {
                sum += modules.GetCount(techType);
            }
            return sum;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + ForAvsVehicle.GetHashCode();
            hash = hash * 23 + ForSeamoth.GetHashCode();
            hash = hash * 23 + ForExosuit.GetHashCode();
            hash = hash * 23 + ForCyclops.GetHashCode();
            return hash;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is UpgradeTechTypes other && Equals(other);
        }


        /// <inheritdoc/>
        public static bool operator ==(UpgradeTechTypes left, UpgradeTechTypes right)
        {
            return left.Equals(right);
        }
        /// <inheritdoc/>
        public static bool operator !=(UpgradeTechTypes left, UpgradeTechTypes right)
        {
            return !(left == right);
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
        private static Dictionary<VehicleSlotId, ActiveAction> ToggledActions { get; } = new Dictionary<VehicleSlotId, ActiveAction>();


        internal readonly struct ActiveAction
        {
            public Coroutine Action { get; }
            public ToggleableUpgrade? ToggleableUpgrade { get; }
            public Vehicle Vehicle { get; }

            public ActiveAction(Coroutine action, Vehicle vehicle, ToggleableUpgrade? toggleableUpgrade)
            {
                Action = action;
                ToggleableUpgrade = toggleableUpgrade;
                Vehicle = vehicle;
            }

            public bool IsValid => Action != null && Vehicle != null;

            public void Stop(ToggleActionParams inactiveParams)
            {
                if (Action != null && Vehicle != null)
                {
                    Vehicle.StopCoroutine(Action);
                    if (ToggleableUpgrade != null)
                        try
                        {
                            ToggleableUpgrade.OnToggleInternal(inactiveParams);
                        }
                        catch (Exception e)
                        {
                            LogWriter.Default.Error($"Error in {ToggleableUpgrade.ClassId} OnToggle: {e.Message}", e);
                        }
                }
            }
        }

        internal readonly struct VehicleSlotId : IEquatable<VehicleSlotId>
        {

            public Vehicle Vehicle { get; }
            public int SlotID { get; }
            public VehicleSlotId(Vehicle vehicle, int slotID)
            {
                Vehicle = vehicle;
                SlotID = slotID;
            }

            public bool Equals(VehicleSlotId other)
            {
                return Vehicle == other.Vehicle && SlotID == other.SlotID;
            }
            public override bool Equals(object? obj)
            {
                return obj is VehicleSlotId other && Equals(other);
            }
            public override int GetHashCode()
            {
                int hash = 17;
                hash = hash * 23 + Vehicle.GetHashCode();
                hash = hash * 23 + SlotID.GetHashCode();
                return hash;
            }
            public static bool operator ==(VehicleSlotId left, VehicleSlotId right)
            {
                return left.Equals(right);
            }
            public static bool operator !=(VehicleSlotId left, VehicleSlotId right)
            {
                return !(left == right);
            }
        }


        /// <summary>
        /// Registers a AvsVehicleUpgrade and sets up its icons, recipes, and actions for compatible vehicle types.
        /// </summary>
        /// <param name="node">The folder containing the upgrade assets.</param>
        /// <param name="upgrade">The upgrade to register.</param>
        /// <param name="compat">Compatibility flags for vehicle types.</param>
        /// <returns>UpgradeTechTypes containing TechTypes for each vehicle type.</returns>
        internal static UpgradeTechTypes RegisterUpgrade(Node node, AvsVehicleModule upgrade, UpgradeCompat compat = default)
        {
            LogWriter.Default.Write($"Registering {nameof(AvsVehicleModule)} " + upgrade.ClassId + " : " + upgrade.DisplayName);
            bool result = ValidateAvsVehicleUpgrade(upgrade, compat);
            if (result)
            {
                if (node.Children.Count > 0)
                    throw new InvalidOperationException($"CraftTreeHandler: Cannot add an upgrade to a folder that already contains folders. Folder: {node.GetPath()}");

                var icon = SpriteHelper.CreateSpriteFromAtlasSprite(upgrade.Icon);
                if (icon != null)
                    UpgradeIcons.Add(upgrade.ClassId, icon);
                else
                    LogWriter.Default.Error($"UpgradeRegistrar Error: {nameof(AvsVehicleModule)} {upgrade.ClassId} has a null icon! Please provide a valid icon sprite.");
                UpgradeTechTypes utt = new UpgradeTechTypes();
                bool isPdaRegistered = false;
                if (!compat.SkipAvsVehicle)
                {
                    utt = new UpgradeTechTypes(
                        forAvsVehicle: RegisterAvsVehicleModule(node, upgrade)
                    );
                    isPdaRegistered = true;
                }
                RegisterUpgradeMethods(upgrade, compat, ref utt, isPdaRegistered);
                upgrade.TechTypes = utt;
                node.Modules.Add(upgrade);
                return utt;
            }
            else
            {
                LogWriter.Default.Error("Failed to register upgrade: " + upgrade.ClassId);
                return default;
            }
        }

        /// <summary>
        /// Validates the provided <see cref="AvsVehicleModule"/> and its compatibility settings.
        /// </summary>
        /// <param name="upgrade">The upgrade to validate.</param>
        /// <param name="compat">Compatibility flags.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private static bool ValidateAvsVehicleUpgrade(AvsVehicleModule upgrade, UpgradeCompat compat)
        {
            if (compat.SkipAvsVehicle && compat.SkipSeamoth && compat.SkipExosuit && compat.SkipCyclops)
            {
                LogWriter.Default.Error($"UpgradeRegistrar Error: {nameof(AvsVehicleModule)} {upgrade.ClassId}: compat cannot skip all vehicle types!");
                return false;
            }
            if (upgrade.ClassId.Equals(string.Empty))
            {
                LogWriter.Default.Error($"UpgradeRegistrar Error: {nameof(AvsVehicleModule)} {upgrade.ClassId} cannot have empty class ID!");
                return false;
            }
            if (upgrade.GetRecipe(VehicleType.AvsVehicle).IsEmpty)
            {
                LogWriter.Default.Error($"UpgradeRegistrar Error: {nameof(AvsVehicleModule)} {upgrade.ClassId} cannot have empty recipe!");
                return false;
            }
            if (!upgrade.UnlockAtStart)
            {
                if (!upgrade.UnlockedSprite && !upgrade.UnlockedMessage.Equals(AvsVehicleModule.DefaultUnlockMessage))
                {
                    LogWriter.Default.Warn($"UpgradeRegistrar Warning: the upgrade {upgrade.ClassId} has UnlockAtStart false and UnlockedSprite null. When unlocked, its custom UnlockedMessage will not be displayed. Add an UnlockedSprite to resolve this.");
                }
            }
            return true;
        }

        /// <summary>
        /// Registers the <see cref="AvsVehicleModule"/> for <see cref="AvsVehicle"/>, sets up its prefab, recipe, and unlock conditions.
        /// </summary>
        /// <param name="folder">The folder containing the upgrade assets.</param>
        /// <param name="upgrade">The upgrade to register.</param>
        /// <returns>The TechType assigned to the upgrade.</returns>
        private static TechType RegisterAvsVehicleModule(Node folder, AvsVehicleModule upgrade)
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

            module_CustomPrefab
                .SetRecipe(moduleRecipe)
                .WithCraftingTime(upgrade.CraftingTime)
                .WithFabricatorType(AvsFabricator.TreeType)
                .WithStepsToFabricatorTab(folder.GetPath().Segments);
            module_CustomPrefab.SetPdaGroupCategory(TechGroup.VehicleUpgrades, TechCategory.VehicleUpgrades);
            module_CustomPrefab
                .SetEquipment(AvsVehicleBuilder.ModuleType)
                .WithQuickSlotType(upgrade.QuickSlotType);
            if (!upgrade.UnlockAtStart)
            {
                var scanningGadget = module_CustomPrefab.SetUnlock(upgrade.UnlockTechType);
                if (upgrade.UnlockedSprite != null)
                {
                    scanningGadget.WithAnalysisTech(upgrade.UnlockedSprite, unlockMessage: upgrade.UnlockedMessage);
                }
            }
            module_CustomPrefab.Register(); // this line causes PDA voice lag by 1.5 seconds ???????
            //upgrade.UnlockTechType = module_info.TechType;
            return module_info.TechType;
        }

        /// <summary>
        /// Registers the appropriate upgrade methods (passive, selectable, chargeable, toggleable) for the upgrade.
        /// </summary>
        /// <param name="upgrade">The upgrade to register methods for.</param>
        /// <param name="compat">Compatibility flags.</param>
        /// <param name="utt">Reference to UpgradeTechTypes to update.</param>
        /// <param name="isPDASetup">Indicates if PDA registration has occurred.</param>
        private static void RegisterUpgradeMethods(AvsVehicleModule upgrade, UpgradeCompat compat, ref UpgradeTechTypes utt, bool isPDASetup)
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
        private static void RegisterPassiveUpgradeActions(AvsVehicleModule upgrade, UpgradeCompat compat, ref UpgradeTechTypes utt, ref bool isPDASetup)
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
        private static void RegisterSelectableUpgradeActions(AvsVehicleModule upgrade, UpgradeCompat compat, ref UpgradeTechTypes utt, ref bool isPDASetup)
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
        private static void RegisterSelectableChargeableUpgradeActions(AvsVehicleModule upgrade, UpgradeCompat compat, ref UpgradeTechTypes utt, ref bool isPDASetup)
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
        private static void RegisterToggleableUpgradeActions(AvsVehicleModule upgrade, UpgradeCompat compat, ref UpgradeTechTypes utt, ref bool isPDASetup)
        {
            if (upgrade is ToggleableUpgrade toggle)
            {
                IEnumerator DoToggleAction(ToggleActionParams param, float timeToFirstActivation, float repeatDelay, float energyCostPerActivation)
                {
                    var isAvsVehicle = param.Vehicle.GetComponent<AvsVehicle>();
                    try
                    {
                        toggle.OnToggleInternal(param);
                    }
                    catch (Exception e)
                    {
                        LogWriter.Default.Error($"Error in {toggle.ClassId} OnToggle: {e.Message}", e);
                        param.Vehicle.ToggleSlot(param.SlotID, false);
                        yield break;
                    }
                    float preTime = Time.time;
                    yield return new WaitForSeconds(timeToFirstActivation);
                    float elapsed = Time.time - preTime;
                    param = param.AdvanceRepeatTime(elapsed);
                    while (true)
                    {
                        bool shouldStopWorking = isAvsVehicle ? !isAvsVehicle.IsBoarded : !param.Vehicle.GetPilotingMode();
                        if (shouldStopWorking)
                        {
                            param.Vehicle.ToggleSlot(param.SlotID, false);
                            try
                            {
                                toggle.OnToggleInternal(param.SetInactive());
                            }
                            catch (Exception e)
                            {
                                LogWriter.Default.Error($"Error in {toggle.ClassId} OnToggle: {e.Message}", e);
                            }
                            yield break;
                        }
                        try
                        {
                            toggle.OnRepeatInternal(param);
                        }
                        catch (Exception e)
                        {
                            LogWriter.Default.Error($"Error in {toggle.ClassId} OnRepeat: {e.Message}", e);
                            param.Vehicle.ToggleSlot(param.SlotID, false);
                            try
                            {
                                toggle.OnToggleInternal(param.SetInactive());
                            }
                            catch (Exception e2)
                            {
                                LogWriter.Default.Error($"Error in {toggle.ClassId} OnToggle: {e2.Message}", e2);
                            }
                            yield break;
                        }
                        float availablePower = param.Vehicle.energyInterface.TotalCanProvide(out _);
                        if (availablePower < energyCostPerActivation)
                        {
                            param.Vehicle.ToggleSlot(param.SlotID, false);
                            try
                            {
                                toggle.OnToggleInternal(param.SetInactive());
                            }
                            catch (Exception e)
                            {
                                LogWriter.Default.Error($"Error in {toggle.ClassId} OnToggle: {e.Message}", e);
                            }
                            yield break;
                        }
                        param.Vehicle.energyInterface.ConsumeEnergy(energyCostPerActivation);
                        preTime = Time.time;
                        yield return new WaitForSeconds(repeatDelay);
                        elapsed = Time.time - preTime;
                        param = param.AdvanceRepeatTime(elapsed);
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
                    var remove = ToggledActions.Where(x => !x.Value.IsValid).Select(x => x.Key).ToList();
                    foreach (var r in remove)
                    {
                        ToggledActions.Remove(r);
                    }
                    if (param.TechType != TechType.None && (param.TechType == mvTT || param.TechType == sTT || param.TechType == eTT || param.TechType == cTT))
                    {
                        var key = new VehicleSlotId(param.Vehicle, param.SlotID);

                        var doesExist = ToggledActions.TryGetValue(key, out var existing);
                        if (param.IsActive)
                        {
                            if (doesExist)
                            {
                                // Something triggers my Nautilus WithOnModuleToggled action doubly for the Seamoth.
                                // So if the toggle action already exists, don't add another one.
                                return;
                            }
                            var thisToggleCoroutine = param.Vehicle.StartCoroutine(DoToggleAction(param, toggle.DelayUntilFirstOnRepeat, toggle.RepeatDelay, toggle.EnergyCostPerActivation));
                            ToggledActions.Add(key, new ActiveAction(thisToggleCoroutine, param.Vehicle, toggle));
                        }
                        else
                        {
                            if (doesExist)
                            {
                                existing.Stop(param);
                                ToggledActions.Remove(key);
                            }
                        }
                    }
                }
                OnToggleActions.Add(WrappedOnToggle);
            }
        }
    }
}

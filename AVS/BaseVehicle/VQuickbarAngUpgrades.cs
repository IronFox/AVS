using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.BaseVehicle
{
    public abstract partial class AvsVehicle
    {
        private int numArmorModules = 0;
        private string[]? _slotIDs = null;


        /// <summary>
        /// Executed if a toggleable upgrade module is toggled on or off.
        /// </summary>
        /// <param name="slotID">Upgrade module slot</param>
        /// <param name="active">True if has been toggled on, false if off</param>
        public override void OnUpgradeModuleToggle(int slotID, bool active)
        {
            TechType techType = modules.GetTechTypeInSlot(slotIDs[slotID]);
            UpgradeTypes.ToggleActionParams param = new UpgradeTypes.ToggleActionParams
            {
                active = active,
                vehicle = this,
                slotID = slotID,
                techType = techType
            };
            Admin.UpgradeRegistrar.OnToggleActions.ForEach(x => x(param));
            base.OnUpgradeModuleToggle(slotID, active);
        }
        /// <summary>
        /// Executed when a usable upgrade module is used.
        /// </summary>
        /// <param name="techType">The tech type of the upgrade being used</param>
        /// <param name="slotID">Upgrade module slot</param>
        public override void OnUpgradeModuleUse(TechType techType, int slotID)
        {
            UpgradeTypes.SelectableActionParams param = new UpgradeTypes.SelectableActionParams
            (
                vehicle: this,
                slotID: slotID,
                techType: techType
            );
            Admin.UpgradeRegistrar.OnSelectActions.ForEach(x => x(param));

            UpgradeTypes.SelectableChargeableActionParams param2 = new UpgradeTypes.SelectableChargeableActionParams
            (
                vehicle: this,
                slotID: slotID,
                techType: techType,
                charge: param.Vehicle.quickSlotCharge[param.SlotID],
                slotCharge: param.Vehicle.GetSlotCharge(param.SlotID)
            );
            Admin.UpgradeRegistrar.OnSelectChargeActions.ForEach(x => x(param2));

            Patches.CompatibilityPatches.BetterVehicleStoragePatcher.TryUseBetterVehicleStorage(this, slotID, techType);
            base.OnUpgradeModuleUse(techType, slotID);
        }
        /// <summary>
        /// Executed when an upgrade module is added or removed.
        /// </summary>
        /// <param name="slotID">Slot index the module is added to or removed from</param>
        /// <param name="techType">Tech type of the module being added or removed</param>
        /// <param name="added">True if the module is added, false if removed</param>
        public override void OnUpgradeModuleChange(int slotID, TechType techType, bool added)
        {
            UpgradeOnAddedActions.ForEach(x => x(slotID, techType, added));
            UpgradeTypes.AddActionParams addedParams = new UpgradeTypes.AddActionParams
            {
                vehicle = this,
                slotID = slotID,
                techType = techType,
                isAdded = added
            };
            Admin.UpgradeRegistrar.OnAddActions.ForEach(x => x(addedParams));
        }
        /// <summary>
        /// Gets the quick slot type of the given slot ID.
        /// </summary>
        /// <param name="slotID">Slot index with 0 being the first</param>
        /// <returns>Slotted inventory item or null</returns>
        public override InventoryItem? GetSlotItem(int slotID)
        {
            if (slotID < 0 || slotID >= slotIDs.Length)
            {
                return null;
            }
            string slot = slotIDs[slotID];
            if (upgradesInput.equipment.equipment.TryGetValue(slot, out InventoryItem result))
            {
                return result;
            }
            return null;
        }

        /// <summary>
        /// Deselects quick-slots and exits piloting.
        /// Misnomer but since the base class has this method, we must override it.
        /// Invoked when you press the Exit button while having a "currentMountedVehicle."
        /// </summary>
        public override void DeselectSlots()
        {
            if (ignoreInput)
            {
                return;
            }
            int i = 0;
            int num = slotIDs.Length;
            while (i < num)
            {
                QuickSlotType quickSlotType = GetQuickSlotType(i, out _);
                if (quickSlotType == QuickSlotType.Toggleable || quickSlotType == QuickSlotType.Selectable || quickSlotType == QuickSlotType.SelectableChargeable)
                {
                    ToggleSlot(i, false);
                }
                quickSlotCharge[i] = 0f;
                i++;
            }
            activeSlot = -1;
            NotifySelectSlot(activeSlot);
            DoExitRoutines();
        }

        /// <summary>
        /// The slotIds of the vehicle.
        /// </summary>
        public override string[] slotIDs
        {
            get
            {
                if (_slotIDs == null)
                {
                    _slotIDs = GenerateSlotIDs(Config.NumModules);
                }
                return _slotIDs;
            }
        }

        private static string[] GenerateModuleSlots(int modules)
        {
            string[] retIDs;
            retIDs = new string[modules];
            for (int i = 0; i < modules; i++)
            {
                retIDs[i] = ModuleBuilder.ModuleName(i);
            }
            return retIDs;
        }
        private static string[] GenerateSlotIDs(int modules)
        {
            string[] retIDs;
            int numUpgradesTotal = modules;
            retIDs = new string[numUpgradesTotal];
            for (int i = 0; i < modules; i++)
            {
                retIDs[i] = ModuleBuilder.ModuleName(i);
            }
            return retIDs;
        }

        /// <summary>
        /// The number of installed power efficiency modules.
        /// Automatically updated when a power efficiency module is added or removed.
        /// </summary>
        public int NumEfficiencyModules { get; private set; } = 0;



        /// <summary>
        /// Actions to execute when an upgrade module is added or removed.
        /// The first argument is the slot ID,
        /// then the tech type of the module,
        /// finally a boolean indicating if the module is being added (true) or removed (false).
        /// </summary>
        internal List<Action<int, TechType, bool>> UpgradeOnAddedActions { get; }
            = new List<Action<int, TechType, bool>>();



        internal List<string> VehicleModuleSlots => GenerateModuleSlots(Config.NumModules).ToList();
        internal Dictionary<EquipmentType, List<string>> VehicleTypeToSlots => new Dictionary<EquipmentType, List<string>>
                {
                    { VehicleBuilder.ModuleType, VehicleModuleSlots }
                };



        private void StorageModuleAction(int slotID, TechType techType, bool added)
        {
            if (techType == TechType.VehicleStorageModule)
            {
                SetStorageModule(slotID, added);
            }
        }
        private void ArmorPlatingModuleAction(int slotID, TechType techType, bool added)
        {
            if (techType == TechType.VehicleArmorPlating)
            {
                _ = added ? numArmorModules++ : numArmorModules--;
                GetComponent<DealDamageOnImpact>().mirroredSelfDamageFraction = 0.5f * Mathf.Pow(0.5f, numArmorModules);
            }
        }
        private void PowerUpgradeModuleAction(int slotID, TechType techType, bool added)
        {
            if (techType == TechType.VehiclePowerUpgradeModule)
            {
                _ = added ? NumEfficiencyModules++ : NumEfficiencyModules--;
            }
        }
        private bool IsAllowedToRemove(Pickupable pickupable, bool verbose)
        {
            if (pickupable.GetTechType() == TechType.VehicleStorageModule)
            {
                // check the appropriate storage module for emptiness
                SeamothStorageContainer component = pickupable.GetComponent<SeamothStorageContainer>();
                if (component != null)
                {
                    bool flag = component.container.count == 0;
                    if (verbose && !flag)
                    {
                        ErrorMessage.AddDebug(Language.main.Get("SeamothStorageNotEmpty"));
                    }
                    return flag;
                }
                Debug.LogError("No VehicleStorageContainer found on VehicleStorageModule item");
            }
            return true;
        }
        private void HandleExtraQuickSlotInputs()
        {
            if (IsPlayerControlling())
            {
                if (Input.GetKeyDown(KeyCode.Alpha6))
                {
                    SlotKeyDown(5);
                }
                if (Input.GetKeyDown(KeyCode.Alpha7))
                {
                    SlotKeyDown(6);
                }
                if (Input.GetKeyDown(KeyCode.Alpha8))
                {
                    SlotKeyDown(7);
                }
                if (Input.GetKeyDown(KeyCode.Alpha9))
                {
                    SlotKeyDown(8);
                }
                if (Input.GetKeyDown(KeyCode.Alpha0))
                {
                    SlotKeyDown(9);
                }
            }
        }
        private void SetStorageModule(int slotID, bool activated)
        {
            foreach (var sto in Com.InnateStorages)
            {
                sto.Container.SetActive(true);
            }
            if (Com.ModularStorages.Count <= slotID)
            {
                ErrorMessage.AddWarning("There is no storage expansion for slot ID: " + slotID.ToString());
                return;
            }
            var modSto = Com.ModularStorages[slotID];
            modSto.Container.SetActive(activated);
            if (activated)
            {
                var modularContainer = GetSeamothStorageContainer(slotID);
                if (modularContainer == null)
                {
                    Log.Warn("Warning: failed to get modular storage container for slotID: " + slotID.ToString());
                    return;
                }
                modularContainer.height = modSto.Height;
                modularContainer.width = modSto.Width;
                ModGetStorageInSlot(slotID, TechType.VehicleStorageModule)?.Resize(modSto.Width, modSto.Height);
            }
        }
        internal SeamothStorageContainer? GetSeamothStorageContainer(int slotID)
        {
            var slotItem = GetSlotItem(slotID);
            if (slotItem == null)
            {
                Log.Warn("Warning: failed to get item for that slotID: " + slotID.ToString());
                return null;
            }
            Pickupable item = slotItem.item;
            if (item.GetTechType() != TechType.VehicleStorageModule)
            {
                Log.Warn("Warning: failed to get pickupable for that slotID: " + slotID.ToString());
                return null;
            }
            SeamothStorageContainer component = item.GetComponent<SeamothStorageContainer>();
            return component;
        }
        internal ItemsContainer? ModGetStorageInSlot(int slotID, TechType techType)
        {
            switch (techType)
            {
                case VehicleBuilder.InnateStorage:
                    {
                        InnateStorageContainer vsc;
                        if (0 <= slotID && slotID < Com.InnateStorages.Count)
                        {
                            vsc = Com.InnateStorages[slotID].Container.GetComponent<InnateStorageContainer>();
                        }
                        else
                        {
                            Log.Error("Error: ModGetStorageInSlot called on invalid innate storage slotID");
                            return null;
                        }
                        return vsc.Container;
                    }
                case TechType.VehicleStorageModule:
                    {
                        var component = GetSeamothStorageContainer(slotID);
                        if (component == null)
                        {
                            Log.Warn("Warning: failed to get storage-container for that slotID: " + slotID.ToString());
                            return null;
                        }
                        return component.container;
                    }
                default:
                    {
                        Log.Error("Error: tried to get storage for unsupported TechType");
                        return null;
                    }
            }
        }
    }
}

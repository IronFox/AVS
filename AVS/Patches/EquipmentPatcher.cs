using AVS.BaseVehicle;
using HarmonyLib;
using System.Collections.Generic;

// PURPOSE: allow AvsVehicle upgrade slots to mesh with the game systems
// VALUE: High

namespace AVS
{
    /// <summary>
    /// The EquipmentPatcher class is designed to enhance the functionality and compatibility
    /// of the base game's Equipment class with custom modifications introduced by AvsVehicles.
    /// It utilizes Harmony patches to modify or extend specific methods within the Equipment
    /// class to handle new upgrade slots and ensure proper interaction with the game's mechanics.
    /// </summary>
    [HarmonyPatch(typeof(Equipment))]
    public class EquipmentPatcher
    {

        /*
         * This collection of patches ensures our new upgrade slots interact nicely with the base game's Equipment class.
         * 
         * At first glance, it appears problematic that I overwrite Equipment.typeToSlots,
         * but typeToSlots is an instance field, and I only overwrite it for AvsVehicles.
         */

        /// <summary>
        /// Prefix method for the SetLabel method in the Equipment class. Updates the typeToSlots
        /// dictionary for Equipment instances owned by an AvsVehicle, ensuring compatibility with
        /// the AvsVehicles' custom upgrade slots.
        /// </summary>
        /// <param name="__instance">The Equipment instance calling the SetLabel method.</param>
        /// <param name="l">The label string passed to the original SetLabel method.</param>
        /// <param name="___typeToSlots">A reference to the Equipment's typeToSlots dictionary
        /// that maps EquipmentType to a list of slot names.</param>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Equipment.SetLabel))]
        public static void SetLabelPrefix(Equipment __instance, string l, ref Dictionary<EquipmentType, List<string>> ___typeToSlots)
        {
            if (!ModuleBuilder.IsModuleName(l))
            {
                return;
            }
            AvsVehicle mv = __instance.owner.GetComponentInParent<AvsVehicle>();
            if (mv == null)
            {
                return;
            }
            ___typeToSlots = mv.VehicleTypeToSlots;
            return;
        }

        /// <summary>
        /// Prefix method for the AddSlot method in the Equipment class. Adjusts the typeToSlots
        /// dictionary for Equipment instances owned by an AvsVehicle. Ensures the slots align
        /// with the custom configuration of the AvsVehicle's module upgrades.
        /// </summary>
        /// <param name="__instance">The Equipment instance calling the AddSlot method.</param>
        /// <param name="slot">The slot name passed to the original AddSlot method.</param>
        /// <param name="___typeToSlots">A reference to the Equipment's typeToSlots dictionary that maps EquipmentType to a list of slot names.</param>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Equipment.AddSlot))]
        public static void AddSlotPrefix(Equipment __instance, string slot, ref Dictionary<EquipmentType, List<string>> ___typeToSlots)
        {
            if (!ModuleBuilder.IsModuleName(slot))
            {
                return;
            }
            AvsVehicle mv = __instance.owner.GetComponentInParent<AvsVehicle>();
            if (mv == null)
            {
                return;
            }
            ___typeToSlots = mv.VehicleTypeToSlots;
            return;
        }

        /// <summary>
        /// Prefix method for the GetCompatibleSlotDefault method in the Equipment class. Updates the typeToSlots
        /// dictionary with the VehicleTypeToSlots dictionary from the owning AvsVehicle, ensuring slot compatibility
        /// for specific EquipmentTypes related to the AvsVehicle class.
        /// </summary>
        /// <param name="__instance">The Equipment instance calling the GetCompatibleSlotDefault method.</param>
        /// <param name="itemType">The EquipmentType being processed to determine slot compatibility.</param>
        /// <param name="___typeToSlots">A reference to the Equipment's typeToSlots dictionary that maps EquipmentType to a list of slot names.</param>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Equipment.GetCompatibleSlotDefault))]
        public static void GetCompatibleSlotDefaultPrefix(Equipment __instance, EquipmentType itemType, ref Dictionary<EquipmentType, List<string>> ___typeToSlots)
        {
            if (itemType != AvsVehicleBuilder.ModuleType && itemType != AvsVehicleBuilder.ArmType)
            {
                return;
            }
            AvsVehicle mv = __instance.owner.GetComponentInParent<AvsVehicle>();
            if (mv == null)
            {
                return;
            }
            ___typeToSlots = mv.VehicleTypeToSlots;
            return;
        }

        /// <summary>
        /// Prefix method for the GetFreeSlot method in the Equipment class. Modifies the typeToSlots
        /// dictionary for Equipment instances owned by an AvsVehicle, ensuring slots are updated appropriately
        /// based on the vehicle's custom configuration for specific EquipmentType values.
        /// </summary>
        /// <param name="__instance">The instance of the Equipment class invoking the GetFreeSlot method.</param>
        /// <param name="type">The EquipmentType being queried for a free slot.</param>
        /// <param name="___typeToSlots">A reference to the Equipment's typeToSlots dictionary, mapping
        /// EquipmentType to a list of slot names.</param>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Equipment.GetFreeSlot))]
        public static void GetFreeSlotPrefix(Equipment __instance, EquipmentType type, ref Dictionary<EquipmentType, List<string>> ___typeToSlots)
        {
            if (type != AvsVehicleBuilder.ModuleType && type != AvsVehicleBuilder.ArmType)
            {
                return;
            }
            AvsVehicle mv = __instance.owner.GetComponentInParent<AvsVehicle>();
            if (mv == null)
            {
                return;
            }
            ___typeToSlots = mv.VehicleTypeToSlots;
            return;
        }

        /// <summary>
        /// Prefix method for the GetSlots method in the Equipment class. Adjusts the typeToSlots
        /// dictionary for Equipment instances to ensure compatibility with AvsVehicles' slot configurations.
        /// </summary>
        /// <param name="__instance">The Equipment instance calling the GetSlots method.</param>
        /// <param name="itemType">The EquipmentType value passed to the original GetSlots method.</param>
        /// <param name="___typeToSlots">A reference to the Equipment's typeToSlots dictionary,
        /// which maps EquipmentType to a list of slot names.</param>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Equipment.GetSlots))]
        public static void GetSlotsPrefix(Equipment __instance, EquipmentType itemType, ref Dictionary<EquipmentType, List<string>> ___typeToSlots)
        {
            if (itemType != AvsVehicleBuilder.ModuleType && itemType != AvsVehicleBuilder.ArmType)
            {
                return;
            }
            AvsVehicle mv = __instance.owner.GetComponentInParent<AvsVehicle>();
            if (mv == null)
            {
                return;
            }
            ___typeToSlots = mv.VehicleTypeToSlots;
            return;
        }

        /// <summary>
        /// Prefix method for the RemoveSlot method in the Equipment class. This method
        /// ensures that any removal of a slot for an equipment instance owned by an AvsVehicle
        /// is aligned with the AvsVehicle's custom VehicleTypeToSlots mapping.
        /// </summary>
        /// <param name="__instance">The Equipment instance calling the RemoveSlot method.</param>
        /// <param name="slot">The name of the slot to be removed.</param>
        /// <param name="___typeToSlots">A reference to the Equipment's typeToSlots dictionary
        /// that maps EquipmentType to a list of slot names.</param>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Equipment.RemoveSlot))]
        public static void RemoveSlot(Equipment __instance, string slot, ref Dictionary<EquipmentType, List<string>> ___typeToSlots)
        {
            if (!ModuleBuilder.IsModuleName(slot))
            {
                return;
            }
            AvsVehicle mv = __instance.owner.GetComponentInParent<AvsVehicle>();
            if (mv == null)
            {
                return;
            }
            ___typeToSlots = mv.VehicleTypeToSlots;
            return;
        }

        /// <summary>
        /// Prefix method for the GetSlotType method in the Equipment class. Determines the EquipmentType
        /// for a given slot name, enabling custom behavior for module slots if applicable.
        /// </summary>
        /// <param name="slot">The slot name passed to the original GetSlotType method.</param>
        /// <param name="__result">A reference to the result EquipmentType that will be returned by the original method.</param>
        /// <param name="___typeToSlots">A reference to the Equipment's typeToSlots dictionary that maps EquipmentType to a list of slot names.</param>
        /// <returns>Returns false if the slot corresponds to a module type, bypassing the original method. Returns true otherwise, allowing the original method to execute.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Equipment.GetSlotType))]
        public static bool GetSlotTypePrefix(string slot, ref EquipmentType __result,
            Dictionary<EquipmentType, List<string>> ___typeToSlots)
        {
            if (ModuleBuilder.IsModuleName(slot))
            {
                __result = AvsVehicleBuilder.ModuleType;
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Prefix method for the IsCompatible method in the Equipment class. Determines whether an EquipmentType
        /// is compatible with a given slot type, considering additional custom compatibility for specific types such as VehicleModule and AvsVehicleBuilder.ModuleType.
        /// </summary>
        /// <param name="itemType">The EquipmentType of the item being checked for compatibility.</param>
        /// <param name="slotType">The EquipmentType of the slot being checked for compatibility with the item.</param>
        /// <param name="__result">A reference to the compatibility check result, which will be set to true if the itemType is compatible with the slotType.</param>
        /// <returns>Returns false to skip the original method execution if compatibility is found; otherwise, returns true to allow the original method to execute.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Equipment.IsCompatible))]
        public static bool IsCompatiblePrefix(EquipmentType itemType, EquipmentType slotType, ref bool __result)
        {
            __result = itemType == slotType || (itemType == EquipmentType.VehicleModule && (slotType == EquipmentType.SeamothModule || slotType == EquipmentType.ExosuitModule || slotType == AvsVehicleBuilder.ModuleType));
            if (__result)
            {
                return false;
            }
            return true;
        }
    }
}

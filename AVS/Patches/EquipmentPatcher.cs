using AVS.BaseVehicle;
using HarmonyLib;
using System.Collections.Generic;

// PURPOSE: allow AvsVehicle upgrade slots to mesh with the game systems
// VALUE: High

namespace AVS
{
    [HarmonyPatch(typeof(Equipment))]
    public class EquipmentPatcher
    {
        /*
         * Atrama has 8 total upgrade slots
         * 6 module slots : "AtramaModuleX" where X int in [0,5]
         * 2 arm slots : "AtramaArmX" where X in {Left, Right}
         */

        /*
         * This collection of patches ensures our new upgrade slots interact nicely with the base game's Equipment class.
         * 
         * At first glance, it appears problematic that I overwrite Equipment.typeToSlots,
         * but typeToSlots is an instance field, and I only overwrite it for ModVehicles.
         */

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

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Equipment.GetSlotType))]
        public static bool GetSlotTypePrefix(string slot, ref EquipmentType __result, Dictionary<EquipmentType, List<string>> ___typeToSlots)
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

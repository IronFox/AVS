using AVS.Log;
using AVS.VehicleBuilding;
using HarmonyLib;
using System.Collections.Generic;

// PURPOSE: PDA displays AvsVehicle upgrades correctly
// VALUE: Very high.

namespace AVS.Patches;

/// <summary>
/// A Harmony patching class designed to ensure proper integration of custom
/// AvsVehicle upgrades into the base game's uGUI_Equipment system in Subnautica.
/// This class modifies the equipment UI behavior to support custom upgrade slots
/// and ensures that the PDA displays vehicle upgrades accurately.
/// </summary>
[HarmonyPatch(typeof(uGUI_Equipment))]
public class uGUI_EquipmentPatcher
{
    /*
     * This collection of patches ensures our upgrade slots mesh well
     * with the base-game uGUI_Equipment system.
     * That is, we ensure here that our PDA displays AvsVehicle upgrades correctly
     */
    /// <summary>
    /// A postfix method that modifies the behavior of the <c>uGUI_Equipment</c> class's Awake method to integrate
    /// custom vehicle upgrade slots into the default equipment system of Subnautica.
    /// Initializes the custom ModuleBuilder class, sets up all upgrade slots, and ensures proper integration into the UI.
    /// </summary>
    /// <param name="__instance">The instance of the <c>uGUI_Equipment</c> class being patched.</param>
    /// <param name="___allSlots">
    /// A reference to the dictionary containing all equipment slots managed by the <c>uGUI_Equipment</c> system.
    /// This parameter is used to integrate custom slots for vehicle upgrades.
    /// </param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(uGUI_Equipment.Awake))]
    public static void AwakePostfix(uGUI_Equipment __instance, ref Dictionary<string, uGUI_EquipmentSlot> ___allSlots)
    {
        using var log = SmartLog.LazyForAVS(RootModController.AnyInstance, parameters: Params.Of(__instance, ___allSlots.Count));

        ModuleBuilder.Init(ref ___allSlots);
    }

    /// <summary>
    /// A prefix method that modifies the behavior of the <c>uGUI_Equipment</c> class's OnDragHoverEnter method to integrate
    /// custom vehicle upgrade slots into the base game's equipment slot system in Subnautica.
    /// Ensures accurate synchronization of the custom slots to allow proper drag-and-drop functionality during upgrades.
    /// </summary>
    /// <param name="___allSlots">
    /// A reference to the dictionary containing all equipment slots managed by the <c>uGUI_Equipment</c> system.
    /// This parameter is updated to include custom slots for vehicle upgrades, enabling seamless interaction and display.
    /// </param>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(uGUI_Equipment.OnDragHoverEnter))]
    public static void OnDragHoverEnterPatch(ref Dictionary<string, uGUI_EquipmentSlot> ___allSlots)
    {
        ModuleBuilder.LinkVehicleSlots(ref ___allSlots, false);
        ___allSlots = ModuleBuilder.AllVehicleSlots;
    }

    /// <summary>
    /// A prefix method that modifies the behavior of the <c>uGUI_Equipment</c> class's <c>OnDragHoverStay</c> method
    /// to ensure custom vehicle upgrade slots are properly integrated into the equipment system in Subnautica.
    /// This method synchronizes the slot data between the default equipment system and the custom slot implementation
    /// managed by the <c>ModuleBuilder</c>.
    /// </summary>
    /// <param name="___allSlots">
    /// A reference to the dictionary that maintains all equipment slots managed by <c>uGUI_Equipment</c>.
    /// This parameter is updated to include custom vehicle upgrade slots defined in the <c>ModuleBuilder</c>.
    /// </param>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(uGUI_Equipment.OnDragHoverStay))]
    public static void OnDragHoverStayPatch(ref Dictionary<string, uGUI_EquipmentSlot> ___allSlots)
    {
        ModuleBuilder.LinkVehicleSlots(ref ___allSlots, false);
        ___allSlots = ModuleBuilder.AllVehicleSlots;
    }

    /// <summary>
    /// A prefix method that patches the behavior of the <c>uGUI_Equipment</c> class's OnDragHoverExit method
    /// to ensure custom vehicle upgrade slots are merged and properly managed in the equipment system.
    /// Updates the <c>___allSlots</c> dictionary with the custom slots defined by the <c>ModuleBuilder</c>.
    /// </summary>
    /// <param name="___allSlots">
    /// A reference to the dictionary containing all equipment slots managed by the <c>uGUI_Equipment</c> system.
    /// This parameter is modified to include all custom vehicle slots from the <c>ModuleBuilder</c>.
    /// </param>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(uGUI_Equipment.OnDragHoverExit))]
    public static void OnDragHoverExitPatch(ref Dictionary<string, uGUI_EquipmentSlot> ___allSlots)
    {
        ModuleBuilder.LinkVehicleSlots(ref ___allSlots, false);
        ___allSlots = ModuleBuilder.AllVehicleSlots;
    }
}
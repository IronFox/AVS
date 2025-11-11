using AVS.Log;
using AVS.VehicleBuilding;
using HarmonyLib;
using System.Collections.Generic;

#pragma warning disable IDE1006 // Naming Styles

// PURPOSE: PDA displays AvsVehicle upgrades correctly
// VALUE: Very high.

namespace AVS.Patches;

/// <summary>
/// Provides Harmony patches for the <see cref="uGUI_Equipment"/> class to integrate custom
/// AVS vehicle upgrade slots into Subnautica's equipment UI system.
/// </summary>
/// <remarks>
/// <para>
/// This patcher ensures that custom vehicle upgrade slots work seamlessly with the base game's
/// equipment interface. Without these patches, AVS vehicle upgrades would not display correctly
/// in the PDA or support proper drag-and-drop interactions.
/// </para>
/// <para>
/// The patches intercept key lifecycle and interaction methods to synchronize custom slots
/// managed by <see cref="ModuleBuilder"/> with the game's native slot dictionary, enabling:
/// <list type="bullet">
/// <item><description>Proper initialization of custom upgrade slots during UI setup</description></item>
/// <item><description>Correct display of vehicle upgrades in the PDA interface</description></item>
/// <item><description>Seamless drag-and-drop functionality for upgrade modules</description></item>
/// <item><description>Hover state management for interaction feedback</description></item>
/// </list>
/// </para>
/// </remarks>
[HarmonyPatch(typeof(uGUI_Equipment))]
public class uGUI_EquipmentPatcher
{
    /*
     * This collection of patches ensures our upgrade slots mesh well
     * with the base-game uGUI_Equipment system.
     * That is, we ensure here that our PDA displays AvsVehicle upgrades correctly
     */

    /// <summary>
    /// Postfix patch for <see cref="uGUI_Equipment.Awake"/> that initializes custom vehicle upgrade slots.
    /// </summary>
    /// <param name="__instance">The <see cref="uGUI_Equipment"/> instance being initialized.</param>
    /// <param name="___allSlots">
    /// The private <c>allSlots</c> field containing all equipment slots. Custom AVS vehicle
    /// slots are registered into this dictionary by <see cref="ModuleBuilder.Init"/>.
    /// </param>
    /// <remarks>
    /// This patch runs after the original <c>Awake</c> method completes, ensuring the base
    /// equipment system is fully initialized before custom slots are added. The <see cref="ModuleBuilder"/>
    /// creates and registers all AVS vehicle upgrade slots during this initialization phase.
    /// </remarks>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(uGUI_Equipment.Awake))]
    public static void AwakePostfix(uGUI_Equipment __instance, Dictionary<string, uGUI_EquipmentSlot> ___allSlots)
    {
        using var log = SmartLog.LazyForAVS(RootModController.AnyInstance, parameters: Params.Of(__instance, ___allSlots.Count));

        ModuleBuilder.Init(__instance, ___allSlots);
    }

    /// <summary>
    /// Prefix patch for <see cref="uGUI_Equipment.OnDragHoverEnter"/> that synchronizes custom vehicle slots
    /// when a dragged item begins hovering over the equipment UI.
    /// </summary>
    /// <param name="__instance">The <see cref="uGUI_Equipment"/> instance handling the drag operation.</param>
    /// <param name="___allSlots">
    /// The private <c>allSlots</c> field that is replaced with <see cref="ModuleBuilder.AllVehicleSlots"/>
    /// to ensure custom slots are recognized during hover detection.
    /// </param>
    /// <remarks>
    /// This patch ensures that when the player drags an upgrade module over the PDA interface,
    /// custom AVS vehicle slots are properly registered and can respond to hover events.
    /// The <see cref="ModuleBuilder.LinkVehicleSlots"/> call synchronizes the slots without forcing
    /// a full refresh (third parameter is <c>false</c>).
    /// </remarks>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(uGUI_Equipment.OnDragHoverEnter))]
    public static void OnDragHoverEnterPatch(uGUI_Equipment __instance, Dictionary<string, uGUI_EquipmentSlot> ___allSlots)
    {
        ModuleBuilder.LinkVehicleSlots(__instance, ___allSlots, false);
        ___allSlots = ModuleBuilder.AllVehicleSlots;
    }

    /// <summary>
    /// Prefix patch for <see cref="uGUI_Equipment.OnDragHoverStay"/> that maintains custom vehicle slot
    /// synchronization while a dragged item continues hovering over the equipment UI.
    /// </summary>
    /// <param name="__instance">The <see cref="uGUI_Equipment"/> instance handling the drag operation.</param>
    /// <param name="___allSlots">
    /// The private <c>allSlots</c> field that is continuously updated with <see cref="ModuleBuilder.AllVehicleSlots"/>
    /// to ensure custom slots remain responsive during sustained hover.
    /// </param>
    /// <remarks>
    /// This patch is called repeatedly while the player holds a dragged item over the equipment interface.
    /// It ensures custom AVS vehicle slots remain synchronized throughout the hover duration,
    /// enabling accurate visual feedback and drop target validation.
    /// </remarks>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(uGUI_Equipment.OnDragHoverStay))]
    public static void OnDragHoverStayPatch(uGUI_Equipment __instance, ref Dictionary<string, uGUI_EquipmentSlot> ___allSlots)
    {
        ModuleBuilder.LinkVehicleSlots(__instance, ___allSlots, false);
        ___allSlots = ModuleBuilder.AllVehicleSlots;
    }

    /// <summary>
    /// Prefix patch for <see cref="uGUI_Equipment.OnDragHoverExit"/> that ensures custom vehicle slots
    /// are synchronized when a dragged item stops hovering over the equipment UI.
    /// </summary>
    /// <param name="__instance">The <see cref="uGUI_Equipment"/> instance handling the drag operation.</param>
    /// <param name="___allSlots">
    /// The private <c>allSlots</c> field that is updated with <see cref="ModuleBuilder.AllVehicleSlots"/>
    /// to ensure proper cleanup of hover states for custom slots.
    /// </param>
    /// <remarks>
    /// This patch ensures that when a dragged upgrade module leaves the equipment interface area,
    /// custom AVS vehicle slots properly clear their hover states and remain synchronized with
    /// the base game's equipment system, preventing visual artifacts and interaction issues.
    /// </remarks>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(uGUI_Equipment.OnDragHoverExit))]
    public static void OnDragHoverExitPatch(uGUI_Equipment __instance, ref Dictionary<string, uGUI_EquipmentSlot> ___allSlots)
    {
        ModuleBuilder.LinkVehicleSlots(__instance, ___allSlots, false);
        ___allSlots = ModuleBuilder.AllVehicleSlots;
    }
}
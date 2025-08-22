using HarmonyLib;
using AVS.Util;

// PURPOSE: Avoid an error at game-exit (LargeWorldStreamer.UnloadGlobalRoot) whereby this method is called with a null InventoryItem
// VALUE: Moderate. Important to make sure things continue to unload correctly (memory leak?)

namespace AVS.Patches;

/// <summary>
/// A patching class for the EnergyMixin component used in the game.
/// This class provides a Harmony patch specifically targeting the NotifyHasBattery method of EnergyMixin.
/// </summary>
/// <remarks>
/// The purpose of this patch is to handle a specific edge case where the EnergyMixin's NotifyHasBattery method is invoked with a null InventoryItem.
/// This could prevent potential errors during game exit, ensuring proper unloading and avoiding memory leaks.
/// </remarks>
[HarmonyPatch(typeof(EnergyMixin))]
public class EnergyMixinPatcher
{
    /// <summary>
    /// A Harmony prefix method that patches the NotifyHasBattery method of the EnergyMixin class.
    /// Handles cases where the NotifyHasBattery method might be called with a null InventoryItem,
    /// preventing potential errors and ensuring proper behavior during the game's unload process.
    /// </summary>
    /// <param name="__instance">The EnergyMixin instance on which the NotifyHasBattery method is invoked.</param>
    /// <param name="item">The InventoryItem associated with the NotifyHasBattery method call; can be null.</param>
    /// <returns>
    /// A boolean value indicating whether the original method should execute (true) or not (false).
    /// Returns false to skip the original method execution in scenarios where InventoryItem is null or invalid.
    /// </returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(EnergyMixin.NotifyHasBattery))]
    public static bool EnergyMixinNotifyHasBatteryHarmonyPrefix(EnergyMixin __instance, InventoryItem item)
    {
        if (__instance.batteryModels.IsNull())
            return false;
        if (__instance.batteryModels.Length > 0)
            if (item.IsNull() || item.item.IsNull())
            {
                if (__instance.controlledObjects.IsNotNull())
                    __instance.controlledObjects.ForEach(x => x.SetActive(false));
                if (__instance.batteryModels.IsNotNull())
                    __instance.batteryModels.ForEach(x =>
                    {
                        if (x.model)
                            x.model.SetActive(false);
                    });
                return false;
            }

        return true;
    }
}
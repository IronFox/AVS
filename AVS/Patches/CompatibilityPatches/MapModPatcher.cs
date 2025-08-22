using HarmonyLib;
using System.Reflection;
using AVS.Assets;
using AVS.Util;
using UnityEngine;

// PURPOSE: ensure AvsVehicles are displayed correctly in the Map mod
// VALUE: High. The Map is a great mod!

namespace AVS.Patches.CompatibilityPatches;

/// <summary>
/// The MapModPatcher class provides compatibility patches for the Map Mod,
/// ensuring that AvsVehicles are displayed correctly on the map using their corresponding ping sprites.
/// This patch prevents the Map Mod from crashing when handling custom AvsVehicle pings.
/// </summary>
public static class MapModPatcher
{
    /*
     * This patch is specifically for the Map Mod.
     * It ensures that our AvsVehicles are displayed correctly as their Ping Sprites.
     * Without this patch, the Map Mod dies completely.
     */
    /// <summary>
    /// Harmony prefix patch for ensuring AvsVehicles are displayed correctly on the Map Mod,
    /// using their associated ping sprites. Prevents the Map Mod from crashing when handling
    /// custom AvsVehicle pings.
    /// </summary>
    /// <param name="__instance">The target instance of the Map Mod object being patched.</param>
    /// <returns>
    /// A boolean indicating whether the original method should execute. Returns false
    /// if the prefix processing handles the requirement and the original method should be skipped.
    /// </returns>
    [HarmonyPrefix]
    public static bool Prefix(object __instance)
    {
        var field = __instance.GetType().GetField("ping");
        var ping = field.GetValue(__instance) as PingInstance;
        if (ping.IsNull())
            return true;
        foreach (var mvPIs in AvsVehicleManager.MvPings)
            if (mvPIs.pingType == ping.pingType)
            {
                var field2 = __instance.GetType().GetField("icon");
                var icon = field2?.GetValue(__instance) as uGUI_Icon;
                if (icon.IsNull())
                    continue; // If we don't have an icon, we can't modify it
                icon.sprite = SpriteManager.Get(TechType.Exosuit);
                foreach (var mvType in AvsVehicleManager.VehicleTypes)
                    if (mvType.pt == ping.pingType)
                    {
                        icon.sprite = new Atlas.Sprite(mvType.ping_sprite);
                        break;
                    }

                var rectTransform = icon.rectTransform;
                rectTransform.sizeDelta = Vector2.one * 28f;
                rectTransform.localPosition = Vector3.zero;
                return false;
            }

        return true;
    }
}
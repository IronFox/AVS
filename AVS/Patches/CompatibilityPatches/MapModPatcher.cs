using HarmonyLib;
using System.Reflection;
using UnityEngine;

// PURPOSE: ensure ModVehicles are displayed correctly in the Map mod
// VALUE: High. The Map is a great mod!

namespace AVS.Patches.CompatibilityPatches
{
    public static class MapModPatcher
    {
        /*
         * This patch is specifically for the Map Mod.
         * It ensures that our ModVehicles are displayed correctly as their Ping Sprites.
         * Without this patch, the Map Mod dies completely.
         */
        [HarmonyPrefix]
        public static bool Prefix(object __instance)
        {
            FieldInfo field = __instance.GetType().GetField("ping");
            var ping = field.GetValue(__instance) as PingInstance;
            if (ping == null)
            {
                return true;
            }
            foreach (var mvPIs in VehicleManager.MvPings)
            {
                if (mvPIs.pingType == ping.pingType)
                {
                    var field2 = __instance.GetType().GetField("icon");
                    var icon = field2?.GetValue(__instance) as uGUI_Icon;
                    if (icon == null)
                        continue; // If we don't have an icon, we can't modify it
                    icon.sprite = SpriteManager.Get(TechType.Exosuit);
                    foreach (var mvType in VehicleManager.VehicleTypes)
                    {
                        if (mvType.pt == ping.pingType)
                        {
                            icon.sprite = mvType.ping_sprite;
                            break;
                        }
                    }
                    RectTransform rectTransform = icon.rectTransform;
                    rectTransform.sizeDelta = Vector2.one * 28f;
                    rectTransform.localPosition = Vector3.zero;
                    return false;
                }
            }
            return true;
        }
    }
}

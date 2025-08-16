using System.Collections.Generic;
using HarmonyLib;

namespace AVS.Patches;

[HarmonyPatch(typeof(SDFCutout))]
public class SDFCutoutPatcher
{
    private static List<SDFCutout> suppressed = [];

    /// <summary>
    /// Notifies that the Start() method should not be called on this cutout instance (because it has been
    /// initialized externally)
    /// </summary>
    /// <param name="cutout">Cutout to suppress Start() of</param>
    public static void SuppressStartOf(SDFCutout cutout)
    {
        if (suppressed.Contains(cutout))
            return;
        suppressed.Add(cutout);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(SDFCutout.Start))]
    public static bool StartPatch(SDFCutout __instance)
    {
        suppressed.RemoveAll(x => !x);
        if (suppressed.Contains(__instance))
            return false;
        return true;
    }
}
using System.Collections.Generic;
using HarmonyLib;

namespace AVS.Patches;

/// <summary>
/// A patching class for the SDFCutout component, providing functionality
/// to suppress the execution of the Start method when a cutout instance has been initialized externally.
/// Uses Harmony to intercept the Start method of SDFCutout.
/// </summary>
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

    /// <summary>
    /// A Harmony prefix patch for the Start method of the SDFCutout class.
    /// Controls whether the Start method should execute, based on whether the cutout instance
    /// has been marked to suppress its Start execution.
    /// </summary>
    /// <param name="__instance">The instance of the SDFCutout whose Start method is being intercepted.</param>
    /// <returns>
    /// Returns false if the Start method should be skipped because the instance is suppressed.
    /// Returns true if the Start method should execute normally.
    /// </returns>
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
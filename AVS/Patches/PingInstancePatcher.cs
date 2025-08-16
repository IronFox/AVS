using HarmonyLib;

// PURPOSE: Force a fix on PingInstances that have a null origin.
// VALUE: High, unfortunately. If GetPosition has an error, it causes subsequent ping instances to not be displayed, which looks like a AVS bug. See uGUI_Pings.UpdatePings for more.

namespace AVS.Patches
{
    /// <summary>
    /// A Harmony patch class that modifies the behavior of the PingInstance class.
    /// Specifically, addresses the issue where PingInstances contain null origins, which can lead to errors
    /// in the display of ping sprites. This patch ensures that such instances are corrected by defaulting the origin
    /// to the PingInstance's own transform.
    /// </summary>
    [HarmonyPatch(typeof(PingInstance))]
    public class PingInstancePatcher
    {
        /// <summary>
        /// Harmony prefix method for the <c>PingInstance.GetPosition</c> method. This prefix
        /// ensures that if the <c>origin</c> property of a PingInstance is <c>null</c>,
        /// it is set to the PingInstance's own transform. This prevents issues where
        /// ping sprites fail to display due to a null origin.
        /// </summary>
        /// <param name="__instance">The PingInstance being patched.</param>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(PingInstance.GetPosition))]
        public static void PingInstanceGetPositionPrefix(PingInstance __instance)
        {
            if (__instance.origin == null)
            {
                __instance.origin = __instance.transform;
                Logger.Warn($"Found null origin for ping instance on object: {__instance.name}. Setting origin to itself. Otherwise, some ping sprites would not be displayed.");
            }
        }
    }
}

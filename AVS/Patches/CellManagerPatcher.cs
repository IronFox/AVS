using AVS.BaseVehicle;
using HarmonyLib;
using System.Collections;
using UnityEngine;

// PURPOSE: Resolve an out-of-time error
// VALUE: Dubious. Maybe it doesn't matter, but it seems like it would have an effect on the ProtoSerialization methods. So it could be important.

namespace AVS.Patches
{
    /// <summary>
    /// Provides patches for the CellManager class to handle specific anomalies or edge cases.
    /// </summary>
    /// <remarks>
    /// The primary purpose of this patch is to address a potential issue where the streamer or globalRoot
    /// properties of the CellManager instance might be null during the invocation of certain methods.
    /// This could lead to unexpected errors in scenarios where registering global entities is performed.
    /// </remarks>
    [HarmonyPatch(typeof(CellManager))]
    public static class CellManagerPatcher
    {
        /// <summary>
        /// Handles the registration of a global entity within the CellManager.
        /// Ensures that the proper parent is set even when the streamer or globalRoot properties are null
        /// during the registration process.
        /// </summary>
        /// <param name="__instance">The instance of the CellManager performing the global entity registration.</param>
        /// <param name="ent">The entity being registered as a global entity.</param>
        /// <returns>
        /// Returns true if the registration process can continue normally. Returns false if the parent-setting
        /// process needs to be deferred due to null streamer or globalRoot properties.
        /// </returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CellManager.RegisterGlobalEntity))]
        public static bool RegisterGlobalEntityPostfix(CellManager __instance, GameObject ent)
        {
            var v = ent.GetComponent<AvsVehicle>();
            if (v == null) return true;

            if (__instance.streamer == null || __instance.streamer.globalRoot == null)
            {
                // Sometimes this function is called when streamer.globalRoot is null.
                // Not sure why or by whom.
                // All it does is set the parent, so we'll do that as soon as we possibly can.
                MainPatcher.Instance.StartCoroutine(SetParentEventually(__instance, ent));
                return false;
            }
            return true;
        }

        private static IEnumerator SetParentEventually(CellManager cellManager, GameObject ent)
        {
            yield return new WaitUntil(() => cellManager.streamer != null && cellManager.streamer.globalRoot != null);
            ent.transform.parent = cellManager.streamer.globalRoot.transform;
        }
    }
}

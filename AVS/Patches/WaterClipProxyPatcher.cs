using AVS.BaseVehicle;
using HarmonyLib;
using UnityEngine;

// PURPOSE: permit waterclipproxies with negative scaling to act in an intuitive way
// VALUE: Moderate. Not really sure this is my problem to fix.

namespace AVS.Patches
{
    /// <summary>
    /// The WaterClipProxyPatcher class is designed to apply patches to the
    /// WaterClipProxy class, enabling improved handling for instances
    /// with negative scaling. This patch ensures the behavior of
    /// WaterClipProxy objects is more intuitive and consistent in such scenarios.
    /// </summary>
    /// <remarks>
    /// This patch introduces moderate value for specific use cases where
    /// negative scaling of WaterClipProxy objects might cause unintended
    /// or non-intuitive behavior, ensuring smoother and more predictable outcomes.
    /// </remarks>
    [HarmonyPatch(typeof(WaterClipProxy))]
    class WaterClipProxyPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(WaterClipProxy.UpdateMaterial))]
        static void VehicleDockedPostfix(WaterClipProxy __instance)
        {
            AvsVehicle vehicle = __instance.GetComponentInParent<AvsVehicle>();
            if (vehicle != null)
            {
                Vector3 oldScale = __instance.transform.lossyScale;
                Vector3 newScale = new Vector3(
                    Mathf.Abs(oldScale.x),
                    Mathf.Abs(oldScale.y),
                    Mathf.Abs(oldScale.z)
                    );
                __instance.clipMaterial.SetVector(ShaderPropertyID._ObjectScale, newScale);
            }
        }
    }
}

using HarmonyLib;
using System.Collections;
using UnityEngine;

// PURPOSE: configure timeToConstruct. Broadcasts the SubConstructionBeginning signal. Manages the building fx colors.
// VALUE: Very high. Important ModVehicle and developer utilities.

namespace AVS.Patches
{
    [HarmonyPatch(typeof(VFXConstructing))]
    public static class VFXConstructingPatch
    {
        public static IEnumerator ManageColor(VFXConstructing vfx, ModVehicle mv)
        {
            if (vfx != null)
            {
                yield return new WaitUntil(() => vfx.ghostMaterial != null);
                if (mv.Config.ConstructionGhostColor != Color.black)
                {
                    Material customGhostMat = new Material(Shader.Find(Admin.Utils.marmosetUberName));
                    customGhostMat.CopyPropertiesFromMaterial(vfx.ghostMaterial);
                    vfx.ghostMaterial = customGhostMat;
                    vfx.ghostMaterial.color = mv.Config.ConstructionGhostColor;
                    vfx.ghostOverlay.material.color = mv.Config.ConstructionGhostColor;
                }
                if (mv.Config.ConstructionWireframeColor != Color.black)
                {
                    vfx.wireColor = mv.Config.ConstructionWireframeColor;
                }
            }
        }
        /*
         * This patches ensures it takes several seconds for the build-bots to build our vehicle.
         */
        [HarmonyPostfix]
        [HarmonyPatch(nameof(VFXConstructing.StartConstruction))]
        public static void StartConstructionPostfix(VFXConstructing __instance)
        {
            ModVehicle mv = __instance.GetComponent<ModVehicle>();
            if (mv != null)
            {
                __instance.timeToConstruct = mv.Config.TimeToConstruct;
                __instance.BroadcastMessage("SubConstructionBeginning", null, (UnityEngine.SendMessageOptions)1);
                __instance.SendMessageUpwards("SubConstructionBeginning", null, (UnityEngine.SendMessageOptions)1);
                UWE.CoroutineHost.StartCoroutine(ManageColor(__instance, mv));
            }
        }
    }
}

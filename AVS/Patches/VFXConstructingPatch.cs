using AVS.Util;
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
        /// <summary>
        /// Manages the visual effects (VFX) colors for a construction process, including ghost and wireframe colors.
        /// </summary>
        /// <remarks>This method updates the ghost material and wireframe colors of the provided <paramref
        /// name="vfx"/> instance based on the configuration settings in the <paramref name="mv"/> instance. If the
        /// ghost or wireframe colors in the configuration are set to <see cref="Color.black"/>, the corresponding
        /// visual effect will not be updated. The method waits until the ghost material of the <paramref name="vfx"/>
        /// instance is initialized before applying any updates.</remarks>
        /// <param name="vfx">The <see cref="VFXConstructing"/> instance representing the visual effects to be updated. Must not be null.</param>
        /// <param name="mv">The <see cref="ModVehicle"/> instance containing configuration settings for the construction process. Must
        /// not be null.</param>
        /// <returns>An enumerator that can be used to control the execution of the color management process.</returns>
        public static IEnumerator ManageColor(VFXConstructing vfx, ModVehicle mv)
        {
            if (vfx != null)
            {
                yield return new WaitUntil(() => vfx.ghostMaterial != null);
                if (mv.Config.ConstructionGhostColor != Color.black)
                {
                    Material customGhostMat = new Material(Shaders.FindMainShader());
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

        /// <summary>
        /// Modifies the construction process of a vehicle to account for custom build times and triggers related
        /// events.
        /// </summary>
        /// <remarks>This method adjusts the construction time based on the configuration of the
        /// associated <see cref="ModVehicle"/> component, if present. It also broadcasts and sends messages to notify
        /// other components of the construction process and starts a coroutine to manage additional visual effects or
        /// behaviors.</remarks>
        /// <param name="__instance">The instance of <see cref="VFXConstructing"/> being patched, representing the vehicle under construction.</param>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(VFXConstructing.StartConstruction))]
        public static void StartConstructionPostfix(VFXConstructing __instance)
        {
            ModVehicle mv = __instance.GetComponent<ModVehicle>();
            if (mv != null)
            {
                __instance.timeToConstruct = mv.Config.TimeToConstruct;
                __instance.BroadcastMessage(nameof(ModVehicle.SubConstructionBeginning), null, (UnityEngine.SendMessageOptions)1);
                __instance.SendMessageUpwards(nameof(ModVehicle.SubConstructionBeginning), null, (UnityEngine.SendMessageOptions)1);
                UWE.CoroutineHost.StartCoroutine(ManageColor(__instance, mv));
            }
        }
    }
}

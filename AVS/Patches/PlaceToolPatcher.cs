using HarmonyLib;
using UnityEngine;

// PURPOSE: ensure placed objects are permitted on and correctly parented to Submarines.
// VALUE: High.

namespace AVS.Patches
{
    /// <summary>
    /// The PlaceToolPatcher class applies patches to the PlaceTool methods using Harmony.
    /// It ensures that objects placed in the game maintain proper hierarchies and are correctly parented,
    /// especially when working with submarine-type vehicles.
    /// </summary>
    [HarmonyPatch(typeof(PlaceTool))]
    public class PlaceToolPatcher
    {
        /// <summary>
        /// Postfix method that modifies the behavior of the PlaceTool's OnPlace method.
        /// Ensures that objects placed in the game, particularly within submarine-type
        /// vehicles, are properly parented to maintain correct hierarchies.
        /// </summary>
        /// <param name="__instance">The instance of the PlaceTool being patched.</param>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PlaceTool.OnPlace))]
        public static void OnPlacePostfix(PlaceTool __instance)
        {
            SubRoot subroot = Player.main.currentSub;
            if (subroot != null && subroot.GetComponent<VehicleTypes.Submarine>())
            {
                Transform aimTransform = Builder.GetAimTransform();
                RaycastHit raycastHit = default;
                bool flag = false;
                int num = UWE.Utils.RaycastIntoSharedBuffer(aimTransform.position, aimTransform.forward, 5f, -5, QueryTriggerInteraction.UseGlobal);
                float num2 = float.PositiveInfinity;
                for (int i = 0; i < num; i++)
                {
                    RaycastHit raycastHit2 = UWE.Utils.sharedHitBuffer[i];
                    if (!raycastHit2.collider.isTrigger && !UWE.Utils.SharingHierarchy(__instance.gameObject, raycastHit2.collider.gameObject) && num2 > raycastHit2.distance)
                    {
                        flag = true;
                        raycastHit = raycastHit2;
                        num2 = raycastHit2.distance;
                    }
                }
                if (flag)
                {
                    VehicleTypes.Submarine componentInParent = raycastHit.collider.gameObject.GetComponentInParent<VehicleTypes.Submarine>();
                    __instance.transform.SetParent(componentInParent.transform);
                }
            }
        }

        /// <summary>
        /// Prefix method that modifies the behavior of the PlaceTool's LateUpdate method.
        /// Ensures proper parenting and hierarchy adjustments for objects interacting with
        /// submarine-type vehicles when the player is present.
        /// </summary>
        /// <param name="__instance">The instance of the PlaceTool being patched.</param>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(PlaceTool.LateUpdate))]
        public static void LateUpdatePrefix(PlaceTool __instance)
        {
            if (Player.main == null)
                return;

            var subroot = Player.main.currentSub;
            if (subroot != null && subroot.GetComponent<VehicleTypes.Submarine>())
            {
                if (__instance.usingPlayer != null)
                {
                    Transform aimTransform = Builder.GetAimTransform();
                    if (aimTransform == null)
                    {
                        return;
                    }
                    RaycastHit raycastHit = default;
                    bool flag = false;
                    int num = UWE.Utils.RaycastIntoSharedBuffer(aimTransform.position, aimTransform.forward, 5f, -5, QueryTriggerInteraction.UseGlobal);
                    float num2 = float.PositiveInfinity;
                    for (int i = 0; i < num; i++)
                    {
                        RaycastHit raycastHit2 = UWE.Utils.sharedHitBuffer[i];
                        if (raycastHit2.collider == null || raycastHit2.collider.gameObject == null)
                        {
                            return;
                        }
                        if (!raycastHit2.collider.isTrigger && !UWE.Utils.SharingHierarchy(__instance.gameObject, raycastHit2.collider.gameObject) && num2 > raycastHit2.distance)
                        {
                            flag = true;
                            raycastHit = raycastHit2;
                            num2 = raycastHit2.distance;
                        }
                    }
                    if (flag)
                    {
                        VehicleTypes.Submarine componentInParent = raycastHit.collider.gameObject.GetComponentInParent<VehicleTypes.Submarine>();
                        __instance.allowedOnRigidBody = componentInParent != null;
                    }
                }
            }
        }
    }
}

using AVS.Util;
using HarmonyLib;
using UnityEngine;

// PURPOSE: ensure placed objects are permitted on and correctly parented to Submarines.
// VALUE: High.

namespace AVS.Patches;

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
        var subroot = Player.main.currentSub;
        if (subroot.IsNotNull() && subroot.GetComponent<VehicleTypes.Submarine>())
        {
            var aimTransform = Builder.GetAimTransform();
            RaycastHit raycastHit = default;
            var flag = false;
            var num = UWE.Utils.RaycastIntoSharedBuffer(aimTransform.position, aimTransform.forward, 5f);
            var num2 = float.PositiveInfinity;
            for (var i = 0; i < num; i++)
            {
                var raycastHit2 = UWE.Utils.sharedHitBuffer[i];
                if (!raycastHit2.collider.isTrigger &&
                    !UWE.Utils.SharingHierarchy(__instance.gameObject, raycastHit2.collider.gameObject) &&
                    num2 > raycastHit2.distance)
                {
                    flag = true;
                    raycastHit = raycastHit2;
                    num2 = raycastHit2.distance;
                }
            }

            if (flag)
            {
                var componentInParent = raycastHit.collider.gameObject.GetComponentInParent<VehicleTypes.Submarine>();
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
        if (Player.main.IsNull())
            return;

        var subroot = Player.main.currentSub;
        if (subroot.IsNotNull() && subroot.GetComponent<VehicleTypes.Submarine>())
            if (__instance.usingPlayer.IsNotNull())
            {
                var aimTransform = Builder.GetAimTransform();
                if (aimTransform.IsNull())
                    return;
                RaycastHit raycastHit = default;
                var flag = false;
                var num = UWE.Utils.RaycastIntoSharedBuffer(aimTransform.position, aimTransform.forward, 5f);
                var num2 = float.PositiveInfinity;
                for (var i = 0; i < num; i++)
                {
                    var raycastHit2 = UWE.Utils.sharedHitBuffer[i];
                    if (raycastHit2.collider.IsNull() || raycastHit2.collider.gameObject.IsNull())
                        return;
                    if (!raycastHit2.collider.isTrigger &&
                        !UWE.Utils.SharingHierarchy(__instance.gameObject, raycastHit2.collider.gameObject) &&
                        num2 > raycastHit2.distance)
                    {
                        flag = true;
                        raycastHit = raycastHit2;
                        num2 = raycastHit2.distance;
                    }
                }

                if (flag)
                {
                    var componentInParent =
                        raycastHit.collider.gameObject.GetComponentInParent<VehicleTypes.Submarine>();
                    __instance.allowedOnRigidBody = componentInParent.IsNotNull();
                }
            }
    }
}
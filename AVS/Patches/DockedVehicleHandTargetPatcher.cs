using AVS.BaseVehicle;
using AVS.Localization;
using AVS.Log;
using AVS.Util;
using AVS.Util.Math;
using AVS.VehicleTypes;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

// PURPOSE: allow docked vehicles to be hovered and clicked in the expected ways
// VALUE: High.

namespace AVS.Patches;

/// <summary>
/// A patch class designed to modify the behavior of the DockedVehicleHandTarget class.
/// This patch ensures the expected interaction with docked vehicles, including hover and click functionalities,
/// enabling appropriate feedback and operations.
/// </summary>
/// <remarks>
/// This class utilizes the Harmony library to inject additional behavior into the DockedVehicleHandTarget's methods
/// through postfix and prefix patches, enhancing the functionality of vehicle docking and undocking processes.
/// </remarks>
[HarmonyPatch(typeof(DockedVehicleHandTarget))]
public static class DockedVehicleHandTargetPatch
{
    /// <summary>
    /// Transpiles the instructions of the OnHandHover method in the DockedVehicleHandTarget class to inject
    /// custom behavior by modifying certain code instructions. This allows enhanced interaction with docked vehicles,
    /// making the method more general and flexible in its functionality.
    /// </summary>
    /// <param name="instructions">An enumerable collection of original IL code instructions to be transpiled.</param>
    /// <returns>A modified enumerable collection of IL code instructions with the custom changes applied.</returns>
    [HarmonyPatch(nameof(DockedVehicleHandTarget.OnHandHover))]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        using var log = SmartLog.LazyForAVS(RootModController.AnyInstance);

        var codes = new List<CodeInstruction>(instructions);
        var origianalCount = codes.Count;
        int insertAt = codes.FindIndex(c =>
            c.opcode == OpCodes.Callvirt && c.operand.ToString().ToLower().Contains("energymixin") && c.operand.ToString().ToLower().Contains("getcomponent"));
        if (insertAt == -1)
        {
            log.Warn("Could not find target instruction to insert after; attempting fallback search.");
            insertAt = codes.FindIndex(c =>
                c.operand.ToString().ToLower().Contains("energymixin"));
        }
        if (insertAt >= 0)
        {
            // (re)load vehicle (this) to end of stack. 
            var c0 = new CodeInstruction(OpCodes.Ldloc_0);
            codes.Insert(insertAt + 1, c0);
            // Stack now expected: energyMixin, vehicle
            var c1 = CodeInstruction.Call(typeof(AvsVehicle), nameof(AvsVehicle.GetEnergyMixinFromVehicle));
            codes.Insert(insertAt + 2, c1);
            // Stack now expected: energyMixin
            log.Debug(() => $"Inserted instructions at #{insertAt + 1}/{origianalCount}:");
            log.Debug(() => $"    {c0}");
            log.Debug(() => $"    {c1}");
        }
        else
            log.Warn("Could not find target instruction to insert after; no changes made.");

        return codes;
    }

    /// <summary>
    /// Executes additional behavior and modifications after the OnHandHover method is invoked on a DockedVehicleHandTarget instance.
    /// This method is used to enhance the interaction with docked vehicles, such as displaying additional information or altering behavior.
    /// </summary>
    /// <param name="__instance">The instance of DockedVehicleHandTarget on which the OnHandHover method was called.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(DockedVehicleHandTarget.OnHandHover))]
    public static void OnHandHoverPostfix(DockedVehicleHandTarget __instance)
    {
        var av = __instance.dockingBay.GetDockedVehicle() as AvsVehicle;
        if (av.IsNotNull())
        {
            var text = av.subName.hullName.text;
            if (av is Submarine sub && sub.Com.Hatches.Count > 0)
                text = sub.GetClosestEntryHatch().Hatch.GetComponent<VehicleComponents.VehicleHatch>().EnterHint;
            float energyActual = 0;
            float energyMax = 0;
            foreach (var battery in av.energyInterface.sources)
            {
                energyActual += battery.charge;
                energyMax += battery.capacity;
            }

            var energyFraction = energyActual / energyMax;
            if (energyFraction == 1)
            {
                var format2 = Translator.GetFormatted(TranslationKey.HandHover_Docked_StatusCharged,
                    av.liveMixin.Percentage());
                HandReticle.main.SetText(HandReticle.TextType.Hand, text, true, GameInput.Button.LeftHand);
                HandReticle.main.SetText(HandReticle.TextType.HandSubscript, format2, false, GameInput.Button.None);
            }
            else
            {
                var format2 = Translator.GetFormatted(TranslationKey.HandHover_Docked_StatusCharging,
                    av.liveMixin.Percentage(), energyActual.Percentage(energyMax));
                HandReticle.main.SetText(HandReticle.TextType.Hand, text, true, GameInput.Button.LeftHand);
                HandReticle.main.SetText(HandReticle.TextType.HandSubscript, format2, false, GameInput.Button.None);
            }

            HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
        }
    }


    /// <summary>
    /// Handles the prefix logic for the OnHandClick method in DockedVehicleHandTarget, allowing for custom behavior during the interaction with a docked vehicle.
    /// This method ensures undocking clearance, triggers undocking animations, and updates environmental states as necessary.
    /// </summary>
    /// <param name="__instance">The instance of the DockedVehicleHandTarget being interacted with.</param>
    /// <param name="hand">The GUIHand instance interacting with the DockedVehicleHandTarget.</param>
    /// <returns>A boolean indicating whether to proceed with the original method's execution. Returning <c>false</c> skips the original method.</returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(DockedVehicleHandTarget.OnHandClick))]
    public static bool OnHandClickPrefix(DockedVehicleHandTarget __instance, GUIHand hand)
    {
        var av = __instance.dockingBay.GetDockedVehicle() as AvsVehicle;
        if (av.IsNull())
            return true;

        if (!__instance.dockingBay.HasUndockingClearance())
            return false;

        var rmc = av.Owner;

        __instance.dockingBay.OnUndockingStart();
        __instance.dockingBay.subRoot.BroadcastMessage("OnLaunchBayOpening", SendMessageOptions.DontRequireReceiver);

        av.IsUndockingAnimating = true;
        var subRootName = __instance.dockingBay.subRoot.name.ToLower();
        var moonpoolMaybe = __instance.dockingBay.transform.parent.SafeGetParent();
        if (subRootName.Contains("cyclops"))
        {
            __instance.dockingBay.transform.parent.parent.parent.Find("CyclopsCollision").gameObject.SetActive(false);
        }
        else
        {
            if (moonpoolMaybe.IsNotNull() &&
                moonpoolMaybe.name.Equals("BaseMoonpool(Clone)", StringComparison.OrdinalIgnoreCase))
                moonpoolMaybe.Find("Collisions").gameObject.SetActive(false);
        }

        Player.main.SetCurrentSub(null, false);
        if (__instance.dockingBay.dockedVehicle.IsNotNull())
        {
            rmc.StartAvsCoroutine(
                nameof(Vehicle) + '.' + nameof(Vehicle.Undock),
                _ => __instance.dockingBay.dockedVehicle.Undock(Player.main, __instance.dockingBay.transform.position.y));
            SkyEnvironmentChanged.Broadcast(__instance.dockingBay.dockedVehicle.gameObject, (GameObject?)null);
        }

        __instance.dockingBay.dockedVehicle = null;

        av.IsUndockingAnimating = false;
        if (subRootName.Contains("cyclops"))
        {
            IEnumerator ReEnableCollisionsInAMoment()
            {
                yield return new WaitForSeconds(5);
                __instance.dockingBay.transform.parent.parent.parent.Find("CyclopsCollision").gameObject
                    .SetActive(true);
                av.useRigidbody.detectCollisions = true;
            }

            rmc.StartAvsCoroutine(
                nameof(DockedVehicleHandTargetPatch) + '.' + nameof(ReEnableCollisionsInAMoment),
                _ => ReEnableCollisionsInAMoment());
        }
        else
        {
            float GetVehicleTop()
            {
                if (av.Com.BoundingBoxCollider.IsNull())
                    return av.transform.position.y + av.transform.lossyScale.y * 0.5f;
                var worldCenter =
                    av.Com.BoundingBoxCollider.transform.TransformPoint(av.Com.BoundingBoxCollider.center);
                return worldCenter.y + av.Com.BoundingBoxCollider.size.y * 0.5f *
                    av.Com.BoundingBoxCollider.transform.lossyScale.y;
            }

            float GetMoonPoolPlane()
            {
                return moonpoolMaybe.Find("Flood_BaseMoonPool/x_BaseWaterPlane").transform.position.y;
            }

            IEnumerator ReEnableCollisionsInAMoment()
            {
                while (GetMoonPoolPlane() < GetVehicleTop())
                    yield return new WaitForEndOfFrame();
                moonpoolMaybe.Find("Collisions").gameObject.SetActive(true);
                av.useRigidbody.detectCollisions = true;
            }

            if (moonpoolMaybe.IsNotNull() &&
                moonpoolMaybe.name.Equals("BaseMoonpool(Clone)", StringComparison.OrdinalIgnoreCase))
                rmc.StartAvsCoroutine(
                    nameof(DockedVehicleHandTargetPatch) + '.' + nameof(ReEnableCollisionsInAMoment),
                    _ => ReEnableCollisionsInAMoment());
        }

        SkyEnvironmentChanged.Broadcast(av.gameObject, (GameObject?)null);
        av.UndockVehicle();


        return false;
    }
}
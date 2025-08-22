using AVS.BaseVehicle;
using AVS.Localization;
using AVS.Util;
using AVS.VehicleTypes;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        var codes = new List<CodeInstruction>(instructions);
        var newCodes = new List<CodeInstruction>(codes.Count);
        var myNOP = new CodeInstruction(OpCodes.Nop);
        for (var i = 0; i < codes.Count; i++)
            newCodes.Add(myNOP);
        for (var i = 0; i < codes.Count; i++)
            if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand.ToString().ToLower().Contains("energymixin"))
                newCodes[i] = CodeInstruction.Call(typeof(AvsVehicle), nameof(AvsVehicle.GetEnergyMixinFromVehicle));
            else
                newCodes[i] = codes[i];

        return newCodes.AsEnumerable();
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
        var mv = __instance.dockingBay.GetDockedVehicle() as AvsVehicle;
        if (mv.IsNotNull())
        {
            var text = mv.subName.hullName.text;
            if (mv is Submarine sub && sub.Com.Hatches.Count > 0)
                text = sub.GetClosestEntryHatch().Hatch.GetComponent<VehicleComponents.VehicleHatch>().EnterHint;
            float energyActual = 0;
            float energyMax = 0;
            foreach (var battery in mv.energyInterface.sources)
            {
                energyActual += battery.charge;
                energyMax += battery.capacity;
            }

            var energyFraction = energyActual / energyMax;
            if (energyFraction == 1)
            {
                var format2 = Translator.GetFormatted(TranslationKey.HandHover_Docked_StatusCharged,
                    mv.liveMixin.Percentage());
                HandReticle.main.SetText(HandReticle.TextType.Hand, text, true, GameInput.Button.LeftHand);
                HandReticle.main.SetText(HandReticle.TextType.HandSubscript, format2, false, GameInput.Button.None);
            }
            else
            {
                var format2 = Translator.GetFormatted(TranslationKey.HandHover_Docked_StatusCharging,
                    mv.liveMixin.Percentage(), energyActual.Percentage(energyMax));
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
        var mv = __instance.dockingBay.GetDockedVehicle() as AvsVehicle;
        if (mv.IsNull())
            return true;

        if (!__instance.dockingBay.HasUndockingClearance())
            return false;

        __instance.dockingBay.OnUndockingStart();
        __instance.dockingBay.subRoot.BroadcastMessage("OnLaunchBayOpening", SendMessageOptions.DontRequireReceiver);

        mv.IsUndockingAnimating = true;
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
            MainPatcher.Instance.StartCoroutine(
                __instance.dockingBay.dockedVehicle.Undock(Player.main, __instance.dockingBay.transform.position.y));
            SkyEnvironmentChanged.Broadcast(__instance.dockingBay.dockedVehicle.gameObject, (GameObject?)null);
        }

        __instance.dockingBay.dockedVehicle = null;

        mv.IsUndockingAnimating = false;
        if (subRootName.Contains("cyclops"))
        {
            IEnumerator ReEnableCollisionsInAMoment()
            {
                yield return new WaitForSeconds(5);
                __instance.dockingBay.transform.parent.parent.parent.Find("CyclopsCollision").gameObject
                    .SetActive(true);
                mv.useRigidbody.detectCollisions = true;
            }

            MainPatcher.Instance.StartCoroutine(ReEnableCollisionsInAMoment());
        }
        else
        {
            float GetVehicleTop()
            {
                if (mv.Com.BoundingBoxCollider.IsNull())
                    return mv.transform.position.y + mv.transform.lossyScale.y * 0.5f;
                var worldCenter =
                    mv.Com.BoundingBoxCollider.transform.TransformPoint(mv.Com.BoundingBoxCollider.center);
                return worldCenter.y + mv.Com.BoundingBoxCollider.size.y * 0.5f *
                    mv.Com.BoundingBoxCollider.transform.lossyScale.y;
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
                mv.useRigidbody.detectCollisions = true;
            }

            if (moonpoolMaybe.IsNotNull() &&
                moonpoolMaybe.name.Equals("BaseMoonpool(Clone)", StringComparison.OrdinalIgnoreCase))
                MainPatcher.Instance.StartCoroutine(ReEnableCollisionsInAMoment());
        }

        SkyEnvironmentChanged.Broadcast(mv.gameObject, (GameObject?)null);
        mv.UndockVehicle();


        return false;
    }
}
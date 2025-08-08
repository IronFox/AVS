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

namespace AVS.Patches
{
    [HarmonyPatch(typeof(DockedVehicleHandTarget))]
    public static class DockedVehicleHandTargetPatch
    {
        /* This transpiler makes one part of OnHandHover more generic
         * Optionally change GetComponent to GetComponentInChildren
         * Simple as
         */
        [HarmonyPatch(nameof(DockedVehicleHandTarget.OnHandHover))]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            List<CodeInstruction> newCodes = new List<CodeInstruction>(codes.Count);
            CodeInstruction myNOP = new CodeInstruction(OpCodes.Nop);
            for (int i = 0; i < codes.Count; i++)
            {
                newCodes.Add(myNOP);
            }
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand.ToString().ToLower().Contains("energymixin"))
                {
                    newCodes[i] = CodeInstruction.Call(typeof(AvsVehicle), nameof(AvsVehicle.GetEnergyMixinFromVehicle));
                }
                else
                {
                    newCodes[i] = codes[i];
                }
            }
            return newCodes.AsEnumerable();
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(DockedVehicleHandTarget.OnHandHover))]
        public static void OnHandHoverPostfix(DockedVehicleHandTarget __instance)
        {
            var mv = __instance.dockingBay.GetDockedVehicle() as AvsVehicle;
            if (mv != null)
            {
                string text = mv.subName.hullName.text;
                if (mv is Submarine sub && sub.Com.Hatches.Count > 0)
                {
                    text = sub.GetClosestEntryHatch().Hatch.GetComponent<VehicleHatch>().EnterHint;
                }
                float energyActual = 0;
                float energyMax = 0;
                foreach (var battery in mv.energyInterface.sources)
                {
                    energyActual += battery.charge;
                    energyMax += battery.capacity;
                }
                float energyFraction = energyActual / energyMax;
                if (energyFraction == 1)
                {
                    string format2 = Translator.GetFormatted(TranslationKey.HandHover_Docked_StatusCharged, mv.liveMixin.Percentage());
                    HandReticle.main.SetText(HandReticle.TextType.Hand, text, true, GameInput.Button.LeftHand);
                    HandReticle.main.SetText(HandReticle.TextType.HandSubscript, format2, false, GameInput.Button.None);
                }
                else
                {
                    string format2 = Translator.GetFormatted(TranslationKey.HandHover_Docked_StatusCharging, mv.liveMixin.Percentage(), energyActual.Percentage(energyMax));
                    HandReticle.main.SetText(HandReticle.TextType.Hand, text, true, GameInput.Button.LeftHand);
                    HandReticle.main.SetText(HandReticle.TextType.HandSubscript, format2, false, GameInput.Button.None);
                }
                HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(nameof(DockedVehicleHandTarget.OnHandClick))]
        public static bool OnHandClickPrefix(DockedVehicleHandTarget __instance, GUIHand hand)
        {
            var mv = __instance.dockingBay.GetDockedVehicle() as AvsVehicle;
            if (mv == null)
            {
                return true;
            }

            if (!__instance.dockingBay.HasUndockingClearance())
            {
                return false;
            }

            __instance.dockingBay.OnUndockingStart();
            __instance.dockingBay.subRoot.BroadcastMessage("OnLaunchBayOpening", SendMessageOptions.DontRequireReceiver);

            mv.IsUndockingAnimating = true;
            string subRootName = __instance.dockingBay.subRoot.name.ToLower();
            var moonpoolMaybe = __instance.dockingBay.transform.parent.SafeGetParent();
            if (subRootName.Contains("cyclops"))
            {
                __instance.dockingBay.transform.parent.parent.parent.Find("CyclopsCollision").gameObject.SetActive(false);
            }
            else
            {
                if (moonpoolMaybe != null && moonpoolMaybe.name.Equals("BaseMoonpool(Clone)", StringComparison.OrdinalIgnoreCase))
                {
                    moonpoolMaybe.Find("Collisions").gameObject.SetActive(false);
                }
            }
            Player.main.SetCurrentSub(null, false);
            if (__instance.dockingBay.dockedVehicle != null)
            {
                UWE.CoroutineHost.StartCoroutine(__instance.dockingBay.dockedVehicle.Undock(Player.main, __instance.dockingBay.transform.position.y));
                SkyEnvironmentChanged.Broadcast(__instance.dockingBay.dockedVehicle.gameObject, (GameObject?)null);
            }
            __instance.dockingBay.dockedVehicle = null;

            mv.IsUndockingAnimating = false;
            if (subRootName.Contains("cyclops"))
            {
                IEnumerator ReEnableCollisionsInAMoment()
                {
                    yield return new WaitForSeconds(5);
                    __instance.dockingBay.transform.parent.parent.parent.Find("CyclopsCollision").gameObject.SetActive(true);
                    mv.useRigidbody.detectCollisions = true;
                }
                UWE.CoroutineHost.StartCoroutine(ReEnableCollisionsInAMoment());
            }
            else
            {
                float GetVehicleTop()
                {
                    if (mv.Com.BoundingBoxCollider == null)
                    {
                        return mv.transform.position.y + (mv.transform.lossyScale.y * 0.5f);
                    }
                    Vector3 worldCenter = mv.Com.BoundingBoxCollider.transform.TransformPoint(mv.Com.BoundingBoxCollider.center);
                    return worldCenter.y + (mv.Com.BoundingBoxCollider.size.y * 0.5f * mv.Com.BoundingBoxCollider.transform.lossyScale.y);
                }
                float GetMoonPoolPlane()
                {
                    return moonpoolMaybe.Find("Flood_BaseMoonPool/x_BaseWaterPlane").transform.position.y;
                }
                IEnumerator ReEnableCollisionsInAMoment()
                {
                    while (GetMoonPoolPlane() < GetVehicleTop())
                    {
                        yield return new WaitForEndOfFrame();
                    }
                    moonpoolMaybe.Find("Collisions").gameObject.SetActive(true);
                    mv.useRigidbody.detectCollisions = true;
                }
                if (moonpoolMaybe != null && moonpoolMaybe.name.Equals("BaseMoonpool(Clone)", StringComparison.OrdinalIgnoreCase))
                {
                    UWE.CoroutineHost.StartCoroutine(ReEnableCollisionsInAMoment());
                }
            }
            SkyEnvironmentChanged.Broadcast(mv.gameObject, (GameObject?)null);
            mv.UndockVehicle();


            return false;
        }
    }
}

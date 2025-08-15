using AVS.BaseVehicle;
using AVS.Localization;
using AVS.Util;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

// PURPOSE: generally ensures AvsVehicles behave like normal Vehicles
// VALUE: Very high.

namespace AVS
{
    [HarmonyPatch(typeof(Vehicle))]
    public class VehiclePatcher
    {
        /*
         * This collection of patches generally ensures our AvsVehicles behave like normal Vehicles.
         * Each will be commented if necessary
         */

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Vehicle.OnHandHover))]
        public static bool OnHandHoverPrefix(Vehicle __instance)
        {
            var mv = __instance as AvsVehicle;
            if (mv == null)
            {
                //if (VehicleTypes.Drone.mountedDrone != null)
                //{
                //    return false;
                //}
            }
            else
            {
                if (mv.isScuttled)
                {
                    float now = mv.GetComponent<Sealed>().openedAmount;
                    float max = mv.GetComponent<Sealed>().maxOpenedAmount;
                    var percent = now.Percentage(max);
                    HandReticle.main.SetText(HandReticle.TextType.Hand, Translator.GetFormatted(TranslationKey.HandHover_Vehicle_DeconstructionPercent, percent), true, GameInput.Button.None);
                }
                else if (mv.IsBoarded)
                {
                    HandReticle.main.SetText(HandReticle.TextType.Hand, "", true, GameInput.Button.None);
                }
                else
                {
                    HandReticle.main.SetText(HandReticle.TextType.Hand, mv.GetName(), true, GameInput.Button.None);
                }
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Vehicle.ApplyPhysicsMove))]
        private static bool ApplyPhysicsMovePrefix(Vehicle __instance, ref bool ___wasAboveWater, ref VehicleAccelerationModifier[] ___accelerationModifiers)
        {
            var mv = __instance as AvsVehicle;
            if (mv != null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Vehicle.LazyInitialize))]
        public static bool LazyInitializePrefix(Vehicle __instance, ref EnergyInterface ___energyInterface)
        {
            var mv = __instance as AvsVehicle;
            if (mv == null)
            {
                return true;
            }

            ___energyInterface = __instance.gameObject.GetComponent<EnergyInterface>();
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Vehicle.GetAllStorages))]
        public static void GetAllStoragesPostfix(Vehicle __instance, ref List<IItemsContainer> containers)
        {
            var mv = __instance as AvsVehicle;
            if (mv == null)
            {
                return;
            }

            foreach (var tmp in ((AvsVehicle)__instance).Com.InnateStorages)
            {
                var ic = tmp.Container.GetComponent<InnateStorageContainer>();
                if (ic != null)
                    containers.Add(ic.Container);
            }

        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Vehicle.IsPowered))]
        public static void IsPoweredPostfix(Vehicle __instance, ref EnergyInterface ___energyInterface, ref bool __result)
        {
            var mv = __instance as AvsVehicle;
            if (mv == null)
            {
                return;
            }
            if (!mv.IsPoweredOn)
            {
                __result = false;
            }
        }

        [HarmonyPatch(nameof(Vehicle.Update))]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            /* This is basically a prefix for Vehicle.Update,
             * but we choose to transpile instead,
             * so that our code may be considered "core."
             * That is, it will be skipped if any other Prefix returns false.
             * This is desirable to be as "alike" normal Vehicles as possible;
             * in particular, this ensures compatibility with FreeLook
             * We must control our AvsVehicle rotation within the core Vehicle.Update code.
             */
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            List<CodeInstruction> newCodes = new List<CodeInstruction>(codes.Count + 2);
            CodeInstruction myNOP = new CodeInstruction(OpCodes.Nop);
            for (int i = 0; i < codes.Count + 2; i++)
            {
                newCodes.Add(myNOP);
            }
            // push reference to vehicle
            // Call a static function which takes a vehicle and ControlsRotation if it's a AvsVehicle
            newCodes[0] = new CodeInstruction(OpCodes.Ldarg_0);
            newCodes[1] = CodeInstruction.Call(typeof(AvsVehicle), nameof(AvsVehicle.MaybeControlRotation));
            for (int i = 0; i < codes.Count; i++)
            {
                newCodes[i + 2] = codes[i];
            }
            return newCodes.AsEnumerable();
        }


        [HarmonyPrefix]
        [HarmonyPatch(nameof(Vehicle.ReAttach))]
        public static bool VehicleReAttachPrefix(Vehicle __instance)
        {
            IEnumerator NotifyDockingBay(Transform baseCell)
            {
                if (baseCell == null)
                {
                    yield break;
                }
                yield return new WaitUntil(() => baseCell.Find("BaseMoonpool(Clone)") != null);
                var thisBasesBays = baseCell.SafeGetComponentsInChildren<VehicleDockingBay>(true);
                if (thisBasesBays != null)
                {
                    const float expectedMaxDistance = 5f;
                    foreach (VehicleDockingBay bay in thisBasesBays)
                    {
                        if (bay != null && Vector3.Distance(__instance.transform.position, bay.transform.position) < expectedMaxDistance)
                        {
                            bay.DockVehicle(__instance, false);
                        }
                    }
                }
            }
            MainPatcher.Instance.StartCoroutine(NotifyDockingBay(__instance.transform.parent.Find("BaseCell(Clone)")));
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Vehicle.Awake))]
        public static void VehicleAwakeHarmonyPostfix(Vehicle __instance)
        {
            Admin.GameObjectManager<Vehicle>.Register(__instance);
        }
    }

    [HarmonyPatch(typeof(Vehicle))]
    public class VehiclePatcher2
    {
        /* This transpiler makes one part of UpdateEnergyRecharge more generic
         * Optionally change GetComponentInParent to GetComponentInParentButNotInMe
         * Simple as.
         * The purpose is to ensure AvsVehicles are recharged while docked.
         */
        [HarmonyPatch(nameof(Vehicle.UpdateEnergyRecharge))]
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
                if (codes[i].opcode == OpCodes.Call && codes[i].operand.ToString().ToLower().Contains("powerrelay"))
                {
                    newCodes[i] = CodeInstruction.Call(typeof(VehiclePatcher2), nameof(VehiclePatcher2.GetPowerRelayAboveVehicle));
                }
                else
                {
                    newCodes[i] = codes[i];
                }
            }
            return newCodes.AsEnumerable();
        }
        public static PowerRelay GetPowerRelayAboveVehicle(Vehicle veh)
        {
            if ((veh as AvsVehicle) == null)
            {
                return veh.GetComponentInParent<PowerRelay>();
            }
            else
            {
                return veh.transform.parent.gameObject.GetComponentInParent<PowerRelay>();
            }
        }
    }
}

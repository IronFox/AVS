using AVS.BaseVehicle;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

// PURPOSE: Prevent AvsVehicles from entering "moon gates"
// VALUE: high, for the sake of world consistency

namespace AVS.Patches
{
    internal class BlockAvsVehicle : MonoBehaviour
    {
        private readonly Dictionary<AvsVehicle, int> MVs = new Dictionary<AvsVehicle, int>();
        internal void FixedUpdate()
        {
            MVs.ForEach(x => x.Key.useRigidbody.AddForce(transform.forward * 3f, ForceMode.VelocityChange));
        }
        internal void OnTriggerEnter(Collider other)
        {
            AvsVehicle mv = other.GetComponentInParent<AvsVehicle>();
            if (mv == null)
            {
                return;
            }
            if (MVs.ContainsKey(mv))
            {
                MVs[mv]++;
            }
            else
            {
                MVs.Add(mv, 1);
            }
        }
        internal void OnTriggerExit(Collider other)
        {
            AvsVehicle mv = other.GetComponentInParent<AvsVehicle>();
            if (mv == null)
            {
                return;
            }
            MVs[mv]--;
            if (MVs[mv] <= 0)
            {
                MVs.Remove(mv);
            }
        }
    }


    /* 
     * Prevent AvsVehicles from entering "moon gates"
     * which are vertical force fields that prevent seamoth entry.
     * There's one at "prison" and one at "lavacastlebase"
     */
    [HarmonyPatch(typeof(BlockSeamoth))]
    public class BlockSeamothPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(BlockSeamoth.FixedUpdate))]
        public static void BlockSeamothFixedUpdatePostfix(BlockSeamoth __instance)
        {
            if (__instance.GetComponent<BlockAvsVehicle>() == null)
            {
                __instance.gameObject.AddComponent<BlockAvsVehicle>();
            }
        }
    }
}

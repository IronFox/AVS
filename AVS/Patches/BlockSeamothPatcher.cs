using AVS.BaseVehicle;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

// PURPOSE: Prevent AvsVehicles from entering "moon gates"
// VALUE: high, for the sake of world consistency

namespace AVS.Patches
{
    /// <summary>
    /// A component designed to prevent AVS vehicles from entering specific restricted areas referred to as "moon gates."
    /// </summary>
    /// <remarks>
    /// This class monitors the entry and exit of AVS vehicles into a trigger zone defined by its collider.
    /// It applies force to prevent the vehicles from progressing further while maintaining consistency in gameplay or world design.
    /// </remarks>
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

}

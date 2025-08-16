using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// PURPOSE: allows Submarines to specify volumes in which things cannot be built or placed
// VALUE: moderate, as a developer utility

namespace AVS.Patches
{
    /// <summary>
    /// A Harmony patch class designed for modifying the behavior of the Builder class.
    /// Specifically tailored for allowing submarines to specify volumes in which
    /// construction or placement of objects is prohibited.
    /// </summary>
    [HarmonyPatch(typeof(Builder))]
    public class BuilderPatcher
    {
        // This patch allows Submarines to specify volumes in which things cannot be built or placed
        /// <summary>
        /// Postfix method that modifies the behavior of the Builder's CheckAsSubModule method.
        /// Ensures that submarines can define specific volumes where object construction or placement
        /// is restricted based on bounding box and collider checks.
        /// </summary>
        /// <param name="__result">
        /// A boolean value indicating whether the construction or placement is allowed.
        /// This is modified to false if certain conditions (e.g., overlapping denial zones) are met.
        /// </param>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Builder.CheckAsSubModule))]
        public static void BuilderCheckAsSubModulePostfix(ref bool __result)
        {
            if (!(Player.main.GetAvsVehicle() is VehicleTypes.Submarine))
            {
                return;
            }

            foreach (var t in Builder.bounds)
            {
                OrientedBounds orientedBounds = t;
                if (orientedBounds.rotation.IsDistinguishedIdentity())
                {
                    orientedBounds.rotation = Quaternion.identity;
                }
                orientedBounds.position = Builder.placePosition + Builder.placeRotation * orientedBounds.position;
                orientedBounds.rotation = Builder.placeRotation * orientedBounds.rotation;
                if (orientedBounds.extents is { x: > 0f, y: > 0f, z: > 0f })
                {
                    List<Collider> outputColliders = new List<Collider>();
                    Builder.GetOverlappedColliders(orientedBounds.position, orientedBounds.rotation, orientedBounds.extents, Builder.placeLayerMask.value, QueryTriggerInteraction.Collide, outputColliders);
                    if (outputColliders.Any(x => x.CompareTag(Builder.denyBuildingTag)))
                    {
                        __result = false;
                    }
                }
            }
        }
    }
}

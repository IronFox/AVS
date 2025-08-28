using AVS.BaseVehicle;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// PURPOSE: AvsVehicles have an awareness of nearby leviathans
// VALUE: Moderate. Allows for some very cool/scary moments.

namespace AVS.Patches
{
    /// <summary>
    /// CreaturePatcher is a class designed to modify the behavior of in-game creatures
    /// by introducing awareness of nearby leviathan-class entities specifically for
    /// vehicles derived from AvsVehicle. This patch enhances gameplay by enabling interactions
    /// with the environment when dangerous creatures are within a specific range.
    /// </summary>
    /// <remarks>
    /// The class operates as a Harmony patch applied to the Creature class, particularly
    /// targeting the ChooseBestAction method. It allows vehicles implementing the IVehicleStatusListener
    /// to respond to proximity of certain leviathan classes, creating dynamic and immersive experiences
    /// during gameplay.
    /// </remarks>
    /// <example>
    /// No example code provided.
    /// </example>
    [HarmonyPatch(typeof(Creature))]
    [HarmonyPatch(nameof(Creature.ChooseBestAction))]
    class CreaturePatcher
    {
        private static readonly IReadOnlyList<string> LeviathanNames = [
            "GhostLeviathan",
            "ReaperLeviathan",
            "SeaDragon",
            "BlazaLeviathan",
            "AbyssalBlaza",
            "Bloop",
            "DeepBloop",
            "GrandBloop",
            "AncientBloop",
            "GulperLeviathan",
            "GulperLeviathanJuvenile",
            "GargantuanVoid",
            "GargantuanJuvenile",
            "AbyssalBlaza",
            "AnglerFish",
        ];

        /// <summary>
        /// Postfix method for the ChooseBestAction method in the Creature class.
        /// </summary>
        /// <param name="__instance">The instance of the Creature being patched.</param>
        /// <remarks>
        /// This method is designed to modify the behavior of the ChooseBestAction method
        /// in the Creature class. It allows vehicles implementing the IVehicleStatusListener
        /// to respond to proximity of certain leviathan classes, creating dynamic and immersive experiences
        /// during gameplay.
        /// </remarks>
        [HarmonyPostfix]
        public static void Postfix(Creature __instance)
        {
            if (!(Player.main.GetVehicle() is AvsVehicle av)) return;

            if (Vector3.Distance(Player.main.transform.position, __instance.transform.position) > 150) return;

            // react to nearby dangerous leviathans
            if (LeviathanNames.Any(x => __instance.name.Contains(x)))
            {
                foreach (var component in Player.main.GetVehicle().GetComponentsInChildren<IVehicleStatusListener>())
                {
                    component.OnNearbyLeviathan();
                }
            }
        }
    }
}

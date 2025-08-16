using HarmonyLib;
using UnityEngine;

// PURPOSE: ensure the Silence doesn't kill us in a way that softlocks the game
// VALUE: High, unfortunately

namespace AVS.Patches.CompatibilityPatches
{
    /// <summary>
    /// A Harmony patch class for modifying the behavior of the <see cref="Player"/> class
    /// in specific contexts involving the Silence and associated vehicles, to prevent
    /// potential game softlocks.
    /// </summary>
    [HarmonyPatch(typeof(Player))]
    public class SilencePlayerPatcher
    {
        /// <summary>
        /// A Harmony Postfix patch for modifying the result of the <see cref="Player.CanBeAttacked"/> method.
        /// Ensures that the game does not softlock when interacting with specific vehicles
        /// associated with the Silence mechanic.
        /// </summary>
        /// <param name="__result">The original result of the <see cref="Player.CanBeAttacked"/> method,
        /// modified to allow specific vehicle interactions to properly execute.</param>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Player.CanBeAttacked))]
        public static void PlayerCanBeAttackedHarmonyPostfix(ref bool __result)
        {
            var mv = Player.main.GetAvsVehicle();
            if (mv != null)
            {
                if (GetComponentByName(mv.gameObject))
                {
                    __result = true;
                    mv.ClosestPlayerExit(true);
                }
            }
        }

        /// <summary>
        /// Determines whether a GameObject contains a component with a specific name, returning true if a match is found.
        /// Specifically checks for a component named "VehicleLock".
        /// </summary>
        /// <param name="obj">The GameObject to inspect for the presence of the specified component.</param>
        /// <returns>True if the GameObject contains a component named "VehicleLock"; otherwise, false.</returns>
        private static bool GetComponentByName(GameObject obj)
        {
            // Get all components on the GameObject
            Component[] components = obj.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp.GetType().Name == "VehicleLock")
                {
                    return true;
                }
            }
            return false; // Return false if no match is found
        }
    }
}

using AVS.BaseVehicle;
using HarmonyLib;
using System.Collections;
using System.Linq;
using UnityEngine;

// PURPOSE: allow the spawn console command to work for AvsVehicle
// VALUE: Moderate. Could register a new console command instead.

namespace AVS.Patches
{
    /// <summary>
    /// SpawnConsoleCommandPatcher is used to enhance the functionality of the existing spawn console command
    /// by ensuring compatibility with the AvsVehicle class objects. This patch enables vehicles to spawn properly
    /// and ensures related tasks such as construction are handled correctly.
    /// </summary>
    [HarmonyPatch(typeof(SpawnConsoleCommand))]
    public static class SpawnConsoleCommandPatcher
    {
        /// <summary>
        /// Harmony postfix for the SpawnConsoleCommand.OnConsoleCommand_spawn method.
        /// Enhances the spawn functionality to ensure compatibility with AvsVehicle objects.
        /// This method checks if the specified TechType corresponds to a vehicle and initiates
        /// a coroutine to handle its spawning and related tasks.
        /// </summary>
        /// <param name="__instance">The instance of the SpawnConsoleCommand executing the method.</param>
        /// <param name="n">Notification object containing data about the console command input.</param>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(SpawnConsoleCommand.OnConsoleCommand_spawn))]
        public static void OnConsoleCommand_spawnPostfix(SpawnConsoleCommand __instance, NotificationCenter.Notification n)
        {
            if (n != null && n.data != null && n.data.Count > 0)
            {
                string text = (string)n.data[0];
                if (UWE.Utils.TryParseEnum<TechType>(text, out TechType techType))
                {
                    MainPatcher.Instance.StartCoroutine(CheckSpawnForMVs(techType));
                }
            }
        }
        private static void FinishAnySpawningVehicles()
        {
            void FinishHim(AvsVehicle mv)
            {
                mv.GetComponentInChildren<VFXConstructing>(true).constructed = 90f;
                mv.GetComponentInChildren<VFXConstructing>(true).delay = 0f;
            }
            AvsVehicleManager.VehiclesInPlay
                .Where(x => x != null && x.GetComponentInChildren<VFXConstructing>(true) != null && x.GetComponentInChildren<VFXConstructing>(true).constructed < 100f)
                .ForEach(x => FinishHim(x));
        }
        private static IEnumerator CheckSpawnForMVs(TechType tt)
        {
            CoroutineTask<GameObject> request = CraftData.GetPrefabForTechTypeAsync(tt, true);
            yield return request;
            GameObject result = request.GetResult();
            if (result != null && result.GetComponent<AvsVehicle>() != null)
            {
                yield return new WaitForSeconds(0.5f);
                FinishAnySpawningVehicles();
            }
        }
    }
}

using AVS.BaseVehicle;
using HarmonyLib;
using System.Collections;
using System.Linq;
using UnityEngine;

// PURPOSE: allow the spawn console command to work for AvsVehicle
// VALUE: Moderate. Could register a new console command instead.

namespace AVS.Patches
{
    [HarmonyPatch(typeof(SpawnConsoleCommand))]
    public static class SpawnConsoleCommandPatcher
    {
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
        public static void FinishAnySpawningVehicles()
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
        public static IEnumerator CheckSpawnForMVs(TechType tt)
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

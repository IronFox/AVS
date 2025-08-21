using System.Collections;
using AVS.Log;
using UnityEngine;

namespace AVS.Util;

internal static class AvsCraftData
{
    internal static IEnumerator InstantiateFromPrefabAsync(LogWriter log, TechType techType,
        TaskResult<GameObject> result, bool customOnly = false)
    {
        log.Write($"Loading prefab for tech type {techType.AsString()} with customOnly={customOnly}");
        yield return CraftData.InstantiateFromPrefabAsync(techType, result, customOnly);
        log.Write($"Prefab for tech type {techType.AsString()} is done loading.");
    }
}
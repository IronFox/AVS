using System.Collections;
using AVS.Assets;
using AVS.Log;
using UnityEngine;

namespace AVS.Util;

internal static class AvsCraftData
{
    internal static IEnumerator InstantiateFromPrefabAsync(LogWriter log, TechType techType,
        TaskResult<GameObject> result, bool customOnly = false)
    {
        log.Write($"Loading prefab for tech type {techType.AsString()} with customOnly={customOnly}");
        var req = PrefabLoader.Request(techType, log, customOnly);
        yield return req.WaitUntilLoaded();
        var instance = req.Instantiate();
        if (instance == null)
        {
            log.Error($"Request for {techType.AsString()} produced no instance. This should never happen.");
            //result.Set(null);
            yield break;
        }

        result.Set(instance);
    }
}
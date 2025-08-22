using System.Collections;
using AVS.Assets;
using AVS.Log;
using UnityEngine;

namespace AVS.Util;

internal static class AvsCraftData
{
    /// <summary>
    /// Asynchronously instantiates a GameObject from a prefab based on a specified TechType.
    /// </summary>
    /// <param name="log">The log writer instance for handling logging activities.</param>
    /// <param name="techType">The type of technology for which the prefab is being instantiated.</param>
    /// <param name="result">The result wrapper that will hold the instantiated GameObject.</param>
    /// <param name="ifNotFoundLeaveEmpty">Specifies what to do if the tech type could not be found.
    /// If true, <paramref name="result"/> will be left empty (null).
    /// If false, <paramref name="result"/> is filled with a new generic loot item that has the requested tech type attached
    /// but no other components.</param>
    /// <returns>An enumerator allowing for asynchronous operation during the prefab instantiation process.</returns>
    internal static IEnumerator InstantiateFromPrefabAsync(LogWriter log, TechType techType,
        TaskResult<GameObject> result, bool ifNotFoundLeaveEmpty = true)
    {
        log.Write($"Loading prefab for tech type {techType.AsString()} with customOnly={ifNotFoundLeaveEmpty}");
        var req = PrefabLoader.Request(techType, log, ifNotFoundLeaveEmpty);
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
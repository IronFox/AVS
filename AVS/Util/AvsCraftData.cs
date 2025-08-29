using AVS.Assets;
using AVS.Log;
using System.Collections;
using UnityEngine;

namespace AVS.Util;

/// <summary>
/// Container for the result of <see cref="AvsCraftData.InstantiateFromPrefabAsync"/>
/// </summary>
internal class InstanceContainer
{
    /// <summary>
    /// Created instance or null if none could be created
    /// </summary>
    public GameObject? Instance { get; set; }
}

internal static class AvsCraftData
{
    /// <summary>
    /// Asynchronously instantiates a GameObject from a prefab based on a specified TechType.
    /// </summary>
    /// <param name="log">The logging instance used for logging messages during the instantiation process.</param>
    /// <param name="techType">The type of technology for which the prefab is being instantiated.</param>
    /// <param name="result">The result wrapper that will hold the instantiated GameObject.</param>
    /// <param name="ifNotFoundLeaveEmpty">Specifies what to do if the tech type could not be found.
    /// If true, <paramref name="result"/> will be left empty (null).
    /// If false, <paramref name="result"/> is filled with a new generic loot item that has the requested tech type attached
    /// but no other components.</param>
    /// <returns>An enumerator allowing for asynchronous operation during the prefab instantiation process.</returns>
    internal static IEnumerator InstantiateFromPrefabAsync(
        SmartLog log,
        TechType techType,
        InstanceContainer result, bool ifNotFoundLeaveEmpty = true)
    {
        log.Write($"Loading prefab for tech type {techType.AsString()} with customOnly={ifNotFoundLeaveEmpty}");
        result.Instance = null;
        var req = PrefabLoader.Request(techType, ifNotFoundLeaveEmpty);
        yield return req.WaitUntilLoaded();
        var instance = req.Instantiate();
        if (instance.IsNull())
        {
            log.Error($"Request for {techType.AsString()} produced no instance. This should never happen.");
            //result.Set(null);
            yield break;
        }

        result.Instance = instance;
    }
}
using AVS.Log;
using System.Collections;
using System.Collections.Generic;
using AVS.Util;
using UnityEngine;

namespace AVS.Assets;

/// <summary>
/// Helper class for loading prefabs asynchronously.
/// </summary>
public class PrefabLoader
{
    private static readonly Dictionary<(TechType, bool), PrefabLoader> _loaders = new();
    private LogWriter Log { get; }

    private PrefabLoader(TechType techType, bool ifNotFoundLeaveEmpty, LogWriter outLog)
    {
        Log = outLog.Tag(nameof(PrefabLoader)).Tag(techType.AsString());
        TechType = techType;
        IfNotFoundLeaveEmpty = ifNotFoundLeaveEmpty;
        _ = MainPatcher.Instance.StartCoroutine(LoadResource());
    }

    /// <summary>
    /// Specifies what to do if the tech type could not be found. <ul>
    /// <li>If true, <see cref="Prefab"/> will be left empty (null). Loading the prefab will terminate after 1 minute of
    /// repeated retrying and set <see cref="TerminalFailure"/> to true.
    /// <see cref="WaitUntilLoaded"/> will return.</li>
    /// <li>If false, <see cref="Prefab"/> will be filled with a new generic loot item that has the requested tech type attached
    /// but no other components. Loading will retry forever until an instance is returned.</li>
    /// </ul>
    /// </summary>
    public bool IfNotFoundLeaveEmpty { get; set; }

    /// <summary>
    /// Signals that prefabs can now be loaded. Until this call is done, no prefabs can be loaded
    /// </summary>
    internal static void SignalCanLoad()
    {
        CanLoad = true;
    }

    /// <summary>
    /// The tech type of the prefab being loaded.
    /// </summary>
    public TechType TechType { get; }


    /// <summary>
    /// Returns an awaitable object that completes once the local prefab has successfully loaded or
    /// <see cref="IfNotFoundLeaveEmpty"/> is true and loading has failed persistently for one minute
    /// </summary>
    public WaitUntil WaitUntilLoaded()
    {
        return new WaitUntil(() => Prefab || TerminalFailure);
    }

    /// <summary>
    /// Requests a <see cref="PrefabLoader"/> instance for the specified <see cref="TechType"/>.
    /// </summary>
    /// <remarks>
    /// Concurrent requests will receive the same instance. Loading will be done only once
    /// </remarks>
    /// <param name="techType">The <see cref="TechType"/> for which to request a <see cref="PrefabLoader"/>.</param>
    /// <param name="ifNotFoundLeaveEmpty">Specifies what to do if the tech type could not be found.
    /// If true, <see cref="Prefab"/> will be left empty (null).
    /// If false, <see cref="Prefab"/> is filled with a new generic loot item that has the requested tech type attached
    /// but no other components.</param>
    /// <param name="outLog">Out log writer. Effective only if this is the first request</param>
    /// <returns>A <see cref="PrefabLoader"/> instance associated with the specified <paramref name="techType"/>. If an
    /// instance already exists, it returns the existing instance; otherwise, it creates a new one and starts the loading process.</returns>
    public static PrefabLoader Request(TechType techType, LogWriter outLog, bool ifNotFoundLeaveEmpty)
    {
        if (_loaders.TryGetValue((techType, ifNotFoundLeaveEmpty), out var instance))
            return instance;

        _loaders[(techType, ifNotFoundLeaveEmpty)] =
            instance = new PrefabLoader(techType, ifNotFoundLeaveEmpty, outLog);
        return instance;
    }

    /// <summary>
    /// Loaded prefab or null if loading is not yet done, or <see cref="IfNotFoundLeaveEmpty"/> is true
    /// and loading has persistently failed.
    /// </summary>
    /// <remarks>
    /// Singleton instance set to never expire. Do <b>NOT</b> destroy this object
    /// </remarks>
    public GameObject? Prefab { get; private set; }

    /// <summary>
    /// Instantiates a new game object from the prefab instance
    /// Null if the prefab has not (yet) been loaded
    /// </summary>
    public GameObject? Instantiate()
    {
        var prefab = Prefab;
        if (!prefab)
            return null;
        return Object.Instantiate(prefab);
    }

    /// <summary>
    /// True if the prefab can be loaded, false if any ongoing loading operation should be delayed.
    /// </summary>
    internal static bool CanLoad { get; private set; }

    private IEnumerator LoadResource()
    {
        var iteration = 0;

        while (true)
        {
            iteration++;
            yield return new WaitUntil(() => CanLoad);

            Log.Write($"Requesting prefab for {TechType}.");
            TaskResult<GameObject> result = new();
            var cor = MainPatcher.Instance.StartCoroutine(
                CraftData.InstantiateFromPrefabAsync(TechType, result, IfNotFoundLeaveEmpty));
            yield return cor;
            var prefab = result.Get();
            if (prefab == null)
            {
                if (iteration > 60 && IfNotFoundLeaveEmpty)
                {
                    Log.Error($"Persistently failed to load prefab for {TechType}. Aborting");
                    TerminalFailure = true;
                    yield break; // give up after 60 seconds if we are supposed to leave empty
                }

                Log.Warn(
                    $"Failed to load prefab for {TechType} at this time (iteration {iteration}). Will retry in 1 second.");
                yield return new WaitForSeconds(1f); // wait a bit and try again
            }
            else
            {
                Log.Write($"Loaded {prefab.NiceName()} for {TechType}. Setting as DontDestroyOnLoad.");
                Object.DontDestroyOnLoad(prefab);
                prefab.hideFlags |= HideFlags.HideAndDontSave;
                prefab.SetActive(false);
                Prefab = prefab;
                yield break;
            }
        }
    }

    /// <summary>
    /// Loading of the requested prefab has persistently failed and <see cref="IfNotFoundLeaveEmpty"/> is true.
    /// </summary>
    public bool TerminalFailure { get; set; }
}
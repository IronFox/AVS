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

    private PrefabLoader(TechType techtype, bool customOnly, LogWriter outLog)
    {
        Log = outLog.Tag(nameof(PrefabLoader)).Tag(techtype.AsString());
        Techtype = techtype;
        CustomOnly = customOnly;
        SingleAwaitableCoroutine = MainPatcher.Instance.StartCoroutine(LoadResource());
    }

    /// <summary>
    /// True if this loader was instantiated with the customOnly flag
    /// </summary>
    public bool CustomOnly { get; set; }

    internal static void SignalCanLoad()
    {
        CanLoad = true;
    }

    /// <summary>
    /// The tech type of the prefab being loaded.
    /// </summary>
    public TechType Techtype { get; }

    /// <summary>
    /// The coroutine that is responsible for loading the prefab.
    /// </summary>
    private Coroutine SingleAwaitableCoroutine { get; }

    /// <summary>
    /// Returns an awaitable object that completes ones the local prefab has successfully loaded
    /// </summary>
    public WaitUntil WaitUntilLoaded()
    {
        return new WaitUntil(() => Prefab != null);
    }

    /// <summary>
    /// Requests a <see cref="PrefabLoader"/> instance for the specified <see cref="TechType"/>.
    /// </summary>
    /// <param name="techtype">The <see cref="TechType"/> for which to request a <see cref="PrefabLoader"/>.</param>
    /// <param name="customOnly">If true, Subnautica will not load a custom instance if the tech type is not found</param>
    /// <param name="outLog">Out log writer. Effective only if this is the first request</param>
    /// <returns>A <see cref="PrefabLoader"/> instance associated with the specified <paramref name="techtype"/>. If an
    /// instance already exists, it returns the existing instance; otherwise, it creates a new one and starts the loading process.</returns>
    public static PrefabLoader Request(TechType techtype, LogWriter outLog, bool customOnly = false)
    {
        if (_loaders.TryGetValue((techtype, customOnly), out var instance))
            return instance;

        _loaders[(techtype, customOnly)] = instance = new PrefabLoader(techtype, customOnly, outLog);
        return instance;
    }

    /// <summary>
    /// Queries the latest known prefab instance of the requested resource.
    /// </summary>
    public GameObject? Prefab { get; private set; }

    /// <summary>
    /// Instantiates a new game object from the prefab instance
    /// </summary>
    public GameObject? Instantiate()
    {
        var prefab = Prefab;
        return Object.Instantiate(prefab);
    }

    /// <summary>
    /// True if the prefab can be loaded, false if any ongoing loading operation should be delayed.
    /// </summary>
    internal static bool CanLoad { get; private set; }

    private IEnumerator LoadResource()
    {
        while (true)
        {
            while (!CanLoad)
                yield return new WaitForSeconds(0.1f); // wait until we can load

            Log.Write($"Requesting prefab for {Techtype}.");
            TaskResult<GameObject> result = new();
            var cor = MainPatcher.Instance.StartCoroutine(
                CraftData.InstantiateFromPrefabAsync(Techtype, result, CustomOnly));
            yield return cor;
            var prefab = result.Get();
            if (prefab == null)
            {
                Log.Error($"Failed to load prefab for {Techtype} at this time.");
                yield return new WaitForSeconds(1f); // wait a bit and try again
            }
            else
            {
                Log.Write($"Loaded {prefab.NiceName()} for {Techtype}. Setting as DontDestroyOnLoad.");
                Object.DontDestroyOnLoad(prefab);
                prefab.hideFlags |= HideFlags.HideAndDontSave;
                prefab.SetActive(false);
                Prefab = prefab;
                yield break;
            }
        }
    }
}
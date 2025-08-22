using AVS.Log;
using AVS.Util;
using System;
using UnityEngine;

namespace AVS.Assets;

/// <summary>
/// Global helper for loading the Seamoth prefab.
/// </summary>
public static class SeamothHelper
{
    private static PrefabLoader? loader;

    /// <summary>
    /// Access to the coroutine that loads the Seamoth prefab.
    /// Allocated on first access, so it is safe to call this property multiple times.
    /// </summary>
    public static void Request()
    {
        GetOrCreateLoader();
    }

    private static PrefabLoader GetOrCreateLoader()
    {
        if (loader is null)
        {
            LogWriter.Default.Write($"Loading Seamoth prefab...");
            loader = PrefabLoader.Request(TechType.Seamoth, LogWriter.Default, true);
        }

        return loader;
    }

    /// <summary>
    /// Returns an awaitable object that completes once the Seamoth has successfully been loaded or
    /// loading has failed persistently for one minute
    /// </summary>
    public static object WaitUntilLoaded() => GetOrCreateLoader().WaitUntilLoaded();


    /// <summary>
    /// Tries to access the Seamoth prefab.
    /// May return null if the prefab is not yet loaded.
    /// </summary>
    public static GameObject? Seamoth
        => GetOrCreateLoader().Prefab;

    /// <summary>
    /// Access to the Seamoth prefab, guaranteed to be non-null.
    /// Throws an <see cref="InvalidOperationException"/> if the prefab is not yet loaded.
    /// </summary>
    public static GameObject RequireSeamoth
        => Seamoth.OrThrow(() => new InvalidOperationException($"Trying to access Seamoth before it is loaded"));
}
using AVS.Log;
using AVS.Util;
using System;
using UnityEngine;

namespace AVS.Assets
{

    /// <summary>
    /// Global helper for loading the Seamoth prefab.
    /// </summary>
    public class SeamothHelper
    {
        private static PrefabLoader? loader;

        /// <summary>
        /// Access to the coroutine that loads the Seamoth prefab.
        /// Allocated on first access, so it is safe to call this property multiple times.
        /// </summary>
        public static Coroutine Coroutine
        {
            get
            {
                if (loader == null)
                {
                    LogWriter.Default.Write($"Loading Seamoth prefab...");
                    loader = PrefabLoader.Request(TechType.Seamoth);
                }
                return loader.Coroutine;
            }
        }
        /// <summary>
        /// Tries to access the Seamoth prefab.
        /// If <see cref="Coroutine" /> was never accessed, it will throw an <see cref="InvalidOperationException"/>.
        /// Ohterwise it may return null if the prefab is not yet loaded.
        /// </summary>
        public static GameObject? Seamoth
            => (loader ?? throw new InvalidOperationException($"Trying to access Seamoth before it has even started loading"))
            .Instance;

        /// <summary>
        /// Access to the Seamoth prefab, guaranteed to be non-null.
        /// Throws an <see cref="InvalidOperationException"/> if the prefab is not yet loaded.
        /// </summary>
        public static GameObject RequireSeamoth
            => Seamoth.OrThrow(() => new InvalidOperationException($"Trying to access Seamoth before it is loaded"));
    }
}

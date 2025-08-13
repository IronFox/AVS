using AVS.Log;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.Assets
{
    /// <summary>
    /// Helper class for loading prefabs asynchronously.
    /// </summary>
    public class PrefabLoader
    {
        private static Dictionary<TechType, PrefabLoader> _loaders = new Dictionary<TechType, PrefabLoader>();
        private TaskResult<GameObject> RequestResult { get; } = new TaskResult<GameObject>();
        private Coroutine? cor = null;

        private PrefabLoader(TechType techtype)
        {
            Techtype = techtype;
            Coroutine = MainPatcher.Instance.StartCoroutine(LoadResource());
        }

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
        public Coroutine Coroutine { get; }

        /// <summary>
        /// Requests a <see cref="PrefabLoader"/> instance for the specified <see cref="TechType"/>.
        /// </summary>
        /// <param name="techtype">The <see cref="TechType"/> for which to request a <see cref="PrefabLoader"/>.</param>
        /// <returns>A <see cref="PrefabLoader"/> instance associated with the specified <paramref name="techtype"/>. If an
        /// instance already exists, it returns the existing instance; otherwise, it creates a new one and starts the loading process.</returns>
        public static PrefabLoader Request(TechType techtype)
        {
            if (_loaders.TryGetValue(techtype, out var instance))
                return instance;
            LogWriter.Default.Write($"PrefabLoader: Creating new loader for {techtype}.");
            _loaders[techtype] = instance = new PrefabLoader(techtype);
            return instance;
        }

        /// <summary>
        /// Queries the latest known instance of the requested resource.
        /// </summary>
        public GameObject? Instance
        {
            get
            {
                GameObject thisInstance = RequestResult.Get();
                if (thisInstance == null)
                {
                    return null;
                }
                UnityEngine.Object.DontDestroyOnLoad(thisInstance);
                thisInstance.SetActive(false);
                return thisInstance;
            }
        }

        /// <summary>
        /// True if the prefab can be loaded, false if any ongoing loading operation should be delayed.
        /// </summary>
        internal static bool CanLoad { get; private set; }

        private IEnumerator LoadResource()
        {
            while (Instance == null)
            {
                while (!CanLoad)
                    yield return new WaitForSeconds(0.1f); // wait until we can load

                LogWriter.Default.Write($"PrefabLoader: Requesting prefab for {Techtype}.");
                if (RequestResult.Get()) // if we have instance
                {
                    LogWriter.Default.Write($"PrefabLoader: Prefab for {Techtype} is done loading.");
                    yield break;
                }
                else if (cor == null) // if we need to get instance
                {
                    cor = MainPatcher.Instance.StartCoroutine(CraftData.InstantiateFromPrefabAsync(Techtype, RequestResult, false));
                    yield return cor;
                    cor = null;
                }
                else
                {
                    yield return null;
                }
            }
        }

    }
}

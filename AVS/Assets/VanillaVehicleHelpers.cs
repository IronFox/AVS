using System.Collections;
using UnityEngine;

namespace AVS
{
    /// <summary>
    /// Helper class for managing the Seamoth prefab instance.
    /// Provides methods to asynchronously ensure the Seamoth prefab is loaded and accessible.
    /// </summary>
    public static class SeamothHelper
    {
        /// <summary>
        /// Stores the result of the asynchronous Seamoth prefab instantiation.
        /// </summary>
        private static TaskResult<GameObject> Request { get; } = new TaskResult<GameObject>();

        /// <summary>
        /// Reference to the currently running coroutine for loading the Seamoth prefab.
        /// </summary>
        private static Coroutine? cor = null;

        /// <summary>
        /// Gets the loaded Seamoth prefab GameObject, or null if not yet loaded.
        /// The prefab is set to inactive and marked to not be destroyed on load.
        /// </summary>
        public static GameObject? Seamoth
        {
            get
            {
                GameObject thisSeamoth = Request.Get();
                if (thisSeamoth == null)
                {
                    //Logger.DebugLog("Couldn't get Seamoth. This is probably normal, and we'll probably get it next frame.");
                    return null;
                }
                UnityEngine.Object.DontDestroyOnLoad(thisSeamoth);
                thisSeamoth.SetActive(false);
                return thisSeamoth;
            }
        }

        /// <summary>
        /// Coroutine that ensures the Seamoth prefab is loaded.
        /// If not loaded, starts the asynchronous instantiation and waits until it is available.
        /// </summary>
        public static IEnumerator EnsureSeamoth()
        {
            while (Seamoth == null)
            {
                if (Request.Get()) // if we have prawn
                {
                    yield break;
                }
                else if (cor == null) // if we need to get prawn
                {
                    cor = UWE.CoroutineHost.StartCoroutine(CraftData.InstantiateFromPrefabAsync(TechType.Seamoth, Request, false));
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


    /// <summary>
    /// Helper class for managing the Prawn prefab instance.
    /// Provides methods to asynchronously ensure the Prawn prefab is loaded and accessible.
    /// </summary>
    public static class PrawnHelper
    {
        private static TaskResult<GameObject> Request { get; } = new TaskResult<GameObject>();
        private static Coroutine? cor = null;

        /// <summary>
        /// Gets the loaded PRAWN prefab GameObject, or null if not yet loaded.
        /// The prefab is set to inactive and marked to not be destroyed on load.
        /// </summary>
        public static GameObject? Prawn
        {
            get
            {
                GameObject thisPrawn = Request.Get();
                if (thisPrawn == null)
                {
                    //Logger.DebugLog("Couldn't get Prawn. This is probably normal, and we'll probably get it next frame.");
                    return null;
                }
                UnityEngine.Object.DontDestroyOnLoad(thisPrawn);
                thisPrawn.SetActive(false);
                return thisPrawn;
            }
        }

        /// <summary>
        /// Coroutine that ensures the PRAWN prefab is loaded.
        /// If not loaded, starts the asynchronous instantiation and waits until it is available.
        /// </summary>
        public static IEnumerator EnsurePrawn()
        {
            while (Prawn == null)
            {
                if (Request.Get()) // if we have prawn
                {
                    yield break;
                }
                else if (cor == null) // if we need to get prawn
                {
                    cor = UWE.CoroutineHost.StartCoroutine(CraftData.InstantiateFromPrefabAsync(TechType.Exosuit, Request, false));
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

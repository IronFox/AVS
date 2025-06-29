﻿using System.Collections;
using UnityEngine;

namespace AVS
{
    public static class SeamothHelper
    {
        internal static TaskResult<GameObject> request = new TaskResult<GameObject>();
        private static Coroutine cor = null;
        public static GameObject Seamoth
        {
            get
            {
                GameObject thisSeamoth = request.Get();
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
        public static IEnumerator EnsureSeamoth()
        {
            while (Seamoth == null)
            {
                if (request.Get()) // if we have prawn
                {
                    yield break;
                }
                else if (cor == null) // if we need to get prawn
                {
                    cor = UWE.CoroutineHost.StartCoroutine(CraftData.InstantiateFromPrefabAsync(TechType.Seamoth, request, false));
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
    public static class PrawnHelper
    {
        internal static TaskResult<GameObject> request = new TaskResult<GameObject>();
        private static Coroutine cor = null;
        public static GameObject Prawn
        {
            get
            {
                GameObject thisPrawn = request.Get();
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
        public static IEnumerator EnsurePrawn()
        {
            while (Prawn == null)
            {
                if (request.Get()) // if we have prawn
                {
                    yield break;
                }
                else if (cor == null) // if we need to get prawn
                {
                    cor = UWE.CoroutineHost.StartCoroutine(CraftData.InstantiateFromPrefabAsync(TechType.Exosuit, request, false));
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

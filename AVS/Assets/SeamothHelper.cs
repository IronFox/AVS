using AVS.Log;
using AVS.Util;
using System;
using UnityEngine;

namespace AVS.Assets
{
    public class SeamothHelper
    {
        private static PrefabLoader? loader;

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
        public static GameObject? Seamoth
            => (loader ?? throw new InvalidOperationException($"Trying to access Seamoth before it has even started loading"))
            .Instance;
        public static GameObject RequireSeamoth
            => Seamoth.OrThrow(() => throw new InvalidOperationException($"Trying to access Seamoth before it is loaded"));
    }
}

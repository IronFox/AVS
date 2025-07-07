using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UWE;

// PURPOSE: Allow easy registration of AudioSources and pause them during game pause.
// VALUE: High.

namespace AVS.Patches
{
    [HarmonyPatch(typeof(FreezeTime))]
    public class FreezeTimePatcher
    {
        private static List<AudioSource> audioSources = new List<AudioSource>();
        public static AudioSource Register(AudioSource source)
        {
            audioSources.RemoveAll(item => item == null);
            audioSources.Add(source);
            return source;
        }
        [HarmonyPostfix]
        [HarmonyPatch(nameof(FreezeTime.Set))]
        public static void FreezeTimeSetPostfix()
        {
            audioSources.RemoveAll(item => item == null);
            if (FreezeTime.HasFreezers())
            {
                audioSources.ForEach(x => { if (x) x.Pause(); });
            }
            else
            {
                audioSources.ForEach(x => { if (x) x.UnPause(); });
            }
        }
    }
}

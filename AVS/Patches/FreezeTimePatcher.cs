using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UWE;

// PURPOSE: Allow easy registration of AudioSources and pause them during game pause.
// VALUE: High.

namespace AVS.Patches
{
    /// <summary>
    /// Provides functionality for managing audio sources in relation to the game's pause state, utilizing the FreezeTime system.
    /// </summary>
    /// <remarks>
    /// This class allows for the registration of audio sources, ensuring they are paused when the game is paused
    /// and unpaused when the game resumes. It interacts with the FreezeTime system to monitor pause state transitions.
    /// </remarks>
    [HarmonyPatch(typeof(FreezeTime))]
    public class FreezeTimePatcher
    {
        private static readonly List<AudioSource> audioSources = [];

        /// <summary>
        /// Registers an AudioSource to be managed by the FreezeTime system.
        /// </summary>
        /// <remarks>
        /// This method adds the given AudioSource to a managed list, ensuring its state (paused or unpaused)
        /// reflects the game's pause status handled by the FreezeTime system. Null references in the list
        /// are removed to maintain integrity.
        /// </remarks>
        /// <param name="source">The AudioSource to be registered.</param>
        /// <returns>The registered AudioSource.</returns>
        public static AudioSource Register(AudioSource source)
        {
            audioSources.RemoveAll(item => !item);
            audioSources.Add(source);
            return source;
        }

        /// <summary>
        /// Postfix method executed after FreezeTime.Set is invoked to manage the state of registered AudioSources.
        /// </summary>
        /// <remarks>
        /// This method checks the state of the FreezeTime system to determine whether the game is paused.
        /// If the game is paused, all valid AudioSources in the registered list are paused.
        /// Conversely, if the game is unpaused, all valid AudioSources are resumed.
        /// It also removes null references from the list to maintain its integrity.
        /// </remarks>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(FreezeTime.Set))]
        public static void FreezeTimeSetPostfix()
        {
            audioSources.RemoveAll(item => !item);
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

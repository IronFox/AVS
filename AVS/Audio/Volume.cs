using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVS.Audio
{
    /// <summary>
    /// Game sound volume access helper. Necessary only when creating sounds outside the control of AVS
    /// </summary>
    public static class Volume
    {
        /// <summary>
        /// Gets the current master volume level for the sound system.
        /// </summary>
        public static float Master => SoundSystem.GetMasterVolume();

        /// <summary>
        /// Gets the current music volume level.
        /// </summary>
        public static float Music => SoundSystem.GetMusicVolume();

        /// <summary>
        /// Gets the current global voice volume level.
        /// </summary>
        public static float Voice => SoundSystem.GetVoiceVolume();

        /// <summary>
        /// Gets the current ambient sound volume level.
        /// </summary>
        public static float Ambient => SoundSystem.GetAmbientVolume();
    }
}

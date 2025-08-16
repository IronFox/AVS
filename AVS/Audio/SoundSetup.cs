using UnityEngine;

namespace AVS.Audio
{

    /// <summary>
    /// Settings that can be changed during the lifetime of a sound source
    /// </summary>
    /// <param name="Volume">The volume value for the sound. Default is 1.0f.</param>
    /// <param name="Pitch">The pitch value for the sound. Default is 1.0f.</param>
    /// <param name="MinDistance">The minimum distance at which point the sound will not get any louder. Default is 1 (meter)</param>
    /// <param name="MaxDistance">The maximum distance at which point the sound can no longer be heard. Default is 500 (meters)</param>
    public readonly record struct SoundSettings(
        float MinDistance = 1f,
        float MaxDistance = 500f,
        float Volume = 1f,
        float Pitch = 1f
    )
    {

        /// <summary>
        /// Determines whether the current settings are significantly different from another <see cref="SoundSettings"/> instance.
        /// </summary>
        /// <param name="other">The other <see cref="SoundSettings"/> to compare with.</param>
        /// <returns>True if the settings differ by more than a small threshold; otherwise, false.</returns>
        public bool IsSignificantlyDifferent(SoundSettings other)
        {
            if (!SigDif(Pitch, other.Pitch)
                && !SigDif(Volume, other.Volume)
                && !SigDif(MinDistance, other.MinDistance)
                && !SigDif(MaxDistance, other.MaxDistance))
                return false;

            return true;
        }

        /// <summary>
        /// Determines if two float values are significantly different based on a fixed threshold.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns>True if the absolute difference is greater than 0.005f; otherwise, false.</returns>
        private static bool SigDif(float a, float b)
        {
            return Mathf.Abs(a - b) > 0.005f;
        }
    }

    /// <summary>
    /// Represents the configuration for a sound source, including its audio clip, playback settings, and spatial properties.
    /// </summary>
    /// <remarks>
    /// This struct encapsulates the settings required to configure and play a sound, such as the
    /// associated audio clip, volume, pitch, spatial distances, and looping behavior. It is immutable and can be used
    /// to define sound properties for playback in both 2D and 3D contexts.
    /// </remarks>
    /// <summary>
    /// Represents the configuration for a sound source, including its audio clip, playback settings, and spatial properties.
    /// </summary>
    /// <param name="Owner">The owning game object</param>
    /// <param name="AudioClip">The audio clip associated with this instance</param>
    /// <param name="Settings">The initial volume and pitch settings</param>
    /// <param name="MinDistance">The minimum distance for the listener. If the listener is closer than this distance, the sound will be played at full volume.</param>
    /// <param name="MaxDistance">The maximum distance for the listener. If the listener is farther than this distance, the sound will not be heard.</param>
    /// <param name="HalfDistance">The listener distance at which the sound volume is exactly half the maximum volume. Must be in the range (MinDistance*2, MaxDistance-MinDistance), otherwise clamped.</param>
    /// <param name="Loop">Whether the playback is set to loop</param>
    /// <param name="Is3D">Whether the object is represented in 3D</param>
    /// <exception cref="System.ArgumentNullException">Thrown if AudioClip is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown if distance parameters are invalid.</exception>
    public readonly record struct SoundSetup(
        GameObject Owner,
        AudioClip AudioClip,
        SoundSettings Settings = default,
        float HalfDistance = 20f,
        bool Loop = false,
        bool Is3D = true)
    {
        /// <summary>
        /// Validates the current <see cref="SoundSetup"/> instance to ensure that all properties have valid values.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <see cref="AudioClip"/> or <see cref="Owner"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <see cref="MinDistance"/> is greater than <see cref="MaxDistance"/>,
        /// <see cref="HalfDistance"/> is greater than the difference between <see cref="MaxDistance"/>
        /// and <see cref="MinDistance"/>, <see cref="HalfDistance"/> is less than twice the <see cref="MinDistance"/>,
        /// or if <see cref="MinDistance"/> or <see cref="MaxDistance"/> is negative.
        /// </exception>
        public void Validate()
        {
            if (AudioClip == null)
                throw new System.ArgumentNullException(nameof(AudioClip));
            if (Settings.MinDistance > Settings.MaxDistance)
                throw new System.ArgumentOutOfRangeException(nameof(Settings.MinDistance));
            if (HalfDistance > Settings.MaxDistance - Settings.MinDistance)
                throw new System.ArgumentOutOfRangeException(nameof(HalfDistance));
            if (HalfDistance < Settings.MinDistance * 2)
                throw new System.ArgumentOutOfRangeException(nameof(HalfDistance));
            if (Settings.MinDistance < 0)
                throw new System.ArgumentOutOfRangeException(nameof(Settings.MinDistance));
            if (Settings.MaxDistance < 0)
                throw new System.ArgumentOutOfRangeException(nameof(Settings.MaxDistance));
            if (Owner == null)
                throw new System.ArgumentNullException(nameof(Owner));
        }

        /// <summary>
        /// Determines if this <see cref="SoundSetup"/> is compatible with another for live playback (e.g., can be swapped without stopping playback).
        /// </summary>
        /// <param name="other">The other <see cref="SoundSetup"/> to compare with.</param>
        /// <returns>True if both setups use the same AudioClip and looping setting; otherwise, false.</returns>
        public bool IsLiveCompatibleTo(SoundSetup other)
        {
            if (AudioClip == other.AudioClip)
            {
                return Loop == other.Loop;
            }

            return false;
        }
    }
}
